using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>
/// 呪文の回数の計算(CASTLE p18〜p24 の解析結果。docs/analysis/06-game-program.md 3.3)。
/// </summary>
public class SpellPointsTests
{
    // 段ごとの呪文の数: 魔術師 4, 2, 2, 3, 3, 4, 3 / 僧侶 5, 4, 4, 4, 6, 4, 2
    private static readonly int[] MageCounts = [4, 2, 2, 3, 3, 4, 3];
    private static readonly int[] PriestCounts = [5, 4, 4, 4, 6, 4, 2];

    private static List<SpellDef> Spells(SpellSchool school, int[] counts, int firstBit)
    {
        var list = new List<SpellDef>();
        int bit = firstBit;
        for (int lv = 1; lv <= 7; lv++)
        {
            for (int i = 0; i < counts[lv - 1]; i++, bit++)
                list.Add(new SpellDef(school, bit - firstBit, lv, $"S{bit}", bit));
        }
        return list;
    }

    private static readonly List<SpellDef> Mage = Spells(SpellSchool.Mage, MageCounts, 1);
    private static readonly List<SpellDef> Priest = Spells(SpellSchool.Priest, PriestCounts, 22);

    private static Character Make(int cls, int level, IEnumerable<SpellDef> known)
    {
        var c = Character.CreateNew("TEST");
        c.Class = cls;
        c.Level = level;
        foreach (var s in known)
            c.SetKnowsSpell(s.Bit, true);
        return c;
    }

    [Fact]
    public void Mage_Level21_AllNine()
    {
        var c = Make(1, 21, Mage);
        Assert.Equal([9, 9, 9, 9, 9, 9, 9], SpellPoints.Compute(c, SpellSchool.Mage, Mage));
    }

    [Fact]
    public void Mage_Level20_SeventhIsEight()
    {
        var c = Make(1, 20, Mage);
        Assert.Equal([9, 9, 9, 9, 9, 9, 8], SpellPoints.Compute(c, SpellSchool.Mage, Mage));
    }

    [Fact]
    public void LowLevel_KnownCountIsTheFloor()
    {
        // レベル 1 の魔術師が全呪文を覚えている: 1 段目は max(4, 1)、ほかは覚えている数
        var c = Make(1, 1, Mage);
        Assert.Equal(MageCounts, SpellPoints.Compute(c, SpellSchool.Mage, Mage));
    }

    [Fact]
    public void Mage_Level5_NothingKnown()
    {
        // 5, 3, 1, 0, ...
        var c = Make(1, 5, []);
        Assert.Equal([5, 3, 1, 0, 0, 0, 0], SpellPoints.Compute(c, SpellSchool.Mage, Mage));
    }

    [Theory]
    [InlineData(4, SpellSchool.Mage, 33)]   // 司教
    [InlineData(4, SpellSchool.Priest, 36)]
    [InlineData(5, SpellSchool.Mage, 30)]   // 侍
    [InlineData(6, SpellSchool.Priest, 24)] // 君主
    [InlineData(2, SpellSchool.Priest, 21)] // 僧侶
    public void SeventhReachesNine(int cls, SpellSchool school, int level)
    {
        var spells = school == SpellSchool.Mage ? Mage : Priest;
        Assert.Equal(8, SpellPoints.Compute(Make(cls, level - 1, []), school, spells)[6]);
        Assert.Equal(9, SpellPoints.Compute(Make(cls, level, []), school, spells)[6]);
    }

    [Fact]
    public void Fighter_GetsOnlyKnownCount()
    {
        var c = Make(0, 30, Mage);
        Assert.Equal(MageCounts, SpellPoints.Compute(c, SpellSchool.Mage, Mage));
        Assert.Equal([0, 0, 0, 0, 0, 0, 0], SpellPoints.Compute(c, SpellSchool.Priest, Priest));
    }
}
