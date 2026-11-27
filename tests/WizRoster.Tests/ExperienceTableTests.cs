using WizRoster.Core;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

public class ExperienceTableTests
{
    private const string Apple1 = @"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk";
    private const string Dos2 = @"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK";

    private const int Fighter = 0, Thief = 3, Lord = 6, Ninja = 7;

    [Fact]
    public void Apple1_TableValuesAndLevels()
    {
        if (!File.Exists(Apple1))
            return;
        var t = RosterDocument.Open(Apple1).Scenario.Experience;
        Assert.NotNull(t);

        Assert.Equal(0, t!.RequiredExperience(Fighter, 1));
        Assert.Equal(1000, t.RequiredExperience(Fighter, 2));
        Assert.Equal(400_075, t.RequiredExperience(Fighter, 13));
        Assert.Equal(400_075 + 7 * 289_709, t.RequiredExperience(Fighter, 20));

        // 境目
        Assert.Equal(13, t.LevelFor(Fighter, 400_075));
        Assert.Equal(12, t.LevelFor(Fighter, 400_074));
        Assert.Equal(14, t.LevelFor(Fighter, 400_075 + 289_709));
        Assert.Equal(1, t.LevelFor(Fighter, 999));
        Assert.Equal(2, t.LevelFor(Fighter, 1000));

        // 同じ経験値でも職業でレベルが違う(LORD1 と THIEF1 は 59,908)
        Assert.Equal(8, t.LevelFor(Lord, 59_908));
        Assert.Equal(9, t.LevelFor(Thief, 59_908));
        Assert.Equal(13, t.LevelFor(Ninja, 740_000));
    }

    /// <summary>レベルだけを書き換えたキャラクタには警告が出て、普通に育てたキャラクタには出ないこと。</summary>
    [Fact]
    public void Rules_WarnWhenLevelExceedsExperience()
    {
        if (File.Exists(Apple1))
        {
            var doc = RosterDocument.Open(Apple1);
            var killer = doc.Characters[7];
            Assert.Equal("KILLER", killer.Name);
            Assert.Contains(CharacterRules.Check(killer, doc.Scenario), w => w.Contains("Lv 13 相当"));
        }
        if (File.Exists(Dos2))
        {
            var doc = RosterDocument.Open(Dos2);
            var caleb = doc.Characters[0];
            Assert.Equal("CALEB", caleb.Name);
            Assert.DoesNotContain(CharacterRules.Check(caleb, doc.Scenario), w => w.Contains("相当"));
            Assert.Equal("経験値は Lv 12 相当 / Lv 13 まであと 127,660",
                doc.Scenario.Experience!.Describe(caleb.Class, caleb.Level, caleb.Experience));
        }
    }

    [Fact]
    public void Describe_ShowsPendingLevelUp()
    {
        if (!File.Exists(Dos2))
            return;
        var t = RosterDocument.Open(Dos2).Scenario.Experience!;
        Assert.Equal("経験値は Lv 3 相当(宿屋でレベルが上がる見込み)", t.Describe(Fighter, 1, 1724));
    }

    [Theory]
    [InlineData(@"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI")]
    [InlineData(@"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry3.FDI")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE1.DSK")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE3.DSK")]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry III 2.dsk")]
    [InlineData(@"C:\usr\games\ROMS\NEC PC-8801\PC88_Wizardry I - Proving Grounds of the Mad Overlord.D88")]
    [InlineData(@"C:\usr\games\ROMS\MSX2\.MSX_Wizardry I - Proving Grounds of the Mad Overlord\MSX_Wizardry I - Proving Grounds of the Mad Overlord(User Disk).dsk")]
    public void Table_IsReadableOnAllPlatforms(string path)
    {
        if (!File.Exists(path))
            return;
        var t = RosterDocument.Open(path).Scenario.Experience;
        Assert.NotNull(t);
        Assert.True(t!.RequiredExperience(Fighter, 2) > 0);
    }

    [Fact]
    public void TryParse_RejectsNonAscending()
    {
        var data = new byte[ExperienceTable.ByteLength];
        for (int c = 0; c < ExperienceTable.ClassCount; c++)
        {
            for (int i = 0; i < ExperienceTable.EntriesPerClass; i++)
                WizBytes.SetWizLong(data, (c * ExperienceTable.EntriesPerClass + i) * 6, 100 + i * 10);
        }
        Assert.NotNull(ExperienceTable.TryParse(data));

        WizBytes.SetWizLong(data, 5 * 6, 1); // 戦士の Lv6 を小さくする
        Assert.Null(ExperienceTable.TryParse(data));
    }
}
