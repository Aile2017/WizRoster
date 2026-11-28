namespace WizRoster.Core.Model;

/// <summary>
/// ゲームが城(レベルアップなど)で計算し直す呪文の回数。Apple II 版 #1 の CASTLE p18〜p24 で確認
/// (docs/analysis/06-game-program.md 3.3)。
/// 各段の回数 = その段で覚えている呪文の数と、レベル − 開始 − (段 − 1) × 間隔 の多いほう。9 が上限。
/// </summary>
public static class SpellPoints
{
    public const int Max = 9;

    /// <summary>職業(0〜7)と系統ごとの (開始, 間隔)。その系統のレベルによる回数がない職業は null。</summary>
    public static (int Base, int Step)? Progression(int charClass, SpellSchool school) => (charClass, school) switch
    {
        (1, SpellSchool.Mage) => (0, 2),    // 魔術師
        (2, SpellSchool.Priest) => (0, 2),  // 僧侶
        (4, SpellSchool.Mage) => (0, 4),    // 司教
        (4, SpellSchool.Priest) => (3, 4),
        (5, SpellSchool.Mage) => (3, 3),    // 侍
        (6, SpellSchool.Priest) => (3, 2),  // 君主
        _ => null,
    };

    /// <summary>
    /// 段 1〜7 の回数(添字 0〜6)をゲームと同じ方法で求める。
    /// <paramref name="spells"/> はその系統の呪文の一覧(段ごとに覚えている数を数えるのに使う)。
    /// </summary>
    public static int[] Compute(Character c, SpellSchool school, IEnumerable<SpellDef> spells)
    {
        var points = new int[7];
        foreach (var s in spells)
        {
            if (s.School == school && s.Level is >= 1 and <= 7 && c.KnowsSpell(s.Bit))
                points[s.Level - 1]++;
        }

        if (Progression(c.Class, school) is var (b, step))
        {
            int n = c.Level - b;
            for (int lv = 0; lv < 7 && n > 0; lv++, n -= step)
            {
                if (n > points[lv])
                    points[lv] = n;
            }
        }

        for (int lv = 0; lv < 7; lv++)
            points[lv] = Math.Min(points[lv], Max);
        return points;
    }
}
