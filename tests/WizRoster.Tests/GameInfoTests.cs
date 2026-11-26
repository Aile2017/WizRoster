using WizRoster.Core;

namespace WizRoster.Tests;

/// <summary>
/// 手元の実イメージで、機種とシナリオの判別を確かめる(読み込みだけ)。イメージがなければ何もせずに通す。
/// </summary>
public class GameInfoTests
{
    private const string Pc98 = @"C:\usr\games\np21w\wizardry\Wizardry1-5\";
    private const string Dos = @"C:\usr\games\ROMS\DOS\dsk\";
    private const string Apple = @"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\";
    private const string Pc88 = @"C:\usr\games\ROMS\NEC PC-8801\";
    private const string Msx = @"C:\usr\games\ROMS\MSX2\";

    private const string Msx1 = Msx + @".MSX_Wizardry I - Proving Grounds of the Mad Overlord\MSX_Wizardry I - Proving Grounds of the Mad Overlord";
    private const string Msx2 = Msx + @".MSX_Wizardry II - Knight of Diamonds\MSX_Wizardry II - Knight of Diamonds";
    private const string Msx3 = Msx + @".MSX_Wizardry III - Legacy of Llylgamyn\MSX_Wizardry III - Legacy of Llylgamyn";

    [Theory]
    [InlineData(Pc98 + "Wizardry1.FDI", "PC-98", 1)]
    [InlineData(Pc98 + "Wizardry2.FDI", "PC-98", 2)]
    [InlineData(Pc98 + "Wizardry3.FDI", "PC-98", 3)]
    [InlineData(Dos + "SAVE1.DSK", "PC-DOS", 1)]
    [InlineData(Dos + "SAVE2.DSK", "PC-DOS", 2)]
    [InlineData(Dos + "SAVE3.DSK", "PC-DOS", 3)]
    [InlineData(Dos + "WIZ1.DSK", "PC-DOS", 1)]
    [InlineData(Dos + "WIZ2.DSK", "PC-DOS", 2)]
    [InlineData(Dos + "WIZ3.DSK", "PC-DOS", 3)]
    [InlineData(Apple + "APL2_Wozardry I 2.dsk", "Apple II", 1)]
    [InlineData(Apple + "APL2_Wozardry II 2.dsk", "Apple II", 2)]
    [InlineData(Apple + "APL2_Wozardry III 2.dsk", "Apple II", 3)]
    [InlineData(Pc88 + "PC88_Wizardry I - Proving Grounds of the Mad Overlord.D88", "PC-8801", 1)]
    [InlineData(Pc88 + "PC88_Wizardry II - Knight of Diamonds.D88", "PC-8801", 2)]
    [InlineData(Pc88 + "PC88_Wizardry III - Legacy of Llylgamyn.D88", "PC-8801", 3)]
    [InlineData(Msx1 + "(Game Disk).dsk", "MSX2", 1)]
    [InlineData(Msx1 + "(User Disk).dsk", "MSX2", 1)]
    [InlineData(Msx2 + "(Game Disk).dsk", "MSX2", 2)]
    [InlineData(Msx2 + "(User Disk).dsk", "MSX2", 3)] // フォルダは #2 だが、中身は #3
    [InlineData(Msx3 + "(Game Disk).dsk", "MSX2", 3)]
    [InlineData(Msx3 + "(User Disk).dsk", "MSX2", 3)]
    public void Identify_RealImages(string path, string platform, int scenario)
    {
        if (!File.Exists(path))
            return;
        var game = RosterDocument.Open(path).Game;
        Assert.Equal(platform, game.Platform);
        Assert.Equal(scenario, game.Scenario);
    }
}
