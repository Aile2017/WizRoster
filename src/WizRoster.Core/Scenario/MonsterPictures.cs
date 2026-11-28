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
/// <item>MSX2 版: <c>200.MONSTERS</c>。1 枚 72×40 ドット、16 色(1 ドット 4 ビット)。絵の番号 n(1 から)が n 枚目</item>
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
        TileFile.Load(directory, image)
        ?? (game.Platform == "MSX2" ? Msx4Bit.Load(directory, image) : null)
        ?? (game.Platform == "Apple II" ? AppleHires.Load(scenario) : null);

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

    // ---- MSX2 版 ----

    /// <summary>
    /// <c>200.MONSTERS</c>(61440 バイト)。2048 バイトの区切りに 1 行 48 バイト × 40 行。
    /// 1 行は 8 バイトずつ 6 組で、各組の先頭 6 バイトが 12 ドット(SCREEN 5 形式。1 バイトに 2 ドット、上位 4 ビットが左)、
    /// 残り 2 バイトは 0。つまり 1 枚 72×40 ドット。区切りの残り 128 バイトも 0。
    /// 色は MSX2 の標準パレット(ゲームがパレットを変えているかは確かめていない)。縦長のドットとして縦を 2 倍で出す。
    /// </summary>
    private static class Msx4Bit
    {
        private const string FileName = "200.MONSTERS";
        private const int Slot = 2048;
        private const int BytesPerRow = 48;
        private const int Groups = 6;
        private const int GroupStride = 8;
        private const int GroupBytes = 6;
        private const int Width = Groups * GroupBytes * 2;
        private const int Height = 40;
        private const int MaxStrayGroups = 8;

        // MSX2 の標準パレット(各色 0〜7 を 0〜255 に広げる)
        private static readonly (byte, byte, byte)[] Palette =
            new (int R, int G, int B)[]
            {
                (0, 0, 0), (0, 0, 0), (1, 6, 1), (3, 7, 3), (1, 1, 7), (2, 3, 7), (5, 1, 1), (2, 6, 7),
                (7, 1, 1), (7, 3, 3), (6, 6, 1), (6, 6, 4), (1, 4, 1), (6, 2, 5), (5, 5, 5), (7, 7, 7),
            }.Select(c => ((byte)(c.R * 255 / 7), (byte)(c.G * 255 / 7), (byte)(c.B * 255 / 7))).ToArray();

        public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image)
        {
            var entry = directory.Find(FileName);
            if (entry == null || entry.LastBlock > image.BlockCount)
                return null;
            byte[] data = image.ReadBytes(entry.FirstBlock, 0, entry.BlockLength * BlockImageExtensions.BlockSize);
            if (data.Length == 0 || data.Length % Slot != 0)
                return null;
            for (int start = 0; start < data.Length; start += Slot)
            {
                var picture = data.AsSpan(start, BytesPerRow * Height);
                var rest = data.AsSpan(start + BytesPerRow * Height, Slot - BytesPerRow * Height);
                if (rest.ContainsAnyExcept((byte)0) || !picture.ContainsAnyExcept((byte)0))
                    return null;
                // 各組の残り 2 バイトはほぼ 0(#1 の User Disk の 1 枚だけ、数バイト 0 でなかった)
                int stray = 0;
                for (int i = GroupBytes; i < picture.Length; i += GroupStride)
                {
                    if (picture[i] != 0 || picture[i + 1] != 0)
                        stray++;
                }
                if (stray > MaxStrayGroups)
                    return null;
            }
            return new MonsterPictures(FileName, data.Length / Slot, 1, k => Decode(data, k * Slot));
        }

        private static MonsterPicture Decode(byte[] data, int start)
        {
            var pixels = new byte[Width * Height];
            int p = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int g = 0; g < Groups; g++)
                {
                    for (int i = 0; i < GroupBytes; i++)
                    {
                        byte b = data[start + y * BytesPerRow + g * GroupStride + i];
                        pixels[p++] = (byte)(b >> 4);
                        pixels[p++] = (byte)(b & 0x0F);
                    }
                }
            }
            return new MonsterPicture(Width, Height, pixels, Palette, 2);
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
