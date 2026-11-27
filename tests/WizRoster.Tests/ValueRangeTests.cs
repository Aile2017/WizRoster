using WizRoster.Core;
using WizRoster.Core.IO;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

public class ValueRangeTests
{
    private const string Pc98 = @"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI";

    private static CharacterFile.CharacterDto ValidDto() => CharacterFile.ToDto(Character.CreateNew("TEST"), null);

    [Fact]
    public void Json_ValidValuesAreAccepted()
    {
        var dto = ValidDto();
        dto.Level = ValueRanges.IntegerMax;
        dto.ArmorClass = -10;
        dto.Gold = WizBytes.WizLongMax;
        var c = CharacterFile.FromDto(dto);
        Assert.Equal(ValueRanges.IntegerMax, c.Level);
        Assert.Equal(-10, c.ArmorClass);
        Assert.Equal(WizBytes.WizLongMax, c.Gold);
    }

    [Fact]
    public void Json_OutOfRangeValuesAreAllReported()
    {
        var dto = ValidDto();
        dto.Level = 40000;
        dto.Hp = -1;
        dto.Gold = WizBytes.WizLongMax + 1;
        dto.Attributes[0] = 32;
        dto.MageSpellPoints[0] = 10;
        dto.Name = "TOO LONG NAME 1234";

        var ex = Assert.Throws<InvalidDataException>(() => CharacterFile.FromDto(dto));
        Assert.Contains("レベル 40,000", ex.Message);
        Assert.Contains("HP -1", ex.Message);
        Assert.Contains("所持金", ex.Message);
        Assert.Contains("能力値 力 32", ex.Message);
        Assert.Contains("魔術師呪文の回数 Lv1 10", ex.Message);
        Assert.Contains("名前", ex.Message);
    }

    /// <summary>Extra は「その他」に出している位置だけ。名前など、ほかの位置は書き換えさせない。</summary>
    [Fact]
    public void Json_ExtraOnlyAllowsKnownOffsets()
    {
        var dto = ValidDto();
        dto.Extra["+0"] = 5;
        var ex = Assert.Throws<InvalidDataException>(() => CharacterFile.FromDto(dto));
        Assert.Contains("+0", ex.Message);

        dto = ValidDto();
        dto.Extra["+182"] = 40000; // SWINGCNT は符号付き
        Assert.Throws<InvalidDataException>(() => CharacterFile.FromDto(dto));

        dto = ValidDto();
        dto.Extra["+190"] = 0xFFFF; // ビット列は 0〜65535
        Assert.Equal(0xFFFF, CharacterFile.FromDto(dto).Word(190));
    }

    [Fact]
    public void Rules_WarnAboutOutOfRangeDataOnDisk()
    {
        if (!File.Exists(Pc98))
            return;
        var scenario = RosterDocument.Open(Pc98).Scenario;

        var c = Character.CreateNew("TEST");
        c.SetWord(Character.OffLevel, 40000);
        c.SetWord(Character.OffGold, 12345); // 1 ワード目が 9,999 を超えている
        var warnings = CharacterRules.Check(c, scenario);

        Assert.Contains(warnings, w => w.Contains("レベルの値 40,000 は範囲外") && w.Contains("-25,536"));
        Assert.Contains(warnings, w => w.Contains("所持金のデータが形式として正しくありません"));

        var ok = Character.CreateNew("TEST");
        Assert.DoesNotContain(CharacterRules.Check(ok, scenario), w => w.Contains("範囲外です(0〜") || w.Contains("形式として"));
    }
}
