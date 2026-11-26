using System.Text;
using WizRoster.Core.Disk;
using WizRoster.Core.Krn;
using WizRoster.Core.Model;
using WizRoster.Core.Ucsd;

namespace WizRoster.Core.Scenario;

/// <summary>データの区分(TZSCN)。目次の配列の添字。</summary>
public enum Zone
{
    Zero = 0,
    Maze = 1,
    Enemy = 2,
    Reward = 3,
    Item = 4,
    Character = 5,
    SpecialCharacter = 6,
    Experience = 7,
}

/// <summary>
/// SCENARIO.DATA。先頭ブロックの目次(TSCNTOC)からデータの位置を求める。
/// <list type="bullet">
/// <item>+0 シナリオ名 STRING[40]</item>
/// <item>+42 RECPER2B[8]: 1024 バイトあたりの件数</item>
/// <item>+58 RECPERDK[8]: 件数</item>
/// <item>+74 UNUSEDXX[8]: PC-DOS 版では 1 件のバイト数(キャラクタが 208 のときだけ使う)</item>
/// <item>+90 BLOFF[8]: ファイル先頭からの開始ブロック</item>
/// </list>
/// アイテム表に名前がない版(PC-DOS)では、アイテム名と呪文名を ASCII.KRN から読む。
/// </summary>
public sealed class ScenarioFile
{
    private const int ZoneCount = 8;
    private const int PageSize = 1024;

    // ASCII.KRN のメッセージ番号
    private const int KrnFirstSpell = 5001;
    private const int KrnItemBase = 14000; // 14000+2n = 未鑑定名, 14000+2n+1 = 名前

    private readonly IBlockImage _image;
    private readonly int _startBlock;

    public ScenarioFile(IBlockImage image, UcsdFileEntry entry, KrnMessageFile? messages = null)
    {
        _image = image;
        _startBlock = entry.FirstBlock;
        ByteLength = entry.BlockLength * BlockImageExtensions.BlockSize;

        byte[] toc = Read(0, BlockImageExtensions.BlockSize);
        Title = WizBytes.PascalString(toc, 0, 40);
        RecordsPerPage = ReadArray(toc, 42);
        RecordCounts = ReadArray(toc, 58);
        BlockOffsets = ReadArray(toc, 90);

        int blockCount = ByteLength / BlockImageExtensions.BlockSize;
        if (RecordsPerPage[(int)Zone.Character] <= 0 || RecordCounts[(int)Zone.Character] is <= 0 or > 100
            || RecordsPerPage[(int)Zone.Item] <= 0 || RecordCounts[(int)Zone.Item] is <= 0 or > 1000
            || BlockOffsets.Any(b => b < 0 || b >= blockCount))
        {
            throw new InvalidDataException("SCENARIO.DATA の目次が読めません。");
        }

        var sizes = ReadArray(toc, 74);
        ItemRecordSize = sizes[(int)Zone.Character] == Character.RecordSize && sizes[(int)Zone.Item] == ItemDef.RecordSizeWithoutNames
            ? ItemDef.RecordSizeWithoutNames
            : ItemDef.RecordSize;

        Items = LoadItems(messages);
        (MageSpells, PriestSpells) = LoadSpells(messages);
    }

    /// <summary>アイテム 1 件のバイト数(78、名前のない版は 46)。</summary>
    public int ItemRecordSize { get; }

    /// <summary>アイテム名をどこから読んだか(画面表示用)。</summary>
    public string ItemNameSource { get; private set; } = "SCENARIO.DATA";

    /// <summary>呪文名をどこから読んだか(画面表示用)。</summary>
    public string SpellNameSource { get; private set; } = "SCENARIO.DATA";

    public string Title { get; }
    public int ByteLength { get; }
    public IReadOnlyList<int> RecordsPerPage { get; }
    public IReadOnlyList<int> RecordCounts { get; }
    public IReadOnlyList<int> BlockOffsets { get; }

    public IReadOnlyList<ItemDef> Items { get; }
    public IReadOnlyList<SpellDef> MageSpells { get; }
    public IReadOnlyList<SpellDef> PriestSpells { get; }

    public IEnumerable<SpellDef> AllSpells => MageSpells.Concat(PriestSpells);

    /// <summary>キャラクタの保存数(Wizardry #1 は 20)。</summary>
    public int CharacterCount => RecordCounts[(int)Zone.Character];

    public ItemDef? FindItem(int index) => index >= 0 && index < Items.Count ? Items[index] : null;

    /// <summary>
    /// #3 で、持ち物の番号に 1000 が足されていることがある(状態が消滅のキャラクタで見つかった。足す意味は未確認)。
    /// 1000 を引いた番号のアイテムは職業と装備が合うので、そのアイテムとして扱う。
    /// </summary>
    public const int ItemNumberFlag = 1000;

