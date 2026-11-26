using WizRoster.Core.Model;

namespace WizRoster.Tests;

public class CharacterTests
{
    [Fact]
    public void WizLong_IsBase10000LowWordFirst()
    {
        byte[] b = new byte[6];
        WizBytes.SetWizLong(b, 0, 740_000);
        Assert.Equal(new byte[] { 0, 0, 74, 0, 0, 0 }, b);
        Assert.Equal(740_000, WizBytes.WizLong(b, 0));

        WizBytes.SetWizLong(b, 0, WizBytes.WizLongMax);
        Assert.Equal(WizBytes.WizLongMax, WizBytes.WizLong(b, 0));
    }

    [Fact]
    public void Attributes_ArePacked5BitsThreePerWord()
    {
        var c = Character.CreateBlank();
        // 0x3187 0x3DC6: エルフ司教 A の値。力7 知恵12 信仰心12 / 体力6 素早さ14 運15
        WizBytes.SetU16(c.Raw, Character.OffAttrib, 0x3187);
        WizBytes.SetU16(c.Raw, Character.OffAttrib + 2, 0x3DC6);
        Assert.Equal(new[] { 7, 12, 12, 6, 14, 15 }, Enumerable.Range(0, 6).Select(c.GetAttribute));

        c.SetAttribute(4, 31);
        Assert.Equal(31, c.GetAttribute(4));
        Assert.Equal(6, c.GetAttribute(3));
        Assert.Equal(15, c.GetAttribute(5));
    }

    [Fact]
    public void SetItems_CompactsAndClearsRest()
    {
        var c = Character.CreateBlank();
        c.SetItems([new Possession(true, false, true, 1), new Possession(false, true, false, 100)]);
        Assert.Equal(2, c.ItemCount);
        Assert.Equal(new Possession(false, true, false, 100), c.GetItem(1));
        Assert.All(c.Raw.Skip(Character.OffItems + 16).Take(48), b => Assert.Equal(0, b));
    }

    [Fact]
    public void SpellBits_MapToBytes()
    {
        var c = Character.CreateBlank();
        c.SetKnowsSpell(1, true);
        c.SetKnowsSpell(50, true);
        Assert.Equal(0x02, c.Raw[Character.OffSpells]);
        Assert.Equal(0x04, c.Raw[Character.OffSpells + 6]);
        Assert.True(c.KnowsSpell(50));
        Assert.False(c.KnowsSpell(49));
    }

    [Fact]
    public void Name_RejectsTooLong()
    {
        var c = Character.CreateBlank();
        Assert.Throws<ArgumentException>(() => c.Name = new string('A', 16));
        c.Name = "ABC";
        Assert.Equal(3, c.Raw[0]);
        Assert.False(c.IsEmpty);
    }
}
