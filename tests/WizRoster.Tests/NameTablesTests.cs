using WizRoster.Core;

namespace WizRoster.Tests;

/// <summary>種族・職業・状態・属性の名前をディスクから読むこと(読み込みだけ)。</summary>
public class NameTablesTests
{
    private const string Pc98 = @"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI";
    private const string Apple1 = @"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk";

    /// <summary>
    /// アスキー移植版は ASCII.KRN を優先する。KRN にない値(NO RACE)は目次から、
    /// 目次の属性の欄が壊れている #1 の UNALIGN は既定の名前になる。
    /// </summary>
    [Fact]
    public void Pc98_PrefersKrnThenTocThenDefault()
    {
        if (!File.Exists(Pc98))
            return;
        var n = RosterDocument.Open(Pc98).Scenario.Names;

        Assert.Equal(new[] { "NO RACE", "HUMAN", "ELF", "DWARF", "GNOME", "HOBBIT" }, n.Races);
        Assert.Equal(new[] { "FIGHTER", "MAGE", "PRIEST", "THIEF", "BISHOP", "SAMURAI", "LORD", "NINJA" }, n.Classes);
        Assert.Equal(new[] { "OK", "AFRAID", "ASLEEP", "PARALYZED", "PETRIFIED", "DEAD", "ASHES", "LOST" }, n.Statuses);
        Assert.Equal(new[] { "なし", "GOOD", "NEUTRAL", "EVIL" }, n.Alignments);
        Assert.Contains("ASCII.KRN", n.Source);
        Assert.Contains("SCENARIO.DATA", n.Source);
        Assert.Contains("既定", n.Source);
    }

    /// <summary>ASCII.KRN のない Apple II 版は、目次の名前だけを使う。</summary>
    [Fact]
    public void Apple_UsesTocNames()
    {
        if (!File.Exists(Apple1))
            return;
        var n = RosterDocument.Open(Apple1).Scenario.Names;

        Assert.Equal(new[] { "OK", "AFRAID", "ASLEEP", "P-LYZE", "STONED", "DEAD", "ASHES", "LOST" }, n.Statuses);
        Assert.Equal(new[] { "UNALIGN", "GOOD", "NEUTRAL", "EVIL" }, n.Alignments);
        Assert.Equal("SCENARIO.DATA", n.Source);
    }

    [Fact]
    public void ItemClassUse_UsesDiskNames()
    {
        if (!File.Exists(Apple1))
            return;
        var s = RosterDocument.Open(Apple1).Scenario;
        Assert.Equal("FIGHTER・SAMURAI・LORD・NINJA", s.Items[1].ClassUseText(s.Names.Classes)); // LONG SWORD
    }
}
