namespace WizRoster.Core.Disk;

/// <summary>
/// UCSD p-System の 512 バイトブロック単位でディスクイメージを読み書きする。
/// 機種ごとのイメージ形式(FDI, 生イメージ, Apple II DOS 順など)の違いはここで吸収する。
/// </summary>
public interface IBlockImage
{
    /// <summary>形式の説明(画面表示用)。</summary>
    string FormatName { get; }

    /// <summary>イメージファイル全体のバイト列。書き込みはこの配列に反映される。</summary>
    byte[] FileBytes { get; }

    /// <summary>扱えるブロック数。</summary>
    int BlockCount { get; }

    void ReadBlock(int block, Span<byte> destination);

    void WriteBlock(int block, ReadOnlySpan<byte> source);
}

public static class BlockImageExtensions
{
    public const int BlockSize = 512;

    /// <summary>ブロック <paramref name="startBlock"/> の先頭から <paramref name="offset"/> バイト目以降を読む。</summary>
    public static byte[] ReadBytes(this IBlockImage image, int startBlock, int offset, int length)
    {
        var result = new byte[length];
        var buf = new byte[BlockSize];
        int done = 0;
        while (done < length)
        {
            int pos = offset + done;
            int block = startBlock + pos / BlockSize;
            int inBlock = pos % BlockSize;
            int n = Math.Min(BlockSize - inBlock, length - done);
            image.ReadBlock(block, buf);
            buf.AsSpan(inBlock, n).CopyTo(result.AsSpan(done));
            done += n;
        }
        return result;
    }

    public static void WriteBytes(this IBlockImage image, int startBlock, int offset, ReadOnlySpan<byte> data)
    {
        var buf = new byte[BlockSize];
        int done = 0;
        while (done < data.Length)
        {
            int pos = offset + done;
            int block = startBlock + pos / BlockSize;
            int inBlock = pos % BlockSize;
            int n = Math.Min(BlockSize - inBlock, data.Length - done);
            image.ReadBlock(block, buf);
            data.Slice(done, n).CopyTo(buf.AsSpan(inBlock));
            image.WriteBlock(block, buf);
            done += n;
        }
    }
}
