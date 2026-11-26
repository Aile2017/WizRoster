using System.Text;
using WizRoster.Core.Model;

namespace WizRoster.Core.Disk;

/// <summary>
/// D88 形式のディスク 1 枚。1 つのファイルに複数枚が続けて入っていることがある。
/// <list type="bullet">
/// <item>ヘッダ(0x2B0 バイト): +0 ディスク名(17), +0x1A 書き込み禁止(0x10), +0x1B 媒体, +0x1C ディスクの大きさ, +0x20 トラックの位置の表(最大 164 件)</item>
/// <item>セクタ: 16 バイトの見出し(C, H, R, N, +4 トラック内のセクタ数, +14 データの大きさ)と、それに続くデータ</item>
/// </list>
/// セクタは「トラックの表の順 → トラック内はセクタ番号 R の順」に並べて、論理セクタ番号を付ける。
/// </summary>
public sealed class D88Disk
{
    private const int HeaderSize = 0x2B0;
    private const int MaxTracks = 164;
    private const int SectorHeaderSize = 16;

    private D88Disk(int index, int fileOffset, string name, bool writeProtected, int media, int[] sectorOffsets, int sectorSize)
    {
        Index = index;
        FileOffset = fileOffset;
        Name = name;
        IsWriteProtected = writeProtected;
        Media = media;
        SectorOffsets = sectorOffsets;
        SectorSize = sectorSize;
    }

    /// <summary>ファイル内で何枚目か(0 から)。</summary>
    public int Index { get; }
    public int FileOffset { get; }
    public string Name { get; }
    public bool IsWriteProtected { get; }
    public int Media { get; }

    /// <summary>論理セクタ番号 → ファイル内のデータの位置。</summary>
    public IReadOnlyList<int> SectorOffsets { get; }

    /// <summary>セクタの大きさ(全セクタ同じときだけ扱う)。</summary>
    public int SectorSize { get; }

    public string MediaName => Media switch
    {
        0x00 => "2D",
        0x10 => "2DD",
        0x20 => "2HD",
        0x30 => "1D",
        0x40 => "1DD",
        _ => $"0x{Media:X2}",
    };

    /// <summary>D88 として読めればディスクの一覧を返す。D88 でなければ null。</summary>
    /// <exception cref="InvalidDataException">D88 だが、セクタの形が扱えないとき。</exception>
    public static IReadOnlyList<D88Disk>? TryParse(byte[] file)
    {
        var disks = new List<D88Disk>();
        int pos = 0;
        while (pos + HeaderSize <= file.Length)
        {
            int size = (int)Math.Min(int.MaxValue, (long)WizBytes.U16(file, pos + 0x1C) | ((long)WizBytes.U16(file, pos + 0x1E) << 16));
            if (size < HeaderSize || pos + (long)size > file.Length)
                break;
            var disk = ParseDisk(file, disks.Count, pos, size);
            if (disk == null)
                break;
            disks.Add(disk);
            pos += size;
        }
        // ファイルの最後までディスクとして読めたときだけ D88 とみなす
        return disks.Count > 0 && pos == file.Length ? disks : null;
    }

