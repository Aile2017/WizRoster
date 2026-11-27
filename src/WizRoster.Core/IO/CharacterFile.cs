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
        var names = scenario?.Names ?? NameTables.Default;
        var dto = new CharacterDto
        {
            Name = c.Name,
            Password = c.Password,
            InMaze = c.InMaze,
            Race = c.Race,
            RaceName = names.Race(c.Race),
            Class = c.Class,
            ClassName = names.Class(c.Class),
            Age = c.Age,
            Status = c.Status,
            StatusName = names.Status(c.Status),
            Alignment = c.Alignment,
            AlignmentName = names.Alignment(c.Alignment),
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
                Name = scenario?.ResolveItem(p.ItemIndex)?.Name,
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

    /// <summary>
    /// JSON の値が、画面の入力欄と同じ範囲に入っているか確かめる。範囲外があれば、すべて並べて例外にする。
    /// </summary>
    /// <exception cref="InvalidDataException">範囲外の値があるとき。</exception>
    public static void Validate(CharacterDto dto)
    {
        var errors = new List<string>();
        void Range(string name, long value, long min, long max)
        {
            if (value < min || value > max)
                errors.Add($"{name} {value:N0}({min:N0}〜{max:N0})");
        }

        if (!ValueRanges.IsValidNameText(dto.Name ?? ""))
            errors.Add($"名前「{dto.Name}」({ValueRanges.NameMaxLength} 文字までの英数字・記号)");
        if (!ValueRanges.IsValidNameText(dto.Password ?? ""))
            errors.Add($"パスワード「{dto.Password}」({ValueRanges.NameMaxLength} 文字までの英数字・記号)");

        Range("種族", dto.Race, 0, ValueRanges.IntegerMax);
        Range("職業", dto.Class, 0, ValueRanges.IntegerMax);
        Range("状態", dto.Status, 0, ValueRanges.IntegerMax);
        Range("属性", dto.Alignment, 0, ValueRanges.IntegerMax);
        Range("年齢", dto.Age, 0, ValueRanges.IntegerMax);
        Range("レベル", dto.Level, 0, ValueRanges.IntegerMax);
        Range("最高レベル", dto.MaxLevel, 0, ValueRanges.IntegerMax);
        Range("HP", dto.Hp, 0, ValueRanges.IntegerMax);
        Range("最大 HP", dto.HpMax, 0, ValueRanges.IntegerMax);
        Range("AC", dto.ArmorClass, ValueRanges.IntegerMin, ValueRanges.IntegerMax);
        Range("所持金", dto.Gold, 0, WizBytes.WizLongMax);
        Range("経験値", dto.Experience, 0, WizBytes.WizLongMax);

        for (int i = 0; i < dto.Attributes.Length; i++)
            Range($"能力値 {(i < GameNames.Attributes.Length ? GameNames.Attributes[i] : i.ToString())}", dto.Attributes[i], 0, ValueRanges.Packed5Max);
        for (int i = 0; i < dto.SavingThrows.Length; i++)
            Range($"セービングスロー {i + 1}({(i < GameNames.SavingThrows.Length ? GameNames.SavingThrows[i] : "?")})",
                dto.SavingThrows[i], 0, ValueRanges.Packed5Max);
        for (int i = 0; i < dto.MageSpellPoints.Length; i++)
            Range($"魔術師呪文の回数 Lv{i + 1}", dto.MageSpellPoints[i], 0, ValueRanges.SpellPointsMax);
        for (int i = 0; i < dto.PriestSpellPoints.Length; i++)
            Range($"僧侶呪文の回数 Lv{i + 1}", dto.PriestSpellPoints[i], 0, ValueRanges.SpellPointsMax);

        if (dto.Items.Count > Character.MaxItems)
            errors.Add($"持ち物の数 {dto.Items.Count}({Character.MaxItems} 個まで)");
        for (int i = 0; i < dto.Items.Count; i++)
            Range($"持ち物 {i + 1} のアイテム番号", dto.Items[i].Index, 0, ValueRanges.IntegerMax);
        foreach (int bit in dto.Spells)
            Range("呪文の番号", bit, 0, Character.SpellBitCount - 1);

        foreach (var (key, value) in dto.Extra)
        {
            var field = RawFields.Extra.FirstOrDefault(f => $"+{f.Offset}" == key);
            if (field == null)
                errors.Add($"Extra の「{key}」(使えるのは {string.Join(", ", RawFields.Extra.Select(f => $"+{f.Offset}"))})");
            else if (field.Signed)
                Range($"Extra {key}", value, ValueRanges.IntegerMin, ValueRanges.IntegerMax);
            else
                Range($"Extra {key}", value, 0, ushort.MaxValue);
        }

        if (errors.Count > 0)
            throw new InvalidDataException("JSON に範囲外の値があります。\n" + string.Join("\n", errors.Select(e => "・" + e)));
    }

    public static Character FromDto(CharacterDto dto)
    {
        Validate(dto);
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

        c.Name = dto.Name ?? "";
        c.Password = dto.Password ?? "";
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
            var field = RawFields.Extra.First(f => $"+{f.Offset}" == key); // Validate で確認済み
            c.SetWord(field.Offset, value);
        }
        return c;
    }
}
