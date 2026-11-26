using WizRoster.Core.Disk;

namespace WizRoster.Tests;

public class D88Tests
{
    /// <summary>
    /// 小さな D88 を作る。各トラックのセクタは R の逆順で記録し(並べ直しの確認のため)、
    /// データの先頭バイトに「トラック × 16 + R」を入れる。
    /// </summary>
    private static byte[] BuildDisk(string name, int tracks, int sectorsPerTrack, int sectorSize, bool writeProtected)
    {
        const int header = 0x2B0;
        int trackBytes = sectorsPerTrack * (16 + sectorSize);
        var d = new byte[header + tracks * trackBytes];
        System.Text.Encoding.ASCII.GetBytes(name).CopyTo(d, 0);
        d[0x1A] = (byte)(writeProtected ? 0x10 : 0);
        BitConverter.GetBytes(d.Length).CopyTo(d, 0x1C);
        for (int t = 0; t < tracks; t++)
        {
            int trackOff = header + t * trackBytes;
            BitConverter.GetBytes(trackOff).CopyTo(d, 0x20 + t * 4);
            for (int s = 0; s < sectorsPerTrack; s++)
            {
                int o = trackOff + s * (16 + sectorSize);
                int r = sectorsPerTrack - s; // 逆順
                d[o] = (byte)(t / 2);
                d[o + 1] = (byte)(t % 2);
                d[o + 2] = (byte)r;
                d[o + 3] = (byte)(sectorSize / 256);
                BitConverter.GetBytes((ushort)sectorsPerTrack).CopyTo(d, o + 4);
                BitConverter.GetBytes((ushort)sectorSize).CopyTo(d, o + 14);
                d[o + 16] = (byte)(t * 16 + r);
            }
        }
        return d;
    }

    [Fact]
    public void Parse_OrdersSectorsByTrackThenR_AndReadsMultipleDisks()
    {
        byte[] file = [.. BuildDisk("DISK-A", 2, 4, 256, false), .. BuildDisk("DISK-B", 2, 4, 256, true)];
        var disks = D88Disk.TryParse(file);

        Assert.NotNull(disks);
        Assert.Equal(2, disks!.Count);
        Assert.Equal("DISK-A", disks[0].Name);
        Assert.False(disks[0].IsWriteProtected);
        Assert.True(disks[1].IsWriteProtected);

        byte[] linear = disks[0].ReadLinear(file);
        var firstBytes = Enumerable.Range(0, 8).Select(i => linear[i * 256]).ToArray();
        Assert.Equal(new byte[] { 1, 2, 3, 4, 17, 18, 19, 20 }, firstBytes);
    }

    [Fact]
    public void Image_WritesIntoSectorDataOnly()
    {
        byte[] file = BuildDisk("DISK-A", 2, 4, 256, false);
        byte[] original = (byte[])file.Clone();
        var disk = D88Disk.TryParse(file)![0];
        var image = new D88Image(file, disk, firstSector: 2);

        Assert.Equal(3, image.BlockCount); // 8 セクタのうち先頭 2 つを飛ばして、2 セクタずつ
        var block = new byte[512];
        image.ReadBlock(0, block);
        Assert.Equal(3, block[0]);   // 論理セクタ 2 = トラック 0 の R=3
        Assert.Equal(4, block[256]); // 論理セクタ 3 = R=4

        block.AsSpan().Fill(0xAA);
        image.WriteBlock(0, block);
        int changed = Enumerable.Range(0, file.Length).Count(i => file[i] != original[i]);
        Assert.Equal(512, changed); // 見出しは変わらず、データ 2 セクタ分だけ
    }

    [Fact]
    public void TryParse_ReturnsNullForNonD88()
    {
        Assert.Null(D88Disk.TryParse(new byte[737280]));
        Assert.Null(D88Disk.TryParse(new byte[10]));
    }

    [Fact]
    public void TryParse_RejectsMixedSectorSizes()
    {
        byte[] file = BuildDisk("DISK-A", 2, 4, 256, false);
        // 2 トラック目の先頭セクタだけ大きさを 128 にする
        int track1 = BitConverter.ToInt32(file, 0x24);
        BitConverter.GetBytes((ushort)128).CopyTo(file, track1 + 14);
        Assert.Throws<InvalidDataException>(() => D88Disk.TryParse(file));
    }
}
