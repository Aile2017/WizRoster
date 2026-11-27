namespace WizRoster.Core.Model;

/// <summary>
/// アイテム表の 1 件(TOBJREC, 78 バイト)。
/// </summary>
public sealed class ItemDef
{
    public const int RecordSize = 78;

    public required int Index { get; init; }
    public required string Name { get; init; }
    public required string UnknownName { get; init; }
    public required int Type { get; init; }
    public required int Alignment { get; init; }
    public required bool Cursed { get; init; }
    public required int Special { get; init; }
    public required int ChangeTo { get; init; }
    public required int ChangeChance { get; init; }
    public required long Price { get; init; }
    public required int BoltacStock { get; init; }
    public required int SpellPower { get; init; }
    /// <summary>装備できる職業のビット列(ビット n が職業 n)。</summary>
    public required int ClassUse { get; init; }
    public required int HealPoints { get; init; }
    public required int ProtectVs { get; init; }
    public required int ResistVs { get; init; }
    public required int ArmorMod { get; init; }
    public required int HitMod { get; init; }
    public required int DamageDice { get; init; }
    public required int DamageSides { get; init; }
    public required int DamageBonus { get; init; }
    public required int ExtraSwings { get; init; }
    public required bool CriticalHit { get; init; }
    public required int PurposedVs { get; init; }

    public bool CanBeUsedBy(int classIndex) => classIndex is >= 0 and < 16 && (ClassUse & (1 << classIndex)) != 0;

    public bool IsEquipment => Type is >= 0 and <= 4;

    /// <summary>名前を除いた部分のバイト数(PC-DOS 版のアイテム表はこの大きさ)。</summary>
    public const int RecordSizeWithoutNames = RecordSize - 32;

    /// <summary>名前の取得元のメモ(画面表示用)。例: ASCII.KRN の元の表記。</summary>
    public string? NameNote { get; init; }

    /// <summary>名前付きの 78 バイトのレコード。</summary>
    public static ItemDef Parse(int index, ReadOnlySpan<byte> r) =>
        FromBody(index, r[32..], WizBytes.PascalString(r, 0, 15), WizBytes.PascalString(r, 16, 15), null);

    /// <summary>名前なしの 46 バイトのレコード。名前は別の場所から渡す。</summary>
    public static ItemDef ParseWithoutNames(int index, ReadOnlySpan<byte> body, string name, string unknownName, string? nameNote) =>
        FromBody(index, body, name, unknownName, nameNote);

    // body はレコードの +32 以降(種類から)。
    private static ItemDef FromBody(int index, ReadOnlySpan<byte> b, string name, string unknownName, string? nameNote) => new()
    {
        Index = index,
        Name = name,
        UnknownName = unknownName,
        NameNote = nameNote,
        Type = WizBytes.S16(b, 0),
        Alignment = WizBytes.S16(b, 2),
        Cursed = WizBytes.U16(b, 4) != 0,
        Special = WizBytes.S16(b, 6),
        ChangeTo = WizBytes.S16(b, 8),
        ChangeChance = WizBytes.S16(b, 10),
        Price = WizBytes.WizLong(b, 12),
        BoltacStock = WizBytes.S16(b, 18),
        SpellPower = WizBytes.S16(b, 20),
        ClassUse = WizBytes.U16(b, 22),
        HealPoints = WizBytes.S16(b, 24),
        ProtectVs = WizBytes.U16(b, 26),
        ResistVs = WizBytes.U16(b, 28),
        ArmorMod = WizBytes.S16(b, 30),
        HitMod = WizBytes.S16(b, 32),
        DamageDice = WizBytes.S16(b, 34),
        DamageSides = WizBytes.S16(b, 36),
        DamageBonus = WizBytes.S16(b, 38),
        ExtraSwings = WizBytes.S16(b, 40),
        CriticalHit = WizBytes.U16(b, 42) != 0,
        PurposedVs = WizBytes.U16(b, 44),
    };

    /// <summary>装備できる職業の一覧。<paramref name="classNames"/> は職業の名前(ディスクにあるもの)。</summary>
    public string ClassUseText(IReadOnlyList<string> classNames)
    {
        if ((ClassUse & 0xFF) == 0xFF)
            return "全職業";
        var names = Enumerable.Range(0, classNames.Count).Where(CanBeUsedBy).Select(i => classNames[i]).ToList();
        return names.Count == 0 ? "なし" : string.Join("・", names);
    }

    public override string ToString() => $"{Index:D3} {Name}";
}
