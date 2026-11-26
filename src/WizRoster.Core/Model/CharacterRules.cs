using WizRoster.Core.Scenario;

namespace WizRoster.Core.Model;

/// <summary>
/// ゲームのルールに合わない組み合わせを調べる。入力は止めず、警告として表示するだけ。
/// </summary>
public static class CharacterRules
{
    // 職業ごとの能力値の条件(力, 知恵, 信仰心, 体力, 素早さ, 運)と、なれる属性(善, 中立, 悪)。
    private static readonly (int[] Min, bool[] Align)[] ClassRequirements =
    [
        ([11, 0, 0, 0, 0, 0], [true, true, true]),         // 戦士
        ([0, 11, 0, 0, 0, 0], [true, true, true]),         // 魔術師
        ([0, 0, 11, 0, 0, 0], [true, false, true]),        // 僧侶
        ([0, 0, 0, 0, 11, 0], [false, true, true]),        // 盗賊
        ([0, 12, 12, 0, 0, 0], [true, false, true]),       // 司教
        ([15, 11, 10, 14, 10, 0], [true, true, false]),    // 侍
        ([15, 12, 12, 15, 14, 15], [true, false, false]),  // 君主
        ([17, 17, 17, 17, 17, 17], [false, false, true]),  // 忍者
    ];

    // 装備部位ごとに 1 つまで(武器, 鎧, 盾, 兜, 小手)
    private const int EquipSlotTypes = 5;

    public static IReadOnlyList<string> Check(Character c, ScenarioFile scenario)
    {
        var w = new List<string>();
        if (c.IsEmpty)
            return w;

        if (c.Race is < 1 or > 5)
            w.Add($"種族の値 {c.Race} はゲームの範囲外です。");
        if (c.Class is < 0 or > 7)
            w.Add($"職業の値 {c.Class} はゲームの範囲外です。");
        if (c.Status is < 0 or > 7)
            w.Add($"状態の値 {c.Status} はゲームの範囲外です。");
        if (c.Alignment is < 1 or > 3)
            w.Add($"属性の値 {c.Alignment} はゲームの範囲外です。");

        for (int i = 0; i < 6; i++)
        {
            if (c.GetAttribute(i) > 18)
                w.Add($"{GameNames.Attributes[i]}が 18 を超えています({c.GetAttribute(i)})。");
        }

        if (c.Class is >= 0 and <= 7)
        {
            var (min, align) = ClassRequirements[c.Class];
            var lacking = Enumerable.Range(0, 6).Where(i => c.GetAttribute(i) < min[i])
                .Select(i => $"{GameNames.Attributes[i]} {min[i]} 以上").ToList();
            if (lacking.Count > 0)
                w.Add($"{GameNames.Classes[c.Class]}の能力値の条件を満たしていません({string.Join("、", lacking)})。");
            if (c.Alignment is >= 1 and <= 3 && !align[c.Alignment - 1])
                w.Add($"{GameNames.Classes[c.Class]}は{GameNames.Alignments[c.Alignment]}にはなれません。");
        }

        if (c.Hp > c.HpMax)
            w.Add($"現在 HP({c.Hp})が最大 HP({c.HpMax})を超えています。");
        if (c.Level > c.MaxLevel)
            w.Add($"レベル({c.Level})が最高レベル({c.MaxLevel})を超えています。");
        if (c.Status >= 5 && c.Hp > 0)
            w.Add("状態が死亡以上なのに HP が残っています。");

        CheckItems(c, scenario, w);
        CheckSpells(c, scenario, w);
        return w;
    }

    private static void CheckItems(Character c, ScenarioFile scenario, List<string> w)
    {
        if (c.ItemCount is < 0 or > Character.MaxItems)
        {
            w.Add($"持ち物の数({c.ItemCount})が 0〜8 の範囲外です。");
            return;
        }

        var equippedPerType = new int[EquipSlotTypes];
        foreach (var (p, i) in c.GetItems().Select((p, i) => (p, i)))
        {
            var item = scenario.FindItem(p.ItemIndex);
            if (item == null)
            {
                w.Add($"持ち物 {i + 1}: アイテム番号 {p.ItemIndex} はアイテム表にありません。");
                continue;
            }
            if (!p.Equipped)
                continue;
            if (!item.CanBeUsedBy(c.Class))
                w.Add($"持ち物 {i + 1}: {item.Name} は{GameNames.Lookup(GameNames.Classes, c.Class)}には装備できません。");
            if (item.Alignment != 0 && item.Alignment != c.Alignment)
                w.Add($"持ち物 {i + 1}: {item.Name} は属性が{GameNames.Lookup(GameNames.Alignments, item.Alignment)}専用です(装備すると呪われます)。");
            if (item.Type is >= 0 and < EquipSlotTypes && ++equippedPerType[item.Type] == 2)
                w.Add($"{GameNames.ItemTypes[item.Type]}を 2 つ以上装備しています。");
            if (!item.IsEquipment && item.Type != 6)
                w.Add($"持ち物 {i + 1}: {item.Name} は装備品ではありません。");
        }
    }

    private static void CheckSpells(Character c, ScenarioFile scenario, List<string> w)
    {
        if (c.KnowsSpell(0))
            w.Add("呪文ビット 0(未使用)が立っています。");
        int lastBit = scenario.AllSpells.Select(s => s.Bit).DefaultIfEmpty(0).Max();
        for (int bit = lastBit + 1; bit < Character.SpellBitCount; bit++)
        {
            if (c.KnowsSpell(bit))
                w.Add($"呪文ビット {bit}(呪文表にない)が立っています。");
        }
        for (int lv = 1; lv <= 7; lv++)
        {
            if (c.GetMageSpellPoints(lv) > 9 || c.GetPriestSpellPoints(lv) > 9)
            {
                w.Add($"レベル {lv} の呪文の回数が 9 を超えています。");
                break;
            }
        }
    }
}
