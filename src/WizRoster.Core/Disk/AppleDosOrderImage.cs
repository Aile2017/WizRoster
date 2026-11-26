namespace WizRoster.Core.Disk;

/// <summary>
/// Apple II の 140KB イメージ(DOS 3.3 のセクタ順, .dsk/.do)。
/// p-System のブロック b はトラック b/8 の、ProDOS 論理セクタ 2k と 2k+1 (k = b%8) に入っている。
/// ProDOS 論理セクタを DOS 3.3 論理セクタに変換してから読み書きする。
/// </summary>
public sealed class AppleDosOrderImage : IBlockImage
{
    public const int ImageSize = 35 * 16 * 256;

    private static readonly int[] ProDosToDos = [0, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 15];

    public AppleDosOrderImage(byte[] fileBytes)
    {
        if (fileBytes.Length != ImageSize)
            throw new ArgumentException("Apple II 140KB イメージではありません。", nameof(fileBytes));
        FileBytes = fileBytes;
    }

    public string FormatName => "Apple II (DOS 3.3 順 .dsk)";
    public byte[] FileBytes { get; }
    public int BlockCount => 280;

    public void ReadBlock(int block, Span<byte> destination)
    {
        var (a, b) = SectorOffsets(block);
        FileBytes.AsSpan(a, 256).CopyTo(destination);
        FileBytes.AsSpan(b, 256).CopyTo(destination[256..]);
    }

    public void WriteBlock(int block, ReadOnlySpan<byte> source)
    {
        var (a, b) = SectorOffsets(block);
        source[..256].CopyTo(FileBytes.AsSpan(a));
        source.Slice(256, 256).CopyTo(FileBytes.AsSpan(b));
    }

    private (int, int) SectorOffsets(int block)
    {
        if (block < 0 || block >= BlockCount)
            throw new ArgumentOutOfRangeException(nameof(block));
        int track = block / 8;
        int k = block % 8;
        return ((track * 16 + ProDosToDos[2 * k]) * 256, (track * 16 + ProDosToDos[2 * k + 1]) * 256);
    }
}
