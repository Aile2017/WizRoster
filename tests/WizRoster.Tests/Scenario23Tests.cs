using WizRoster.Core;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>#2・#3 と、呪文名の表の探し方(読み込みだけ)。イメージがなければ何もせずに通す。</summary>
public class Scenario23Tests
{
    private const string Apple = @"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\";
    private const string Dos = @"C:\usr\games\ROMS\DOS\dsk\";
    private const string Pc98 = @"C:\usr\games\np21w\wizardry\Wizardry1-5\";

    /// <summary>呪文名の表を SCENARIO.DATA から読めること(Apple II #3 は表の位置が #1 と違う)。</summary>
    [Theory]
    [InlineData(Apple + "APL2_Wozardry I 2.dsk")]
    [InlineData(Apple + "APL2_Wozardry II 2.dsk")]
    [InlineData(Apple + "APL2_Wozardry III 2.dsk")]
    [InlineData(Pc98 + "Wizardry1.FDI")]
    public void SpellTable_IsFoundInScenarioData(string path)
    {
        if (!File.Exists(path))
            return;
        var s = RosterDocument.Open(path).Scenario;
        Assert.Equal("SCENARIO.DATA", s.SpellNameSource);
        Assert.Equal(21, s.MageSpells.Count);
        Assert.Equal(29, s.PriestSpells.Count);
        Assert.Equal("HALITO", s.MageSpells[0].Name);
        Assert.Equal(7, s.MageSpells[^1].Level);
        Assert.Equal("KALKI", s.PriestSpells[0].Name);
        Assert.Equal("KADORTO", s.PriestSpells[^1].Name);
        Assert.Equal(7, s.PriestSpells[^1].Level);
    }

    /// <summary>#3 の「1000 + アイテム番号」の持ち物を解決できること(SAVE3 の 6502 は戦士の装備一式)。</summary>
    [Fact]
    public void Wiz3_ResolvesItemNumbersPlus1000()
    {
        string path = Dos + "SAVE3.DSK";
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        var s = doc.Scenario;
        var c = doc.Characters[14];
        Assert.Equal("6502", c.Name);

        var items = c.GetItems();
        Assert.Equal(1028, items[0].ItemIndex);
        Assert.True(s.IsFlaggedItemNumber(1028));
        Assert.Equal("SALLET", s.ResolveItem(1028)!.Name);
        Assert.Equal(new[] { "SALLET", "HEATER+1", "BREASTPLATE+1", "BATTLE AXE+1", "GLOVES OF IRON" },
            items.Select(p => s.ResolveItem(p.ItemIndex)!.Name));
        Assert.All(items, p => Assert.True(s.ResolveItem(p.ItemIndex)!.CanBeUsedBy(c.Class)));

        Assert.DoesNotContain(CharacterRules.Check(c, s), w => w.Contains("アイテム表にありません"));
        Assert.False(s.IsFlaggedItemNumber(28)); // 普通の番号はそのまま
    }

    [Fact]
    public void Wiz2_PcDosCharactersHaveNoWarnings()
    {
        string path = Dos + "SAVE2.DSK";
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        Assert.Equal(130, doc.Scenario.Items.Count);
        Assert.Equal("KOD'S ARMOR", doc.Scenario.Items[129].Name);
        var active = doc.Characters.Where(c => !c.IsEmpty && c.Status == 0).ToList();
        Assert.NotEmpty(active);
        Assert.All(active, c => Assert.Empty(CharacterRules.Check(c, doc.Scenario)));
    }
}
