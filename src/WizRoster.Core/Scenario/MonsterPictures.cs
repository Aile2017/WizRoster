using WizRoster.Core.Disk;
using WizRoster.Core.Ucsd;

namespace WizRoster.Core.Scenario;

/// <summary>モンスターの絵 1 枚。<see cref="Pixels"/> は色番号(0〜7、PC-98 のデジタル 8 色。ビット 0 = 青、1 = 赤、2 = 緑)。</summary>
public sealed record MonsterPicture(int Width, int Height, byte[] Pixels, int PixelAspect)
{
    /// <summary>PC-98 のデジタル 8 色(色番号 → RGB)。</summary>
    public static readonly (byte R, byte G, byte B)[] Palette =
    [
        (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255),
        (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255),
    ];
}

/// <summary>
/// アスキー移植版(PC-98・PC-8801)のモンスターの絵のファイル。
/// <list type="bullet">
/// <item><c>400.MONSTERS</c>: 1 枚 96×80 ドット。3072 バイトの区切りに 2880 バイト</item>
/// <item><c>200.MONSTERS</c>: 1 枚 96×40 ドット(縦長のドットで表示する)。1536 バイトの区切りに 1440 バイト</item>
/// </list>
/// 1 枚は 16 ドット幅のタイルを横 6 × 縦 5 に並べたもの(左上から横へ)。タイルはプレーン 0・1・2 の順に、
/// 1 行 2 バイト(左のドットが上位ビット)で並ぶ。区切りの残りは ff a5 の繰り返し。
/// モンスターの絵の番号 n(1 から)が n 枚目。
/// </summary>
public sealed class MonsterPictures
{
    private const int TilesAcross = 6;
    private const int TilesDown = 5;
    private const int Planes = 3;

    private readonly byte[] _data;
    private readonly int _slot;
    private readonly int _tileHeight;

    private MonsterPictures(string fileName, byte[] data, int slot, int tileHeight)
    {
        FileName = fileName;
        _data = data;
        _slot = slot;
        _tileHeight = tileHeight;
        Count = data.Length / slot;
    }

    /// <summary>読んだファイルの名前。</summary>
    public string FileName { get; }

    /// <summary>絵の枚数。</summary>
    public int Count { get; }

    private int DataLength => TilesAcross * TilesDown * Planes * _tileHeight * 2;

    /// <summary>同じボリュームから絵のファイルを探す。400 ライン用を優先する。形式が合わなければ null。</summary>
    public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image)
    {
        foreach (var (name, slot, tileHeight) in new[] { ("400.MONSTERS", 3072, 16), ("200.MONSTERS", 1536, 8) })
        {
            var entry = directory.Find(name);
            if (entry == null || entry.LastBlock > image.BlockCount)
                continue;
            byte[] data = image.ReadBytes(entry.FirstBlock, 0, entry.BlockLength * BlockImageExtensions.BlockSize);
            var pictures = new MonsterPictures(name, data, slot, tileHeight);
            if (pictures.LooksValid())
                return pictures;
        }
        return null;
    }

    /// <summary>
    /// 形式が合うか。各区切りの残りが ff a5 で埋まっていること(PC-DOS 版・MSX2 版の同名のファイルは別の形式)。
    /// </summary>
    private bool LooksValid()
    {
        if (Count == 0 || _data.Length % _slot != 0)
            return false;
        for (int k = 0; k < Count; k++)
        {
            for (int i = k * _slot + DataLength; i + 1 < (k + 1) * _slot; i += 2)
            {
                if (_data[i] != 0xFF || _data[i + 1] != 0xA5)
                    return false;
            }
        }
        return true;
    }

    /// <summary>絵の番号(1 から)の絵。範囲外なら null。</summary>
    public MonsterPicture? Get(int number)
    {
        int k = number - 1;
        if (k < 0 || k >= Count)
            return null;

        int width = TilesAcross * 16, height = TilesDown * _tileHeight;
        var pixels = new byte[width * height];
        int tileSize = Planes * _tileHeight * 2;
        for (int t = 0; t < TilesAcross * TilesDown; t++)
        {
            int tx = t % TilesAcross, ty = t / TilesAcross;
            for (int plane = 0; plane < Planes; plane++)
            {
                for (int row = 0; row < _tileHeight; row++)
                {
                    int o = k * _slot + t * tileSize + (plane * _tileHeight + row) * 2;
                    int bits = (_data[o] << 8) | _data[o + 1];
                    int y = ty * _tileHeight + row;
                    for (int b = 0; b < 16; b++)
                    {
                        if ((bits & (0x8000 >> b)) != 0)
                            pixels[y * width + tx * 16 + b] |= (byte)(1 << plane);
                    }
                }
            }
        }
        return new MonsterPicture(width, height, pixels, _tileHeight == 8 ? 2 : 1);
    }
}
