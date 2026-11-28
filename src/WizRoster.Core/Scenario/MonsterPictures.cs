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
/// <item>PC-DOS 版: <c>200.MONSTERS</c>。1 枚 512 バイト、48×40 ドットの CGA 4 色。絵の番号 n(1 から)が n 枚目</item>
/// <item>Apple II 版: SCENARIO.DATA の区分 6。1 枚 512 バイトの高解像度の絵(#1・#2 は 70×50、#3 は文字で組んだ 70×48)。絵の番号 n(0 から)が n 件目</item>
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
        ?? (game.Platform == "MSX2" ? Msx4Bit.Load(directory, image, game.Scenario) : null)
        ?? (game.Platform == "PC-DOS" ? CgaCells.Load(directory, image) : null)
        ?? (game.Platform == "Apple II" ? AppleHires.Load(scenario, game.Scenario) : null);

    // ---- PC-98・PC-8801 版 ----

    /// <summary>
    /// 16 ドット幅のタイルを横 6 × 縦 5 に並べた絵(左上から横へ)。タイルはプレーン 0・1・2 の順に、
    /// 1 行 2 バイト(左のドットが上位ビット)。1 枚ごとの区切りの残りは ff a5 の繰り返し。
    /// プレーン 0・1・2 を画面の青・赤・緑のプレーンに書く(PC-98 の SYSTEM.INTERP $1358)。
    /// 色はデジタル 8 色だが、ゲームが色 1 と 7 を入れ替えている(色 1 = 白、色 7 = 青)。
    /// PC-98 は I/O A8h〜AEh に 31h・75h・26h・04h、PC-8801 は OUT (55h),7・OUT (5Bh),1。
    /// </summary>
    private static class TileFile
    {
        private const int TilesAcross = 6;
        private const int TilesDown = 5;
        private const int Planes = 3;

        private static readonly (byte, byte, byte)[] Palette =
        [
            (0, 0, 0), (255, 255, 255), (255, 0, 0), (255, 0, 255),
            (0, 255, 0), (0, 255, 255), (255, 255, 0), (0, 0, 255),
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
    /// 色はゲームが設定するパレット(ゲームディスクのプログラム部分にある 32 バイトの表。#1 は 0x6420 バイト目)。
    /// #2・#3 は色 1 と 4 だけが #1 と違う。縦長のドットとして縦を 2 倍で出す。
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

        // ゲームが設定するパレット(R, G, B 各 0〜7)。実機の画面と色が一致することを確かめた(#1 の KOBOLD)
        private static readonly (byte, byte, byte)[] Palette1 = Expand(
        [
            (0, 0, 0), (0, 0, 0), (4, 0, 0), (0, 0, 4), (0, 0, 0), (0, 4, 0), (4, 4, 0), (0, 4, 4),
            (4, 4, 4), (6, 0, 0), (0, 0, 6), (6, 0, 6), (0, 6, 0), (6, 6, 0), (0, 6, 6), (7, 7, 7),
        ]);

        // #2・#3: 色 1 が暗い灰色、色 4 が暗い紫
        private static readonly (byte, byte, byte)[] Palette23 = Expand(
        [
            (0, 0, 0), (2, 2, 2), (4, 0, 0), (0, 0, 4), (4, 0, 4), (0, 4, 0), (4, 4, 0), (0, 4, 4),
            (4, 4, 4), (6, 0, 0), (0, 0, 6), (6, 0, 6), (0, 6, 0), (6, 6, 0), (0, 6, 6), (7, 7, 7),
        ]);

        private static (byte, byte, byte)[] Expand((int R, int G, int B)[] colors) =>
            colors.Select(c => ((byte)(c.R * 255 / 7), (byte)(c.G * 255 / 7), (byte)(c.B * 255 / 7))).ToArray();

        public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image, int? scenarioNumber)
        {
            var palette = scenarioNumber is 2 or 3 ? Palette23 : Palette1;
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
            return new MonsterPictures(FileName, data.Length / Slot, 1, k => Decode(data, k * Slot, palette));
        }

        private static MonsterPicture Decode(byte[] data, int start, (byte, byte, byte)[] palette)
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
            return new MonsterPicture(Width, Height, pixels, palette, 2);
        }
    }

    // ---- PC-DOS 版 ----

    /// <summary>
    /// <c>200.MONSTERS</c>。1 ブロック(512 バイト)に 1 枚で、絵の番号 n が n − 1 ブロック目
    /// (SYSTEM.INTERP の画面ドライバが 1 枚ずつ 1 ブロックを読む)。
    /// 1 枚は 8×8 ドットのセルを横 6 × 縦 5 に並べたもの(左上から横へ)。1 セル 16 バイトで、1 行 2 バイト。
    /// 1 バイトに 4 ドット(上位 2 ビットが左)の CGA 4 色。30 セル(480 バイト)のあとの 32 バイトは使わない。
    /// 色は CGA のパレット 1(黒・シアン・マゼンタ・白。ドライバが INT 10h AH=0Bh BX=0101h で選ぶ)。
    /// </summary>
    private static class CgaCells
    {
        private const string FileName = "200.MONSTERS";
        private const int Slot = 512;
        private const int CellsAcross = 6;
        private const int CellsDown = 5;
        private const int CellBytes = 16;
        private const int Width = CellsAcross * 8;
        private const int Height = CellsDown * 8;

        private static readonly (byte, byte, byte)[] Palette =
        [
            (0, 0, 0), (85, 255, 255), (255, 85, 255), (255, 255, 255),
        ];

        public static MonsterPictures? Load(UcsdDirectory directory, IBlockImage image)
        {
            var entry = directory.Find(FileName);
            if (entry == null || entry.LastBlock > image.BlockCount)
                return null;
            byte[] data = image.ReadBytes(entry.FirstBlock, 0, entry.BlockLength * BlockImageExtensions.BlockSize);
            if (data.Length == 0 || data.Length % Slot != 0)
                return null;
            return new MonsterPictures(FileName, data.Length / Slot, 1, k => Decode(data, k * Slot));
        }

        private static MonsterPicture Decode(byte[] data, int start)
        {
            var pixels = new byte[Width * Height];
            for (int c = 0; c < CellsAcross * CellsDown; c++)
            {
                int cx = c % CellsAcross, cy = c / CellsAcross;
                for (int row = 0; row < 8; row++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        byte b = data[start + c * CellBytes + row * 2 + x / 4];
                        pixels[(cy * 8 + row) * Width + cx * 8 + x] = (byte)((b >> (6 - 2 * (x % 4))) & 3);
                    }
                }
            }
            return new MonsterPicture(Width, Height, pixels, Palette, 1);
        }
    }

    // ---- Apple II 版 ----

    /// <summary>
    /// SCENARIO.DATA の区分 6(ZSPCCHRS)。1 件 512 バイトの高解像度の絵。1 バイト 7 ドット(下位ビットが左)、
    /// 最上位ビットは色の組(0 = 紫・緑、1 = 青・橙)。
    /// <list type="bullet">
    /// <item>#1・#2: 1 行 10 バイト × 50 行(70×50 ドット)。残り 12 バイトは 0。戦闘の始めに高解像度画面へそのまま描く(CINIT p5)</item>
    /// <item>#3: 文字 60 個分(1 文字 7×8 ドット = 8 バイト)。横 10 × 縦 6 文字に並べて 70×48 ドット。
    /// 512 バイトを文字の形として $0A00 に写し、画面の (13, 9) から文字コード 192 から順に書く(COMBAT p1・p3)。
    /// 文字の 8 バイトは下の行から並ぶ(文字を描く WIZARDRY p36 が 8 バイト目から逆順に上の行へ書く)</item>
    /// </list>
    /// 色は NTSC の擬似カラーをまねる: 隣と続くドットは白、単独のドットは列の偶奇と色の組で決まる色、
    /// 同じ組の色ドットに挟まれた 1 ドットの隙間はその色で埋める。偶奇は絵が自然な色になる向き
    /// (#1・#2 はスライムが緑、#3 は大蛇が緑)に合わせた。
    /// </summary>
    private static class AppleHires
    {
        private const int RecordSize = 512;
        private const int BytesPerRow = 10;
        private const int Width = BytesPerRow * 7;
        private const int LinearHeight = 50;
        private const int CellRows = 6;
        private const int CellHeight = 8;

        private const byte Black = 0, White = 1, Violet = 2, Green = 3, Blue = 4, Orange = 5;

        private static readonly (byte, byte, byte)[] Palette =
        [
            (0, 0, 0), (255, 255, 255), (255, 68, 253), (20, 245, 60), (20, 207, 253), (255, 106, 60),
        ];

        public static MonsterPictures? Load(ScenarioFile scenario, int? scenarioNumber)
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
            if (records.Count == 0)
                return null;

            if (scenarioNumber == 3)
                return new MonsterPictures("SCENARIO.DATA 区分 6", records.Count, 0, k => Decode(FromCells(records[k]), CellRows * CellHeight, 1));

            // 50 行(500 バイト)のあとの 12 バイトは 0(#3 の文字の形式ではこれが合わない)
            if (records.Any(r => r.AsSpan(LinearHeight * BytesPerRow).ContainsAnyExcept((byte)0)))
                return null;
            return new MonsterPictures("SCENARIO.DATA 区分 6", records.Count, 0, k => Decode(records[k], LinearHeight, 1));
        }

        /// <summary>#3 の文字 60 個(文字コード順、1 文字 8 バイトで下の行から)を、上から 1 行 10 バイトの並びに直す。</summary>
        private static byte[] FromCells(byte[] r)
        {
            var rows = new byte[CellRows * CellHeight * BytesPerRow];
            for (int ch = 0; ch < CellRows * BytesPerRow; ch++)
            {
                int cx = ch % BytesPerRow, cy = ch / BytesPerRow;
                for (int j = 0; j < CellHeight; j++)
                    rows[(cy * CellHeight + j) * BytesPerRow + cx] = r[ch * CellHeight + CellHeight - 1 - j];
            }
            return rows;
        }

        private static MonsterPicture Decode(byte[] r, int height, int phase)
        {
            var pixels = new byte[Width * height];
            var on = new bool[Width];
            var group = new int[Width];
            for (int y = 0; y < height; y++)
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
                        c = left || right ? White : ColorOf(x, group[x], phase);
                    else if (left && right && group[x - 1] == group[x + 1])
                        c = ColorOf(x - 1, group[x - 1], phase);
                    else
                        c = Black;
                    pixels[y * Width + x] = c;
                }
            }
            return new MonsterPicture(Width, height, pixels, Palette, 1);
        }

        private static byte ColorOf(int x, int group, int phase) =>
            (x + phase) % 2 == 0
                ? (group == 0 ? Violet : Blue)
                : (group == 0 ? Green : Orange);
    }
}
