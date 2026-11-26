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

    public static ItemDef Parse(int index, ReadOnlySpan<byte> r) => new()
    {
        Index = index,
        Name = WizBytes.PascalString(r, 0, 15),
        UnknownName = WizBytes.PascalString(r, 16, 15),
        Type = WizBytes.S16(r, 32),
        Alignment = WizBytes.S16(r, 34),
        Cursed = WizBytes.U16(r, 36) != 0,
        Special = WizBytes.S16(r, 38),
        ChangeTo = WizBytes.S16(r, 40),
        ChangeChance = WizBytes.S16(r, 42),
        Price = WizBytes.WizLong(r, 44),
        BoltacStock = WizBytes.S16(r, 50),
        SpellPower = WizBytes.S16(r, 52),
        ClassUse = WizBytes.U16(r, 54),
        HealPoints = WizBytes.S16(r, 56),
        ProtectVs = WizBytes.U16(r, 58),
        ResistVs = WizBytes.U16(r, 60),
        ArmorMod = WizBytes.S16(r, 62),
        HitMod = WizBytes.S16(r, 64),
        DamageDice = WizBytes.S16(r, 66),
        DamageSides = WizBytes.S16(r, 68),
        DamageBonus = WizBytes.S16(r, 70),
        ExtraSwings = WizBytes.S16(r, 72),
        CriticalHit = WizBytes.U16(r, 74) != 0,
        PurposedVs = WizBytes.U16(r, 76),
    };

    public string ClassUseText()
    {
        if ((ClassUse & 0xFF) == 0xFF)
            return "全職業";
        var names = Enumerable.Range(0, GameNames.Classes.Length).Where(CanBeUsedBy).Select(i => GameNames.Classes[i]).ToList();
        return names.Count == 0 ? "なし" : string.Join("・", names);
    }

    public override string ToString() => $"{Index:D3} {Name}";
}
