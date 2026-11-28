using WizRoster.Core.Disk;
using WizRoster.Core.Ucsd;

namespace WizRoster.Core.Scenario;

/// <summary>
/// モンスターの絵 1 枚。<see cref="Pixels"/> は <see cref="Palette"/> の色番号。
/// <see cref="PixelAspect"/> はドットの縦横比(縦長のドットなら 2。表示では縦をこの倍にする)。
/// </summary>
public sealed record MonsterPicture(int Width, int Height, byte[] Pixels, (byte R, byte G, byte B)[] Palette, int PixelAspect);

/// <summary>
/// モンスターの絵。機種ごとに置き場所と形式が違う(docs/analysis/08-monsters.md)。
/// <list type="bullet">
/// <item>PC-98・PC-8801 版: <c>400.MONSTERS</c>(96×80)/ <c>200.MONSTERS</c>(96×40)。絵の番号 n(1 から)が n 枚目</item>
/// <item>Apple II 版: SCENARIO.DATA の区分 6。1 枚 512 バイトの高解像度の絵(70×50)。絵の番号 n(0 から)が n 件目</item>
/// </list>
/// </summary>
public sealed class MonsterPictures
{
    private readonly Func<int, MonsterPicture> _decode;

    private MonsterPictures(string source, int count, int firstNumber, Func<int, MonsterPicture> decode)
    {
        Source = source;
        Count = count;
        FirstNumber = firstNumber;
        _decode = decode;
    }

    /// <summary>絵の置き場所(画面表示用)。例: 「400.MONSTERS」「SCENARIO.DATA 区分 6」。</summary>
    public string Source { get; }

    /// <summary>絵の枚数。</summary>
    public int Count { get; }

    /// <summary>最初の絵の番号(PC-98 系は 1、Apple II は 0)。</summary>
    public int FirstNumber { get; }

    /// <summary>絵の番号の絵。範囲外なら null。</summary>
    public MonsterPicture? Get(int number)
    {
        int k = number - FirstNumber;
        return k >= 0 && k < Count ? _decode(k) : null;
    }