    /// <summary>持ち物の番号からアイテムを引く。1000 が足された番号も解決する。</summary>
    public ItemDef? ResolveItem(int number) =>
        FindItem(number) ?? (number >= ItemNumberFlag ? FindItem(number - ItemNumberFlag) : null);

    /// <summary>番号が「1000 + アイテム番号」の形か。</summary>
    public bool IsFlaggedItemNumber(int number) =>
        FindItem(number) == null && number >= ItemNumberFlag && FindItem(number - ItemNumberFlag) != null;

    public SpellDef? FindSpell(int bit) => AllSpells.FirstOrDefault(s => s.Bit == bit);

    /// <summary>区分 zone の index 番目のレコードの位置(SCENARIO.DATA 先頭からのバイト数)。</summary>
    public int RecordOffset(Zone zone, int index, int recordSize)
    {
        int z = (int)zone;
        int perPage = RecordsPerPage[z];
        return BlockOffsets[z] * BlockImageExtensions.BlockSize + index / perPage * PageSize + index % perPage * recordSize;
    }

    public Character ReadCharacter(int slot)
    {
        CheckSlot(slot);
        return new Character(Read(RecordOffset(Zone.Character, slot, Character.RecordSize), Character.RecordSize));
    }

    public void WriteCharacter(int slot, Character character)
    {
        CheckSlot(slot);
        Write(RecordOffset(Zone.Character, slot, Character.RecordSize), character.Raw);
    }

    /// <summary>SCENARIO.DATA 内の位置からイメージファイル内の位置へ(連続イメージのときだけ意味がある。表示用)。</summary>
    public string DescribeLocation(int offset)
    {
        int block = _startBlock + offset / BlockImageExtensions.BlockSize;
        return $"ブロック {block} + {offset % BlockImageExtensions.BlockSize}";
    }

    public byte[] Read(int offset, int length)
    {
        CheckRange(offset, length);
        return _image.ReadBytes(_startBlock, offset, length);
    }

    public void Write(int offset, ReadOnlySpan<byte> data)
    {
        CheckRange(offset, data.Length);
        _image.WriteBytes(_startBlock, offset, data);
    }

    private List<ItemDef> LoadItems(KrnMessageFile? messages)
    {
        bool hasNames = ItemRecordSize == ItemDef.RecordSize;
        if (!hasNames)
            ItemNameSource = messages != null ? KrnMessageFile.FileName : "(名前なし)";

        int count = RecordCounts[(int)Zone.Item];
        var items = new List<ItemDef>(count);
        for (int i = 0; i < count; i++)
        {
            int offset = RecordOffset(Zone.Item, i, ItemRecordSize);
            if (offset + ItemRecordSize > ByteLength)
                break;
            byte[] r = Read(offset, ItemRecordSize);
            if (hasNames)
            {
                items.Add(ItemDef.Parse(i, r));
                continue;
            }

            string? rawName = messages?.Get(KrnItemBase + 2 * i + 1);
            string? rawUnknown = messages?.Get(KrnItemBase + 2 * i);
            string name = rawName != null ? KrnMessageFile.ExpandAbbreviations(rawName) : $"ITEM {i:D3}";
            string unknown = rawUnknown != null ? KrnMessageFile.ExpandAbbreviations(rawUnknown) : name;
            string? note = rawName != null && (rawName != name || rawUnknown != unknown)
                ? $"{KrnMessageFile.FileName} の表記: {rawName} / {rawUnknown}(略記の展開は推測)"
                : null;
            items.Add(ItemDef.ParseWithoutNames(i, r, name, unknown, note));
        }
        return items;
    }

    /// <summary>
    /// 呪文名の表。迷宮データの 2 ブロック前が魔術師、1 ブロック前が僧侶。
    /// 名前は 0Dh 区切りで、2 つ目以降のレベルの先頭に '*' が付く。
    /// アイテム表に名前がない版、または表が読めないときは ASCII.KRN の 5001 番以降を使う。
    /// </summary>
    private (List<SpellDef>, List<SpellDef>) LoadSpells(KrnMessageFile? messages)
    {
        if (ItemRecordSize == ItemDef.RecordSize && FindSpellTables() is var (mageNames, priestNames))
        {
            var mage = BuildSpells(mageNames, SpellSchool.Mage, firstBit: 1);
            return (mage, BuildSpells(priestNames, SpellSchool.Priest, firstBit: 1 + mage.Count));
        }

        if (messages == null)
        {
            SpellNameSource = "(名前なし)";
            return ([], []);
        }

        // 5001 番から続けて並ぶ。'*' がレベルの区切りで、レベル 7 の次の '*' から僧侶。
        var mageList = new List<string>();
        var priestList = new List<string>();
        int levelMarks = 0;
        for (int m = KrnFirstSpell; mageList.Count + priestList.Count < Character.SpellBitCount - 1; m++)
        {
            string? name = messages.Get(m);
            if (string.IsNullOrEmpty(name))
                break;
            if (name.StartsWith('*'))
                levelMarks++;
            (levelMarks <= 7 ? mageList : priestList).Add(name);
        }
        SpellNameSource = mageList.Count + priestList.Count > 0 ? KrnMessageFile.FileName : "(名前なし)";
        var mageSpells = BuildSpells(mageList, SpellSchool.Mage, firstBit: 1);
        return (mageSpells, BuildSpells(priestList, SpellSchool.Priest, firstBit: 1 + mageSpells.Count));
    }

