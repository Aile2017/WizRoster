using WizRoster.Core;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>
/// プログラムの解析で分かったセービングスローの計算式を、実キャラクタの値で確かめる(読み込みだけ)。
/// 20 − レベル÷5 − 運÷6 から職業・種族の分を引き、どれも 1 未満にしない。
/// </summary>
public class SavingThrowTests
{
    // 職業ごとの (番号, 引く値)。Apple II 版・PC-98 版の UTILITIE の分岐表と同じ。
    private static readonly (int, int)[][] ClassBonus =
    [
        [(0, 3)],                                   // 戦士
        [(4, 3)],                                   // 魔術師
        [(1, 3)],                                   // 僧侶
        [(3, 3)],                                   // 盗賊
        [(2, 2), (4, 2), (1, 2)],                   // 司教
        [(0, 2), (4, 2)],                           // 侍
        [(0, 2), (1, 2)],                           // 君主
        [(0, 3), (1, 2), (2, 4), (3, 3), (4, 2)],   // 忍者
    ];

    private static readonly (int, int)[][] RaceBonus =
    [
        [],          // 未設定
        [(0, 1)],    // 人間
        [(2, 2)],    // エルフ
        [(3, 4)],    // ドワーフ
        [(1, 2)],    // ノーム
        [(4, 3)],    // ホビット
    ];

    private static int[] Expected(Character c)
    {
        int b = Math.Max(1, 20 - c.Level / 5 - c.GetAttribute(5) / 6);
        var s = Enumerable.Repeat(b, 5).ToArray();
        foreach (var (i, v) in ClassBonus[c.Class].Concat(RaceBonus[c.Race]))
            s[i] = Math.Max(1, s[i] - v);
        return s;
    }

    [Theory]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "CALEB")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "BOREE")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "ODDLER")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "NAUTUS")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "ZIL")]
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE2.DSK", "GUYDOMAN")]
    [InlineData(@"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI", "T2")]
    public void Formula_MatchesRealCharacters(string path, string name)
    {
        if (!File.Exists(path))
            return;
        var c = RosterDocument.Open(path).Characters.First(x => x.Name == name);
        Assert.Equal(Expected(c), Enumerable.Range(0, 5).Select(c.GetSavingThrow).ToArray());
    }

    [Fact]
    public void Labels_FollowAnalysis()
    {
        Assert.Equal(["毒・麻痺・即死", "石化", "未使用", "ブレス・ガス", "呪文・罠"], GameNames.SavingThrows);
        Assert.Equal(5, GameNames.SavingThrowDetails.Length);
    }
}
