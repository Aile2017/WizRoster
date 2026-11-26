using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;

namespace WizRoster.Core.IO;

/// <summary>
/// キャラクタ 1 人分の書き出しと読み込み。
/// <list type="bullet">
/// <item>.wchr: 208 バイトの生データ。そのまま復元できる。</item>
/// <item>.json: 人が読める形式。読み込み時は Raw を土台にして、各項目の値で上書きする。</item>
/// </list>
/// </summary>
public static class CharacterFile
{
    public const string RawExtension = ".wchr";
    public const string JsonExtension = ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Save(string path, Character c, ScenarioFile scenario)
    {
        if (Path.GetExtension(path).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase))
            File.WriteAllText(path, JsonSerializer.Serialize(ToDto(c, scenario), JsonOptions));
        else
            File.WriteAllBytes(path, c.Raw);
    }

    public static Character Load(string path)
    {
        if (Path.GetExtension(path).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            var dto = JsonSerializer.Deserialize<CharacterDto>(File.ReadAllText(path), JsonOptions)
                      ?? throw new InvalidDataException("JSON が空です。");
            return FromDto(dto);
        }
        byte[] raw = File.ReadAllBytes(path);
        if (raw.Length != Character.RecordSize)
            throw new InvalidDataException($"キャラクタファイルは {Character.RecordSize} バイトのはずですが、{raw.Length} バイトです。");
        return new Character(raw);
    }

    public sealed class CharacterDto
    {
        public int Format { get; set; } = 1;
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public bool InMaze { get; set; }
        public int Race { get; set; }
        public string? RaceName { get; set; }
        public int Class { get; set; }
        public string? ClassName { get; set; }
        public int Age { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public int Alignment { get; set; }
        public string? AlignmentName { get; set; }
        /// <summary>力, 知恵, 信仰心, 体力, 素早さ, 運</summary>
        public int[] Attributes { get; set; } = new int[6];
        public int[] SavingThrows { get; set; } = new int[5];
        public long Gold { get; set; }
        public long Experience { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public int Hp { get; set; }
        public int HpMax { get; set; }
        public int ArmorClass { get; set; }
        public List<ItemDto> Items { get; set; } = [];
        /// <summary>覚えている呪文のビット番号。</summary>
        public List<int> Spells { get; set; } = [];
        public List<string>? SpellNames { get; set; }
        public int[] MageSpellPoints { get; set; } = new int[7];
        public int[] PriestSpellPoints { get; set; } = new int[7];
        /// <summary>意味の分かっていない項目。キーは "+174" のような位置。</summary>
        public Dictionary<string, int> Extra { get; set; } = [];
        /// <summary>208 バイトの生データ(16 進)。読み込みの土台にする。</summary>
        public string? Raw { get; set; }
    }

    public sealed class ItemDto
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public bool Equipped { get; set; }
        public bool Cursed { get; set; }
        public bool Identified { get; set; }
    }

    public static CharacterDto ToDto(Character c, ScenarioFile? scenario)
    {
        var dto = new CharacterDto
        {
            Name = c.Name,
            Password = c.Password,
            InMaze = c.InMaze,
            Race = c.Race,
            RaceName = GameNames.Lookup(GameNames.Races, c.Race),
            Class = c.Class,
            ClassName = GameNames.Lookup(GameNames.Classes, c.Class),
            Age = c.Age,
            Status = c.Status,
            StatusName = GameNames.Lookup(GameNames.Statuses, c.Status),
            Alignment = c.Alignment,
            AlignmentName = GameNames.Lookup(GameNames.Alignments, c.Alignment),
            Attributes = Enumerable.Range(0, 6).Select(c.GetAttribute).ToArray(),
            SavingThrows = Enumerable.Range(0, 5).Select(c.GetSavingThrow).ToArray(),
            Gold = c.Gold,
            Experience = c.Experience,
            Level = c.Level,
            MaxLevel = c.MaxLevel,
            Hp = c.Hp,
            HpMax = c.HpMax,
            ArmorClass = c.ArmorClass,
            Items = c.GetItems().Select(p => new ItemDto
            {
                Index = p.ItemIndex,
                Name = scenario?.FindItem(p.ItemIndex)?.Name,
                Equipped = p.Equipped,
                Cursed = p.Cursed,
                Identified = p.Identified,
            }).ToList(),
            Spells = Enumerable.Range(0, Character.SpellBitCount).Where(c.KnowsSpell).ToList(),
            MageSpellPoints = Enumerable.Range(1, 7).Select(c.GetMageSpellPoints).ToArray(),
            PriestSpellPoints = Enumerable.Range(1, 7).Select(c.GetPriestSpellPoints).ToArray(),
            Extra = RawFields.Extra.ToDictionary(f => $"+{f.Offset}", f => f.Signed ? c.SignedWord(f.Offset) : c.Word(f.Offset)),
            Raw = Convert.ToHexString(c.Raw),
        };
        if (scenario != null)
            dto.SpellNames = dto.Spells.Select(b => scenario.FindSpell(b)?.Name ?? $"#{b}").ToList();
        return dto;
    }

    public static Character FromDto(CharacterDto dto)
    {
        Character c;
        if (!string.IsNullOrEmpty(dto.Raw))
        {
            byte[] raw = Convert.FromHexString(dto.Raw);
            if (raw.Length != Character.RecordSize)
                throw new InvalidDataException("Raw の長さが 208 バイトではありません。");
            c = new Character(raw);
        }
        else
        {
            c = Character.CreateBlank();
        }

        c.Name = dto.Name;
        c.Password = dto.Password;
        c.InMaze = dto.InMaze;
        c.Race = dto.Race;
        c.Class = dto.Class;
        c.Age = dto.Age;
        c.Status = dto.Status;
        c.Alignment = dto.Alignment;
        for (int i = 0; i < 6 && i < dto.Attributes.Length; i++)
            c.SetAttribute(i, dto.Attributes[i]);
        for (int i = 0; i < 5 && i < dto.SavingThrows.Length; i++)
            c.SetSavingThrow(i, dto.SavingThrows[i]);
        c.Gold = dto.Gold;
        c.Experience = dto.Experience;
        c.Level = dto.Level;
        c.MaxLevel = dto.MaxLevel;
        c.Hp = dto.Hp;
        c.HpMax = dto.HpMax;
        c.ArmorClass = dto.ArmorClass;
        c.SetItems(dto.Items.Select(i => new Possession(i.Equipped, i.Cursed, i.Identified, i.Index)).ToList());
        for (int bit = 0; bit < Character.SpellBitCount; bit++)
            c.SetKnowsSpell(bit, dto.Spells.Contains(bit));
        for (int lv = 1; lv <= 7; lv++)
        {
            if (lv - 1 < dto.MageSpellPoints.Length)
                c.SetMageSpellPoints(lv, dto.MageSpellPoints[lv - 1]);
            if (lv - 1 < dto.PriestSpellPoints.Length)
                c.SetPriestSpellPoints(lv, dto.PriestSpellPoints[lv - 1]);
        }
        foreach (var (key, value) in dto.Extra)
        {
            if (key.StartsWith('+') && int.TryParse(key[1..], out int offset)
                && offset % 2 == 0 && offset is >= 0 and < Character.RecordSize)
            {
                c.SetWord(offset, value);
            }
        }
        return c;
    }
}
