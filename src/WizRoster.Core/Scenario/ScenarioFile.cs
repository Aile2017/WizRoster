using System.Text;
using WizRoster.Core.Disk;
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
/// <item>+74 UNUSEDXX[8]</item>
/// <item>+90 BLOFF[8]: ファイル先頭からの開始ブロック</item>
/// </list>
/// </summary>
public sealed class ScenarioFile
{
    private const int ZoneCount = 8;
    private const int PageSize = 1024;

    private readonly IBlockImage _image;
    private readonly int _startBlock;

    public ScenarioFile(IBlockImage image, UcsdFileEntry entry)
    {
        _image = image;
        _startBlock = entry.FirstBlock;
        ByteLength = entry.BlockLength * BlockImageExtensions.BlockSize;

        byte[] toc = Read(0, BlockImageExtensions.BlockSize);
        Title = WizBytes.PascalString(toc, 0, 40);
        RecordsPerPage = ReadArray(toc, 42);
        RecordCounts = ReadArray(toc, 58);
        BlockOffsets = ReadArray(toc, 90);

        if (RecordsPerPage[(int)Zone.Character] <= 0 || RecordCounts[(int)Zone.Character] is <= 0 or > 100)
            throw new InvalidDataException("SCENARIO.DATA の目次が読めません。");

        Items = LoadItems();
        (MageSpells, PriestSpells) = LoadSpells();
    }

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

    private List<ItemDef> LoadItems()
    {
        int count = RecordCounts[(int)Zone.Item];
        var items = new List<ItemDef>(count);
        for (int i = 0; i < count; i++)
        {
            int offset = RecordOffset(Zone.Item, i, ItemDef.RecordSize);
            if (offset + ItemDef.RecordSize > ByteLength)
                break;
            items.Add(ItemDef.Parse(i, Read(offset, ItemDef.RecordSize)));
        }
        return items;
    }

    /// <summary>
    /// 呪文名の表。迷宮データの 2 ブロック前が魔術師、1 ブロック前が僧侶。
    /// 名前は 0Dh 区切りで、2 つ目以降のレベルの先頭に '*' が付く。
    /// </summary>
    private (List<SpellDef>, List<SpellDef>) LoadSpells()
    {
        int mazeBlock = BlockOffsets[(int)Zone.Maze];
        var mage = ParseSpellNames(mazeBlock - 2, SpellSchool.Mage, firstBit: 1);
        var priest = ParseSpellNames(mazeBlock - 1, SpellSchool.Priest, firstBit: 1 + mage.Count);
        return (mage, priest);
    }

    private List<SpellDef> ParseSpellNames(int block, SpellSchool school, int firstBit)
    {
        var result = new List<SpellDef>();
        if (block < 1)
            return result;
        byte[] data = Read(block * BlockImageExtensions.BlockSize, BlockImageExtensions.BlockSize);
        int end = Array.FindIndex(data, b => b != 0x0D && (b < 0x20 || b > 0x7E));
        string text = Encoding.ASCII.GetString(data, 0, end < 0 ? data.Length : end);
        int level = 1;
        foreach (string part in text.Split('\r'))
        {
            string name = part;
            if (name.StartsWith('*'))
            {
                level++;
                name = name[1..];
            }
            if (name.Length == 0)
                continue;
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