    /// <summary>開いたディスクから絵を探す。読める形式がなければ null。</summary>
    public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image, ScenarioFile scenario, GameInfo game) =>
        TileFile.Load(directory, image) ?? (game.Platform == "Apple II" ? AppleHires.Load(scenario) : null);

    // ---- PC-98・PC-8801 版 ----

    /// <summary>
    /// 16 ドット幅のタイルを横 6 × 縦 5 に並べた絵(左上から横へ)。タイルはプレーン 0・1・2 の順に、
    /// 1 行 2 バイト(左のドットが上位ビット)。1 枚ごとの区切りの残りは ff a5 の繰り返し。
    /// 色はデジタル 8 色(ビット 0 = 青、1 = 赤、2 = 緑)。
    /// </summary>
    private static class TileFile
    {
        private const int TilesAcross = 6;
        private const int TilesDown = 5;
        private const int Planes = 3;

        private static readonly (byte, byte, byte)[] Palette =
        [
            (0, 0, 0), (0, 0, 255), (255, 0, 0), (255, 0, 255),
            (0, 255, 0), (0, 255, 255), (255, 255, 0), (255, 255, 255),
        ];

        public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image)
        {
            foreach (var (name, slot, tileHeight) in new[] { ("400.MONSTERS", 3072, 16), ("200.MONSTERS", 1536, 8) })
            {
                var entry = directory.Find(name);
                if (entry == null || entry.LastBlock > image.BlockCount)
                    continue;
                byte[] data = image.ReadBytes(entry.FirstBlock, 0, entry.BlockLength * BlockImageExtensions.BlockSize);
                if (IsValid(data, slot, tileHeight))
                    return new MonsterPictures(name, data.Length / slot, 1, k => Decode(data, k * slot, tileHeight));
            }
            return null;
        }

        private static int DataLength(int tileHeight) => TilesAcross * TilesDown * Planes * tileHeight * 2;

        /// <summary>各区切りの残りが ff a5 で埋まっていること(PC-DOS 版・MSX2 版の同名のファイルは別の形式)。</summary>
        private static bool IsValid(byte[] data, int slot, int tileHeight)
        {
            if (data.Length == 0 || data.Length % slot != 0)
                return false;
            for (int start = 0; start < data.Length; start += slot)
            {
                for (int i = start + DataLength(tileHeight); i + 1 < start + slot; i += 2)
                {
                    if (data[i] != 0xFF || data[i + 1] != 0xA5)
                        return false;
                }
            }
            return true;
        }

        private static MonsterPicture Decode(byte[] data, int start, int tileHeight)
        {
            int width = TilesAcross * 16, height = TilesDown * tileHeight;
            var pixels = new byte[width * height];
            int tileSize = Planes * tileHeight * 2;
            for (int t = 0; t < TilesAcross * TilesDown; t++)
            {
                int tx = t % TilesAcross, ty = t / TilesAcross;
                for (int plane = 0; plane < Planes; plane++)
                {
                    for (int row = 0; row < tileHeight; row++)
                    {
                        int o = start + t * tileSize + (plane * tileHeight + row) * 2;
                        int bits = (data[o] << 8) | data[o + 1];
                        int y = ty * tileHeight + row;
                        for (int b = 0; b < 16; b++)
                        {
                            if ((bits & (0x8000 >> b)) != 0)
                                pixels[y * width + tx * 16 + b] |= (byte)(1 << plane);
                        }
                    }
                }
            }
            return new MonsterPicture(width, height, pixels, Palette, tileHeight == 8 ? 2 : 1);
        }
    }

    // ---- Apple II 版 ----

    /// <summary>
    /// SCENARIO.DATA の区分 6(ZSPCCHRS)。1 件 512 バイトに、1 行 10 バイト × 50 行の高解像度の絵(70×50 ドット)。
    /// 1 バイト 7 ドット(下位ビットが左)、最上位ビットは色の組(0 = 紫・緑、1 = 青・橙)。
    /// 色は NTSC の擬似カラーをまねる: 隣と続くドットは白、単独のドットは列の偶奇と色の組で決まる色、
    /// 同じ組の色ドットに挟まれた 1 ドットの隙間はその色で埋める。偶奇は絵が自然な色になる向き(スライムが緑)に合わせた。
    /// </summary>
    private static class AppleHires
    {
        private const int RecordSize = 512;
        private const int BytesPerRow = 10;
        private const int Width = BytesPerRow * 7;
        private const int Height = 50;
        private const int Phase = 1;

        private const byte Black = 0, White = 1, Violet = 2, Green = 3, Blue = 4, Orange = 5;

        private static readonly (byte, byte, byte)[] Palette =
        [
            (0, 0, 0), (255, 255, 255), (255, 68, 253), (20, 245, 60), (20, 207, 253), (255, 106, 60),
        ];

        public static MonsterPictures? Load(ScenarioFile scenario)
        {
            int z = (int)Zone.SpecialCharacter;
            int count = scenario.RecordCounts[z];
            if (scenario.RecordsPerPage[z] != 2 || count is <= 0 or > 100)
                return null;
            var records = new List<byte[]>();
            for (int i = 0; i < count; i++)
            {
                int offset = scenario.RecordOffset(Zone.SpecialCharacter, i, RecordSize);
                if (offset + RecordSize > scenario.ByteLength)
                    break;
                records.Add(scenario.Read(offset, RecordSize));
            }
            // 50 行(500 バイト)のあとの 12 バイトは 0。#3 の区分 6 は別の形式(圧縮されているらしい)でこれが合わない
            if (records.Count == 0 || records.Any(r => r.AsSpan(Height * BytesPerRow).ContainsAnyExcept((byte)0)))
                return null;
            return new MonsterPictures("SCENARIO.DATA 区分 6", records.Count, 0, k => Decode(records[k]));
        }

        private static MonsterPicture Decode(byte[] r)
        {
            var pixels = new byte[Width * Height];
            var on = new bool[Width];
            var group = new int[Width];
            for (int y = 0; y < Height; y++)
            {
                for (int xb = 0; xb < BytesPerRow; xb++)
                {
                    int b = r[y * BytesPerRow + xb];
                    for (int bit = 0; bit < 7; bit++)
                    {
                        on[xb * 7 + bit] = (b & (1 << bit)) != 0;
                        group[xb * 7 + bit] = b >> 7;
                    }
                }
                for (int x = 0; x < Width; x++)
                {
                    bool left = x > 0 && on[x - 1], right = x < Width - 1 && on[x + 1];
                    byte c;
                    if (on[x])
                        c = left || right ? White : ColorOf(x, group[x]);
                    else if (left && right && group[x - 1] == group[x + 1])
                        c = ColorOf(x - 1, group[x - 1]);
                    else
                        c = Black;
                    pixels[y * Width + x] = c;
                }
            }
            return new MonsterPicture(Width, Height, pixels, Palette, 1);
        }

        private static byte ColorOf(int x, int group) =>
            (x + Phase) % 2 == 0
                ? (group == 0 ? Violet : Blue)
                : (group == 0 ? Green : Orange);
    }
}