    private static D88Disk? ParseDisk(byte[] file, int index, int pos, int size)
    {
        var trackOffsets = new List<int>();
        int firstTrack = int.MaxValue;
        for (int t = 0; t < MaxTracks; t++)
        {
            int entry = 0x20 + t * 4;
            if (entry >= firstTrack || entry + 4 > HeaderSize)
                break; // 表が短い形式(160 件など)ではトラックのデータが先に始まる
            int off = WizBytes.U16(file, pos + entry) | (WizBytes.U16(file, pos + entry + 2) << 16);
            if (off == 0)
                continue;
            if (off < 0x20 + 4 || off >= size)
                return null;
            firstTrack = Math.Min(firstTrack, off);
            trackOffsets.Add(off);
        }
        if (trackOffsets.Count == 0)
            return null;

        var sectors = new List<int>();
        int sectorSize = -1;
        foreach (int trackOff in trackOffsets)
        {
            int o = pos + trackOff;
            if (o + SectorHeaderSize > pos + size)
                return null;
            int count = WizBytes.U16(file, o + 4);
            var track = new List<(int R, int DataOffset)>(count);
            for (int s = 0; s < count; s++)
            {
                if (o + SectorHeaderSize > pos + size)
                    return null;
                int r = file[o + 2];
                int dataSize = WizBytes.U16(file, o + 14);
                if (o + SectorHeaderSize + dataSize > pos + size)
                    return null;
                if (sectorSize < 0)
                    sectorSize = dataSize;
                else if (dataSize != sectorSize)
                    throw new InvalidDataException(
                        $"D88 ディスク {index + 1} にセクタの大きさが違うトラックがあります({sectorSize} と {dataSize})。この形のディスクには対応していません。");
                track.Add((r, o + SectorHeaderSize));
                o += SectorHeaderSize + dataSize;
            }
            if (track.Select(t => t.R).Distinct().Count() != track.Count)
                throw new InvalidDataException($"D88 ディスク {index + 1} に同じ番号のセクタが重なったトラックがあります。この形のディスクには対応していません。");
            sectors.AddRange(track.OrderBy(t => t.R).Select(t => t.DataOffset));
        }

        string name = Encoding.Latin1.GetString(file, pos, 17).Split('\0')[0].Trim();
        bool writeProtected = (file[pos + 0x1A] & 0x10) != 0;
        return new D88Disk(index, pos, name, writeProtected, file[pos + 0x1B], sectors.ToArray(), sectorSize);
    }

    /// <summary>セクタを順につなげたデータ(ボリュームの位置を探すため。読み取り専用のコピー)。</summary>
    public byte[] ReadLinear(byte[] file)
    {
        var result = new byte[SectorOffsets.Count * SectorSize];
        for (int i = 0; i < SectorOffsets.Count; i++)
            Array.Copy(file, SectorOffsets[i], result, i * SectorSize, SectorSize);
        return result;
    }
}

/// <summary>
/// D88 ディスク 1 枚を p-System のブロックとして読み書きする。
/// ブロック b は論理セクタ <c>FirstSector + b × (512 / セクタの大きさ)</c> から始まる。
/// </summary>
public sealed class D88Image : IBlockImage
{
    private readonly D88Disk _disk;
    private readonly int _firstSector;
    private readonly int _sectorsPerBlock;

    public D88Image(byte[] fileBytes, D88Disk disk, int firstSector)
    {
        if (disk.SectorSize is not (128 or 256 or 512))
            throw new InvalidDataException($"D88 のセクタの大きさ {disk.SectorSize} には対応していません。");
        FileBytes = fileBytes;
        _disk = disk;
        _firstSector = firstSector;
        _sectorsPerBlock = BlockImageExtensions.BlockSize / disk.SectorSize;
    }

    public D88Disk Disk => _disk;

    public string FormatName =>
        $"D88 ディスク {_disk.Index + 1}「{_disk.Name}」({_disk.MediaName}{(_firstSector > 0 ? $", 先頭セクタ {_firstSector}" : "")})";

    public byte[] FileBytes { get; }
    public int BlockCount => Math.Max(0, (_disk.SectorOffsets.Count - _firstSector) / _sectorsPerBlock);
    public bool IsWriteProtected => _disk.IsWriteProtected;
    public int DiskIndex => _disk.Index;
    public string DiskLabel => $"ディスク {_disk.Index + 1}「{_disk.Name}」";

    public void ReadBlock(int block, Span<byte> destination)
    {
        int size = _disk.SectorSize;
        for (int i = 0; i < _sectorsPerBlock; i++)
            FileBytes.AsSpan(SectorOffset(block, i), size).CopyTo(destination[(i * size)..]);
    }

    public void WriteBlock(int block, ReadOnlySpan<byte> source)
    {
        int size = _disk.SectorSize;
        for (int i = 0; i < _sectorsPerBlock; i++)
            source.Slice(i * size, size).CopyTo(FileBytes.AsSpan(SectorOffset(block, i)));
    }

    private int SectorOffset(int block, int i)
    {
        if (block < 0 || block >= BlockCount)
            throw new ArgumentOutOfRangeException(nameof(block), $"ブロック {block} はディスクの範囲外です。");
        return _disk.SectorOffsets[_firstSector + block * _sectorsPerBlock + i];
    }
}
