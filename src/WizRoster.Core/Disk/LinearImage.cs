namespace WizRoster.Core.Disk;

/// <summary>
/// ブロックがファイル内に連続して並んでいるイメージ。
/// ブロック b の位置は <c>BaseOffset + b * 512</c>。
/// PC-98 FDI(ヘッダとシリンダ 0 を飛ばす)、PC-DOS の生イメージ、Apple II の ProDOS 順イメージなど。
/// </summary>
public sealed class LinearImage : IBlockImage
{
    public LinearImage(byte[] fileBytes, long baseOffset, string formatName)
    {
        if (baseOffset < 0 || baseOffset > fileBytes.Length)
            throw new ArgumentOutOfRangeException(nameof(baseOffset));
        FileBytes = fileBytes;
        BaseOffset = baseOffset;
        FormatName = formatName;
    }

    public string FormatName { get; }
    public byte[] FileBytes { get; }
    public long BaseOffset { get; }
    public int BlockCount => (int)((FileBytes.Length - BaseOffset) / BlockImageExtensions.BlockSize);

    public void ReadBlock(int block, Span<byte> destination)
    {
        CheckBlock(block);
        FileBytes.AsSpan(Offset(block), BlockImageExtensions.BlockSize).CopyTo(destination);
    }

    public void WriteBlock(int block, ReadOnlySpan<byte> source)
    {
        CheckBlock(block);
        source[..BlockImageExtensions.BlockSize].CopyTo(FileBytes.AsSpan(Offset(block)));
    }

    private int Offset(int block) => checked((int)(BaseOffset + (long)block * BlockImageExtensions.BlockSize));

    private void CheckBlock(int block)
    {
        if (block < 0 || block >= BlockCount)
            throw new ArgumentOutOfRangeException(nameof(block), $"ブロック {block} はイメージの範囲外です。");
    }
}
