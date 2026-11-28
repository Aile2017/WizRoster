using WizRoster.Core;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>
/// モンスター表(ZENEMY, TENEMY)の読み込み。手元の実イメージで確かめる(なければ何もせずに通す)。
/// 名前付きの版(Apple II・PC-98)は 158 バイト、名前なしの版(PC-DOS)は 94 バイトで名前は ASCII.KRN の 13000 + 4n。
/// </summary>
public class MonsterTests
{
    [Theory]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk")]
    [InlineData(@"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE1.DSK")]
    public void Wizardry1_Monsters(string path)
    {
        if (!File.Exists(path))
            return;
        var monsters = RosterDocument.Open(path).Scenario.Monsters;
        Assert.Equal(101, monsters.Count);

        var slime = monsters[0];
        Assert.Equal("BUBBLY SLIME", slime.Name);
        Assert.Equal("BUBBLY SLIMES", slime.NamePlural);
        Assert.Equal("SLIME", slime.UnknownName);
        Assert.Equal(new Dice(2, 2, 0), slime.GroupSize);
        Assert.Equal(new Dice(1, 3, 1), slime.HitPoints);
        Assert.Equal(8, slime.Class);   // 動物
        Assert.Equal(12, slime.ArmorClass);
        Assert.Equal([new Dice(1, 1, 0)], slime.Attacks);
        Assert.Equal(1, slime.Team);    // ORC
        Assert.Equal(10, slime.TeamPercent);

        var dragon = monsters[81];
        Assert.Equal("FIRE DRAGON", dragon.Name);
        Assert.Equal(1, dragon.Breath); // 炎
        Assert.Equal(7, dragon.Class);  // 竜
        Assert.Equal(5, dragon.MageSpellLevel);
        Assert.Equal(3, dragon.Attacks.Count);

        var werdna = monsters[96];
        // Apple II・PC-98 の SCENARIO.DATA は "W E R D N A"、アスキー移植版の ASCII.KRN は "**W*E*R*D*N*A**"
        Assert.Equal("WERDNA", werdna.Name.Replace(" ", "").Replace("*", ""));
        Assert.Equal(new Dice(10, 10, 20), werdna.HitPoints);
        Assert.Equal(-7, werdna.ArmorClass);
        Assert.Equal(7, werdna.MageSpellLevel);
        Assert.Equal(7, werdna.PriestSpellLevel);
        Assert.Equal(70, werdna.SpellResistance);

        Assert.Equal(1, monsters[46].SpecialBits); // MEDUSALIZARD: 石化
        Assert.All(monsters, m => Assert.Equal(0, m.Experience)); // #1 は経験値を持たない
    }

    [Fact]
    public void BitNames_ListsSetBits()
    {
        Assert.Equal("なし", GameNames.BitNames(0, GameNames.MonsterSpecials));
        Assert.Equal("眠る・逃げる", GameNames.BitNames(0x30, GameNames.MonsterSpecials));
        Assert.Equal("竜・不死", GameNames.BitNames((1 << 7) | (1 << 10), GameNames.MonsterClasses));
    }
}
