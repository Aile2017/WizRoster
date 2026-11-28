using WizRoster.Core;

namespace WizRoster.Tests;

/// <summary>
/// ボルタック商店の在庫数(アイテムの BOLTACXX)。手元の実イメージで、読み書きが在庫の 2 バイトだけを変えることを確かめる。
/// イメージがなければ何もせずに通す。
/// </summary>
public class BoltacStockTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory("wizroster-boltac").FullName;

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData(@"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk")] // 名前付き 78 バイト
    [InlineData(@"C:\usr\games\ROMS\DOS\dsk\SAVE1.DSK")]                             // 名前なし 46 バイト
    [InlineData(@"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI")]
    public void Stock_MatchesItemTableAndRoundTrips(string source)
    {
        if (!File.Exists(source))
            return;
        string path = Path.Combine(_tempDir, Path.GetFileName(source));
        File.Copy(source, path);
        byte[] before = File.ReadAllBytes(path);

        var doc = RosterDocument.Open(path);
        var items = doc.Scenario.Items;
        Assert.Equal(items.Select(i => i.BoltacStock), doc.BoltacStock);
        Assert.False(doc.IsAnyDirty);

        // 在庫 0 の品を 1 つ選んで 7 にする
        int target = Array.FindIndex(doc.BoltacStock, s => s == 0);
        Assert.True(target >= 0);
        doc.BoltacStock[target] = 7;
        Assert.True(doc.IsStockDirty);
        Assert.True(doc.IsAnyDirty);
        doc.Save(backup: false);
        Assert.False(doc.IsAnyDirty);

        byte[] after = File.ReadAllBytes(path);
        var diff = Enumerable.Range(0, before.Length).Where(i => before[i] != after[i]).ToList();
        Assert.InRange(diff.Count, 1, 2);
        Assert.True(diff.Max() - diff.Min() <= 1);

        var reopened = RosterDocument.Open(path);
        Assert.Equal(7, reopened.BoltacStock[target]);
        Assert.Equal(7, reopened.Scenario.Items[target].BoltacStock);

        // −1(無限)も書ける
        reopened.BoltacStock[target] = -1;
        reopened.Save(backup: false);
        Assert.Equal(-1, RosterDocument.Open(path).BoltacStock[target]);
    }
}
