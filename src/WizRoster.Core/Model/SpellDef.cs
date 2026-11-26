namespace WizRoster.Core.Model;

public enum SpellSchool
{
    Mage,
    Priest,
}

/// <summary>
/// 呪文 1 つ。<see cref="Bit"/> はキャラクタの「覚えている呪文」ビット列での位置で、
/// アイテムの SPELLPWR と同じ番号(魔術師が 1 から、続けて僧侶)。
/// </summary>
public sealed record SpellDef(SpellSchool School, int Order, int Level, string Name, int Bit)
{
    public override string ToString() => $"Lv{Level} {Name}";
}
