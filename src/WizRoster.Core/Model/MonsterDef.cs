namespace WizRoster.Core.Model;

/// <summary>ダイス(THPREC): 数 d 面 + 加算。</summary>
public readonly record struct Dice(int Count, int Sides, int Bonus)
{
    public static Dice Read(ReadOnlySpan<byte> b, int offset) =>
        new(WizBytes.S16(b, offset), WizBytes.S16(b, offset + 2), WizBytes.S16(b, offset + 4));

    public int Min => Count + Bonus;
    public int Max => Count * Sides + Bonus;

    public override string ToString() => Bonus == 0 ? $"{Count}d{Sides}" : $"{Count}d{Sides}{Bonus:+0;-0}";
}

/// <summary>
/// モンスター表の 1 件(TENEMY, 158 バイト。名前のない版は 94 バイト)。
/// 名前 4 つ(不確定名・その複数形・名前・その複数形 各 STRING[15])のあとに、
/// 絵、出現数、HP、種族、AC、攻撃(回数と 7 つのダイス)、経験値、レベル吸収、HP 回復、宝 2 つ、
/// 仲間とその確率、魔術師・僧侶の呪文のレベル、出現の上限、ブレス、呪文無効化率、抵抗、特殊能力が並ぶ。
/// </summary>
public sealed class MonsterDef
{
    public const int RecordSize = 158;
    public const int RecordSizeWithoutNames = RecordSize - 64;
    public const int MaxAttacks = 7;

    public required int Index { get; init; }
    public required string UnknownName { get; init; }
    public required string UnknownNamePlural { get; init; }
    public required string Name { get; init; }
    public required string NamePlural { get; init; }

    public required int Picture { get; init; }
    /// <summary>出現数(CALC1)。</summary>
    public required Dice GroupSize { get; init; }
    /// <summary>HP(HPREC)。ダイスの数がモンスターのレベルにあたる。</summary>
    public required Dice HitPoints { get; init; }
    /// <summary>種族(0〜13)。アイテムの倍打・防御の対象のビットと同じ番号。</summary>
    public required int Class { get; init; }
    public required int ArmorClass { get; init; }
    /// <summary>攻撃(RECSN 個のダメージダイス)。</summary>
    public required IReadOnlyList<Dice> Attacks { get; init; }
    /// <summary>経験値(EXPAMT)。#1 のデータではすべて 0。</summary>
    public required long Experience { get; init; }
    public required int LevelDrain { get; init; }
    public required int HealPoints { get; init; }
    /// <summary>宝(REWARD1 = 徘徊のとき、REWARD2 = 玄室のとき)。宝の表の番号。</summary>
    public required int Reward1 { get; init; }
    public required int Reward2 { get; init; }
    /// <summary>一緒に出る仲間のモンスター番号とその確率(%)。</summary>
    public required int Team { get; init; }
    public required int TeamPercent { get; init; }
    public required int MageSpellLevel { get; init; }
    public required int PriestSpellLevel { get; init; }
    /// <summary>出現の上限(UNIQUE)。−1 は無制限。</summary>
    public required int Unique { get; init; }
    /// <summary>ブレスの種類(0 = なし)。</summary>
    public required int Breath { get; init; }
    /// <summary>呪文無効化率(%)。</summary>
    public required int SpellResistance { get; init; }
    /// <summary>抵抗のビット列(WEPVSTY3)。</summary>
    public required int ResistBits { get; init; }
    /// <summary>特殊能力のビット列(SPPC)。</summary>
    public required int SpecialBits { get; init; }

    /// <summary>名前付きの 158 バイトのレコード。</summary>
    public static MonsterDef Parse(int index, ReadOnlySpan<byte> r) =>
        FromBody(index, r[64..],
            WizBytes.PascalString(r, 0, 15), WizBytes.PascalString(r, 16, 15),
            WizBytes.PascalString(r, 32, 15), WizBytes.PascalString(r, 48, 15));

    /// <summary>名前なしの 94 バイトのレコード。名前は別の場所から渡す。</summary>
    public static MonsterDef ParseWithoutNames(int index, ReadOnlySpan<byte> body,
        string unknownName, string unknownNamePlural, string name, string namePlural) =>
        FromBody(index, body, unknownName, unknownNamePlural, name, namePlural);

    // body は名前のあと(絵から)。
    private static MonsterDef FromBody(int index, ReadOnlySpan<byte> b,
        string unknownName, string unknownNamePlural, string name, string namePlural)
    {
        int attackCount = Math.Clamp((int)WizBytes.S16(b, 18), 0, MaxAttacks);
        var attacks = new Dice[attackCount];
        for (int i = 0; i < attackCount; i++)
            attacks[i] = Dice.Read(b, 20 + i * 6);

        return new MonsterDef
        {
            Index = index,
            UnknownName = unknownName,
            UnknownNamePlural = unknownNamePlural,
            Name = name,
            NamePlural = namePlural,
            Picture = WizBytes.S16(b, 0),
            GroupSize = Dice.Read(b, 2),
            HitPoints = Dice.Read(b, 8),
            Class = WizBytes.S16(b, 14),
            ArmorClass = WizBytes.S16(b, 16),
            Attacks = attacks,
            Experience = WizBytes.WizLong(b, 62),
            LevelDrain = WizBytes.S16(b, 68),
            HealPoints = WizBytes.S16(b, 70),
            Reward1 = WizBytes.S16(b, 72),
            Reward2 = WizBytes.S16(b, 74),
            Team = WizBytes.S16(b, 76),
            TeamPercent = WizBytes.S16(b, 78),
            MageSpellLevel = WizBytes.S16(b, 80),
            PriestSpellLevel = WizBytes.S16(b, 82),
            Unique = WizBytes.S16(b, 84),
            Breath = WizBytes.S16(b, 86),
            SpellResistance = WizBytes.S16(b, 88),
            ResistBits = WizBytes.U16(b, 90),
            SpecialBits = WizBytes.U16(b, 92),
        };
    }
}
