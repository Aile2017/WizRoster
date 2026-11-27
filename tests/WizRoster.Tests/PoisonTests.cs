using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>
/// 毒(+200)と、迷宮に置き去りのときの位置(+200〜+204)。
/// ゲームのプログラムで、毒は +200 に入り、置き去りにすると +200〜+204 が位置で上書きされることを確かめた。
/// </summary>
public class PoisonTests
{
    [Fact]
    public void Poison_IsStoredAt200()
    {
        var c = Character.CreateNew("TEST");
        c.Poison = 3;
        Assert.Equal(3, c.Word(200));
        Assert.False(c.IsLeftInMaze);
    }

    [Fact]
    public void Cure_ClearsStatusHpAndPoison()
    {
        var c = Character.CreateNew("TEST");
        c.Status = 5;
        c.Hp = 0;
        c.Poison = 2;
        c.Cure();
        Assert.Equal(0, c.Status);
        Assert.Equal(c.HpMax, c.Hp);
        Assert.Equal(0, c.Poison);
    }

    /// <summary>置き去りのとき(+204 の階が 0 以外)は、+200 は位置 X なので Cure で消さない。</summary>
    [Theory]
    [InlineData(5, 7, 3)]     // X=5, Y=7, 3 階
    [InlineData(-1, -1, -1)]  // 行方不明
    public void Cure_KeepsMazeLocationWhenLeftInMaze(int x, int y, int level)
    {
        var c = Character.CreateNew("TEST");
        c.SetWord(Character.OffLeftMazeX, x);
        c.SetWord(Character.OffLeftMazeY, y);
        c.SetWord(Character.OffLeftMazeLevel, level);
        Assert.True(c.IsLeftInMaze);

        c.Status = 5;
        c.Cure();
        Assert.Equal(0, c.Status);
        Assert.Equal(x, c.SignedWord(Character.OffLeftMazeX));
        Assert.Equal(y, c.SignedWord(Character.OffLeftMazeY));
        Assert.Equal(level, c.SignedWord(Character.OffLeftMazeLevel));
    }

    [Fact]
    public void RawFields_DescribePoisonAndLocation()
    {
        var f = RawFields.Extra.Single(r => r.Offset == 200);
        Assert.Contains("毒", f.Description);
        Assert.True(f.Signed);
        Assert.Contains("階", RawFields.Extra.Single(r => r.Offset == 204).Description);
    }
}
