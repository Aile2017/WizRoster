using System.Buffers.Binary;
using System.Text;
using WizRoster.Core.Disk;

namespace WizRoster.Core.Ucsd;

public sealed record UcsdFileEntry(string Name, int FirstBlock, int LastBlock, int Kind)
{
    public int BlockLength => LastBlock - FirstBlock;
}

/// <summary>
/// UCSD p-System のディレクトリ(ブロック 2〜5)。
/// 先頭 26 バイトがボリューム情報、以降 26 バイトずつファイルのエントリ。
/// </summary>
public sealed class UcsdDirectory
{
    private const int DirectoryBlock = 2;
    private const int EntrySize = 26;
    private const int MaxEntries = 77;

    private UcsdDirectory(string volumeName, IReadOnlyList<UcsdFileEntry> files)
    {
        VolumeName = volumeName;
        Files = files;
    }

    public string VolumeName { get; }
    public IReadOnlyList<UcsdFileEntry> Files { get; }

    public UcsdFileEntry? Find(string name) =>
        Files.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>ディレクトリとして正しく読めたときだけ結果を返す。</summary>
    public static UcsdDirectory? TryRead(IBlockImage image)
    {
        if (image.BlockCount < 6)
            return null;
        byte[] dir = image.ReadBytes(DirectoryBlock, 0, 4 * BlockImageExtensions.BlockSize);
        return TryParse(dir, image.BlockCount);
    }

    /// <summary>ディレクトリ 2048 バイトを解析する。</summary>
    public static UcsdDirectory? TryParse(ReadOnlySpan<byte> dir, int blockCount)
    {
        int firstBlock = BinaryPrimitives.ReadUInt16LittleEndian(dir);
        int lastBlock = BinaryPrimitives.ReadUInt16LittleEndian(dir[2..]);
        int kind = BinaryPrimitives.ReadUInt16LittleEndian(dir[4..]);
        int nameLen = dir[6];
        int volBlocks = BinaryPrimitives.ReadUInt16LittleEndian(dir[14..]);
        int numFiles = BinaryPrimitives.ReadUInt16LittleEndian(dir[16..]);

        if (firstBlock != 0 || lastBlock != 6 || (kind & 0x0F) != 0)
            return null;
        if (nameLen is < 1 or > 7 || numFiles > MaxEntries || volBlocks < 6)
            return null;
        if (!IsPrintable(dir.Slice(7, nameLen)))
            return null;

        string volumeName = Encoding.ASCII.GetString(dir.Slice(7, nameLen));
        var files = new List<UcsdFileEntry>(numFiles);
        for (int i = 1; i <= numFiles; i++)
        {
            var e = dir.Slice(i * EntrySize, EntrySize);
            int start = BinaryPrimitives.ReadUInt16LittleEndian(e);
            int end = BinaryPrimitives.ReadUInt16LittleEndian(e[2..]);
            int fkind = BinaryPrimitives.ReadUInt16LittleEndian(e[4..]) & 0x0F;
            int len = e[6];
            if (len is < 1 or > 15 || start < 6 || end < start || end > Math.Max(volBlocks, blockCount))
                return null;
            if (!IsPrintable(e.Slice(7, len)))
                return null;
            files.Add(new UcsdFileEntry(Encoding.ASCII.GetString(e.Slice(7, len)), start, end, fkind));
        }
        return new UcsdDirectory(volumeName, files);
    }

    private static bool IsPrintable(ReadOnlySpan<byte> s)
    {
        foreach (byte b in s)
            if (b is < 0x20 or > 0x7E)
                return false;
        return true;
    }
}
