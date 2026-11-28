using WizRoster.Core;

namespace WizRoster.Tests;

/// <summary>
/// モンスターの絵(アスキー移植版の 200/400.MONSTERS)。手元の実イメージで確かめる(なければ何もせずに通す)。
/// </summary>
public class MonsterPictureTests
{
    private const string Pc98 = @"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI";
    private const string Pc88 = @"C:\usr\games\ROMS\NEC PC-8801\PC88_Wizardry I - Proving Grounds of the Mad Overlord.D88";
    private const string PcDos = @"C:\usr\games\ROMS\DOS\dsk\SAVE1.DSK";
    private const string Msx = @"C:\usr\games\ROMS\MSX2\.MSX_Wizardry I - Proving Grounds of the Mad Overlord\MSX_Wizardry I - Proving Grounds of the Mad Overlord(User Disk).dsk";

    [Theory]
    [InlineData(Pc98, "400.MONSTERS", 80, 1)]
    [InlineData(Pc88, "200.MONSTERS", 40, 2)]
    public void AsciiPorts_ReadPictures(string path, string file, int height, int aspect)
    {
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        var pictures = doc.MonsterPictures;
        Assert.NotNull(pictures);
        Assert.Equal(file, pictures!.Source);
        Assert.Equal(30, pictures.Count);
        Assert.Null(pictures.Get(0));
        Assert.Null(pictures.Get(31));

        // BUBBLY SLIME の絵は緑(色番号 4)が多い
        var slime = pictures.Get(doc.Scenario.Monsters[0].Picture)!;
        Assert.Equal(96, slime.Width);
        Assert.Equal(height, slime.Height);
        Assert.Equal(aspect, slime.PixelAspect);
        var colored = slime.Pixels.Where(p => p != 0).ToList();
        Assert.True(colored.Count > 500);
        Assert.Equal(4, colored.GroupBy(p => p & 4).OrderByDescending(g => g.Count()).First().Key);
        // ゲームは色 1 と 7 を入れ替えている(色 1 = 白、色 7 = 青)
        Assert.Equal(((byte)255, (byte)255, (byte)255), slime.Palette[1]);
        Assert.Equal(((byte)0, (byte)0, (byte)255), slime.Palette[7]);

        // すべてのモンスターの絵の番号が範囲内
        Assert.All(doc.Scenario.Monsters, m => Assert.NotNull(pictures.Get(m.Picture)));
    }

    [Theory]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk", 20, 50)]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry II 2.dsk", 30, 50)]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry III 2.dsk", 30, 48)] // 文字で組んだ絵
    public void AppleII_ReadsHiresPictures(string path, int count, int height)
    {
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        var pictures = doc.MonsterPictures;
        Assert.NotNull(pictures);
        Assert.Equal("SCENARIO.DATA 区分 6", pictures!.Source);
        Assert.Equal(count, pictures.Count);
        Assert.Equal(0, pictures.FirstNumber);
        Assert.All(doc.Scenario.Monsters, m => Assert.NotNull(pictures.Get(m.Picture)));
        var pic = pictures.Get(doc.Scenario.Monsters[0].Picture)!;
        Assert.Equal((70, height, 1), (pic.Width, pic.Height, pic.PixelAspect));
        Assert.True(pic.Pixels.Count(p => p != 0) > 200);
    }

    [Theory]
    [InlineData(Msx)]
    [InlineData(@"C:\usr\games\ROMS\MSX2\.MSX_Wizardry I - Proving Grounds of the Mad Overlord\MSX_Wizardry I - Proving Grounds of the Mad Overlord(Game Disk).dsk")]
    public void Msx2_ReadsScreen5Pictures(string path)
    {
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        var pictures = doc.MonsterPictures;
        Assert.NotNull(pictures);
        Assert.Equal("200.MONSTERS", pictures!.Source);
        Assert.Equal(30, pictures.Count);
        Assert.Equal(1, pictures.FirstNumber);
        Assert.All(doc.Scenario.Monsters, m => Assert.NotNull(pictures.Get(m.Picture)));
        var pic = pictures.Get(doc.Scenario.Monsters[0].Picture)!;
        Assert.Equal((72, 40, 2), (pic.Width, pic.Height, pic.PixelAspect));
        Assert.True(pic.Pixels.Count(p => p > 1) > 200);
        // ゲームが設定するパレット(KOBOLD の体の色 7 は暗い水色、色 2 は暗い赤)
        Assert.Equal(((byte)0, (byte)145, (byte)145), pic.Palette[7]);
        Assert.Equal(((byte)145, (byte)0, (byte)0), pic.Palette[2]);
    }

    [Theory]
    [InlineData(PcDos)]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\WIZ1.DSK")]
    public void PcDos_ReadsCgaPictures(string path)
    {
        if (!File.Exists(path))
            return;
        var doc = RosterDocument.Open(path);
        var pictures = doc.MonsterPictures;
        Assert.NotNull(pictures);
        Assert.Equal("200.MONSTERS", pictures!.Source);
        Assert.Equal(32, pictures.Count);
        Assert.Equal(1, pictures.FirstNumber);
        Assert.All(doc.Scenario.Monsters, m => Assert.NotNull(pictures.Get(m.Picture)));
        var pic = pictures.Get(doc.Scenario.Monsters[0].Picture)!;
        Assert.Equal((48, 40, 1), (pic.Width, pic.Height, pic.PixelAspect));
        Assert.True(pic.Pixels.Count(p => p != 0) > 200);
    }
}
