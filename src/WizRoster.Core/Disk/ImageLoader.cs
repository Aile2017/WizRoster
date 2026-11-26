using System.Buffers.Binary;
using System.Text;
using WizRoster.Core.Ucsd;

namespace WizRoster.Core.Disk;

/// <summary>
/// ファイルの形式を判別し、SCENARIO.DATA を含む UCSD ボリュームとして開ける読み方を探す。
/// </summary>
public static class ImageLoader
{
    public const string ScenarioFileName = "SCENARIO.DATA";

    public sealed record OpenResult(IBlockImage Image, UcsdDirectory Directory, UcsdFileEntry ScenarioEntry);

    public const string NotFoundMessage =
        "SCENARIO.DATA を含む UCSD p-System ボリュームが見つかりませんでした。" +
        "キャラクタの入ったディスク(シナリオ/ユーザーディスク)を指定してください。";

    public static OpenResult Open(string path) => Open(File.ReadAllBytes(path));

    public static OpenResult Open(byte[] bytes) =>
        OpenCandidates(bytes).FirstOrDefault() ?? throw new InvalidDataException(NotFoundMessage);

    /// <summary>
    /// SCENARIO.DATA を含むボリュームとして読める候補を、確からしい順に返す。
    /// 古いディレクトリの写しが残っているディスク(MSX2 版など)があるので、
    /// 呼び出し側は中身まで読めた候補を採用すること。
    /// </summary>
    public static IEnumerable<OpenResult> OpenCandidates(byte[] bytes)
    {
        foreach (var image in Candidates(bytes))
        {
            var dir = UcsdDirectory.TryRead(image);
            var entry = dir?.Find(ScenarioFileName);
            if (dir != null && entry != null && entry.LastBlock <= image.BlockCount)
                yield return new OpenResult(image, dir, entry);
        }
    }

    private static IEnumerable<IBlockImage> Candidates(byte[] bytes)
    {
        if (TryFdiHeader(bytes, out int headerSize, out int cylinderBytes))
        {
            // PC-98 版: ブートセクタがシリンダ 0 を飛ばしてブロックを数える。
            yield return new LinearImage(bytes, headerSize + cylinderBytes, "PC-98 FDI");
            yield return new LinearImage(bytes, headerSize, "FDI");
        }

        if (bytes.Length == AppleDosOrderImage.ImageSize)
        {
            yield return new AppleDosOrderImage(bytes);
            yield return new LinearImage(bytes, 0, "Apple II (ProDOS 順 .po)");
        }

        yield return new LinearImage(bytes, 0, "生イメージ");

        // ブロックが連続していれば、ディレクトリ中のファイル名から先頭位置を逆算できる。
        foreach (long baseOffset in ScanDirectoryBases(bytes))
            yield return new LinearImage(bytes, baseOffset, $"生イメージ (先頭 {baseOffset} バイト)");
    }

    /// <summary>FDI ヘッダ: +8 ヘッダサイズ, +12 データサイズ, +16 セクタ長, +20 セクタ数, +24 面数, +28 シリンダ数。</summary>
    private static bool TryFdiHeader(byte[] bytes, out int headerSize, out int cylinderBytes)
    {
        headerSize = cylinderBytes = 0;
        if (bytes.Length < 32)
            return false;
        var h = bytes.AsSpan();
        long hdr = BinaryPrimitives.ReadUInt32LittleEndian(h[8..]);
        long data = BinaryPrimitives.ReadUInt32LittleEndian(h[12..]);
        long sectorSize = BinaryPrimitives.ReadUInt32LittleEndian(h[16..]);
        long sectors = BinaryPrimitives.ReadUInt32LittleEndian(h[20..]);
        long surfaces = BinaryPrimitives.ReadUInt32LittleEndian(h[24..]);
        long cylinders = BinaryPrimitives.ReadUInt32LittleEndian(h[28..]);
        if (hdr < 32 || hdr > 65536 || sectorSize is 0 or > 4096 || sectors is 0 or > 64 || surfaces is 0 or > 2 || cylinders is 0 or > 100)
            return false;
        if (hdr + data != bytes.Length || sectorSize * sectors * surfaces * cylinders != data)
            return false;
        headerSize = (int)hdr;
        cylinderBytes = (int)(sectorSize * sectors * surfaces);
        return true;
    }

    private static IEnumerable<long> ScanDirectoryBases(byte[] bytes)
    {
        byte[] needle = [(byte)ScenarioFileName.Length, .. Encoding.ASCII.GetBytes(ScenarioFileName)];
        var seen = new HashSet<long>();
        int from = 0;
        while (true)
        {
            int p = bytes.AsSpan(from).IndexOf(needle);
            if (p < 0)
                yield break;
            p += from;
            from = p + 1;
            int entryStart = p - 6;
            for (int i = 1; i <= 77; i++)
            {
                long baseOffset = entryStart - 26L * i - 2L * BlockImageExtensions.BlockSize;
                if (baseOffset < 0)
                    break;
                long dirOffset = baseOffset + 2L * BlockImageExtensions.BlockSize;
                if (dirOffset + 2048 > bytes.Length || !seen.Add(baseOffset))
                    continue;
                if (UcsdDirectory.TryParse(bytes.AsSpan((int)dirOffset, 2048), int.MaxValue) != null)
                    yield return baseOffset;
            }
        }
    }
}