    /// <summary>
    /// 魔術師と僧侶の呪文名の表(続いた 2 ブロック)を探す。
    /// まず迷宮データの直前(#1・#2 の多く)を見て、なければ目次と最初のデータの間を探す
    /// (Apple II 版の #3 は 1・2 ブロック目にあり、データの並び順が違うので直前の規則が外れる)。
    /// </summary>
    private (List<string> Mage, List<string> Priest)? FindSpellTables()
    {
        int mazeBlock = BlockOffsets[(int)Zone.Maze];
        int firstData = BlockOffsets.Where(b => b > 0).DefaultIfEmpty(1).Min();
        var candidates = new[] { mazeBlock - 2 }.Concat(Enumerable.Range(1, Math.Max(0, firstData - 2)));
        foreach (int b in candidates.Distinct())
        {
            if (b < 1 || b + 1 >= ByteLength / BlockImageExtensions.BlockSize)
                continue;
            var mage = ReadSpellBlock(b);
            var priest = ReadSpellBlock(b + 1);
            if (LooksLikeSpellTable(mage) && LooksLikeSpellTable(priest))
                return (mage!, priest!);
        }
        return null;
    }

    /// <summary>呪文名の表らしいか。名前は英大文字だけで、レベル 1〜7 の区切り '*' が 6 つ以上ある。</summary>
    private static bool LooksLikeSpellTable(List<string>? names) =>
        names is { Count: >= 7 } &&
        names.All(n => n.TrimStart('*') is { Length: >= 2 and <= 15 } s && s.All(ch => ch is >= 'A' and <= 'Z')) &&
        names.Skip(1).Count(n => n.StartsWith('*')) >= 6;

    /// <summary>
    /// 呪文名の表を 1 ブロック読む。途中に空の行がある(壊れている)ときは null。
    /// MSX2 版のユーザーディスクでは、先頭の名前が 0Dh で上書きされていた。
    /// </summary>
    private List<string>? ReadSpellBlock(int block)
    {
        if (block < 1)
            return null;
        byte[] data = Read(block * BlockImageExtensions.BlockSize, BlockImageExtensions.BlockSize);
        int end = Array.FindIndex(data, b => b != 0x0D && (b < 0x20 || b > 0x7E));
        string text = Encoding.ASCII.GetString(data, 0, end < 0 ? data.Length : end);
        var parts = text.Split('\r').ToList();
        while (parts.Count > 0 && parts[^1].Length == 0)
            parts.RemoveAt(parts.Count - 1); // 末尾の区切りは正常
        if (parts.Any(p => p.Trim('*').Length == 0))
            return null;
        return parts;
    }

    /// <summary>名前の並びから呪文を作る。'*' で次のレベルへ(先頭の '*' はレベル 1 のまま)。</summary>
    private static List<SpellDef> BuildSpells(IReadOnlyList<string> names, SpellSchool school, int firstBit)
    {
        var result = new List<SpellDef>();
        int level = 1;
        foreach (string raw in names)
        {
            string name = raw;
            if (name.StartsWith('*'))
            {
                if (result.Count > 0)
                    level++;
                name = name[1..];
            }
            if (firstBit + result.Count >= Character.SpellBitCount)
                break;
            result.Add(new SpellDef(school, result.Count, level, name, firstBit + result.Count));
        }
        return result;
    }

    private static int[] ReadArray(byte[] toc, int offset) =>
        Enumerable.Range(0, ZoneCount).Select(i => (int)WizBytes.S16(toc, offset + i * 2)).ToArray();

    private void CheckSlot(int slot)
    {
        if (slot < 0 || slot >= CharacterCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
    }

    private void CheckRange(int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > ByteLength)
            throw new ArgumentOutOfRangeException(nameof(offset), "SCENARIO.DATA の範囲外です。");
    }
}
