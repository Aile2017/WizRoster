using WizRoster.Core;
using WizRoster.Core.IO;
using WizRoster.Core.Model;

namespace WizRoster.Tests;

/// <summary>
/// 手元の実イメージで確かめるテスト。イメージがなければ何もせずに通す。
/// 場所は環境変数 WIZROSTER_PC98 / WIZROSTER_APPLE2 で変えられる。
/// </summary>
public class ImageTests : IDisposable
{
    private static readonly string Pc98Image = Environment.GetEnvironmentVariable("WIZROSTER_PC98")
        ?? @"C:\usr\games\np21w\wizardry\Wizardry1-5\Wizardry1.FDI";

    private static readonly string AppleImage = Environment.GetEnvironmentVariable("WIZROSTER_APPLE2")
        ?? @"C:\usr\games\ROMS\Apple II\.APL2_Wizardry\APL2_Wozardry I 2.dsk";

    private static readonly string PcDosImage = Environment.GetEnvironmentVariable("WIZROSTER_PCDOS")
        ?? @"C:\usr\games\ROMS\DOS\dsk\SAVE1.DSK";

    private static readonly string MsxDir = Environment.GetEnvironmentVariable("WIZROSTER_MSX")
        ?? @"C:\usr\games\ROMS\MSX2\.MSX_Wizardry I - Proving Grounds of the Mad Overlord";

    private static string MsxImage(string kind) =>
        Path.Combine(MsxDir, $"MSX_Wizardry I - Proving Grounds of the Mad Overlord({kind} Disk).dsk");

    private readonly string _tempDir = Directory.CreateTempSubdirectory("wizroster-test").FullName;

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string? CopyImage(string source)
    {
        if (!File.Exists(source))
            return null;
        string dest = Path.Combine(_tempDir, Path.GetFileName(source));
        File.Copy(source, dest);
        return dest;
    }

    [Fact]
    public void Pc98_ReadsMasterData()
    {
        string? path = CopyImage(Pc98Image);
        if (path == null)
            return;

        var doc = RosterDocument.Open(path);
        var s = doc.Scenario;

        Assert.Equal("PC-98 FDI", doc.Image.FormatName);
        Assert.Equal(20, s.CharacterCount);
        Assert.Equal(101, s.Items.Count);
        Assert.Equal("LONG SWORD", s.Items[1].Name);
        Assert.Equal(8, s.Items[1].DamageSides);
        Assert.Equal(21, s.MageSpells.Count);
        Assert.Equal(29, s.PriestSpells.Count);
        Assert.Equal("HALITO", s.MageSpells[0].Name);
        Assert.Equal(1, s.MageSpells[0].Bit);
        Assert.Equal(7, s.MageSpells[20].Level);
        Assert.Equal("KALKI", s.PriestSpells[0].Name);
        Assert.Equal(22, s.PriestSpells[0].Bit);
        Assert.Equal(7, s.PriestSpells[28].Level);
        // アイテムの SPELLPWR と呪文ビットが同じ番号であること(ROD OF FLAME = MAHALITO)
        Assert.Equal("MAHALITO", s.FindSpell(s.Items[61].SpellPower)?.Name);
    }

    [Fact]
    public void Pc98_SaveRoundTripChangesOnlyTheCharacter()
    {
        string? path = CopyImage(Pc98Image);
        if (path == null)
            return;
        byte[] before = File.ReadAllBytes(path);

        var doc = RosterDocument.Open(path);
        var c = doc.Characters[0];
        Assert.False(c.IsEmpty);
        c.Gold = 123_456_789;
        c.SetAttribute(4, 17);
        Assert.True(doc.IsDirty(0));
        string? backup = doc.Save();

        Assert.NotNull(backup);
        Assert.Equal(before, File.ReadAllBytes(backup!));
        Assert.False(doc.IsDirty(0));

        byte[] after = File.ReadAllBytes(path);
        Assert.Equal(before.Length, after.Length);
        int recordStart = 82432; // スロット 0
        for (int i = 0; i < before.Length; i++)
        {
            if (i < recordStart || i >= recordStart + Character.RecordSize)
                Assert.True(before[i] == after[i], $"位置 {i} が変わっています。");
        }

        var reopened = RosterDocument.Open(path).Characters[0];
        Assert.Equal(123_456_789, reopened.Gold);
        Assert.Equal(17, reopened.GetAttribute(4));
    }

    [Fact]
    public void Pc98_SaveWithoutChangesKeepsFileIdentical()
    {
        string? path = CopyImage(Pc98Image);
        if (path == null)
            return;
        byte[] before = File.ReadAllBytes(path);
        RosterDocument.Open(path).Save(backup: false);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void Apple2_ReadsRoster()
    {
        string? path = CopyImage(AppleImage);
        if (path == null)
            return;

        var doc = RosterDocument.Open(path);
        Assert.Equal(20, doc.Scenario.CharacterCount);
        Assert.Equal("MURAMASA BLADE", doc.Scenario.Items[86].Name);
        Assert.Equal("STYX", doc.Characters[6].Name);
        Assert.Equal(3, doc.Characters[6].Race);
        Assert.Equal(154_647, doc.Characters[6].Experience);
    }

    [Fact]
    public void PcDos_ReadsNamesFromKrn()
    {
        string? path = CopyImage(PcDosImage);
        if (path == null)
            return;

        var doc = RosterDocument.Open(path);
        var s = doc.Scenario;
        Assert.Equal(46, s.ItemRecordSize);
        Assert.Equal("ASCII.KRN", s.ItemNameSource);
        Assert.Equal("ASCII.KRN", s.SpellNameSource);
        Assert.Equal(101, s.Items.Count);
        Assert.Equal("LONG SWORD", s.Items[1].Name);
        Assert.Equal("SWORD", s.Items[1].UnknownName);
        Assert.Equal(8, s.Items[1].DamageSides);
        Assert.Equal("MURAMASA BLADE!", s.Items[86].Name);
        Assert.Equal("POTION OF DIOS", s.Items[15].Name);
        Assert.NotNull(s.Items[15].NameNote);

        Assert.Equal(21, s.MageSpells.Count);
        Assert.Equal(29, s.PriestSpells.Count);
        Assert.Equal("HALITO", s.MageSpells[0].Name);
        Assert.Equal(1, s.MageSpells[0].Level);
        Assert.Equal("TILTOWAIT", s.MageSpells[20].Name);
        Assert.Equal(7, s.MageSpells[20].Level);
        Assert.Equal("KALKI", s.PriestSpells[0].Name);
        Assert.Equal(22, s.PriestSpells[0].Bit);
        Assert.Equal(1, s.PriestSpells[0].Level);
        Assert.Equal("KADORTO", s.PriestSpells[28].Name);
        Assert.Equal(7, s.PriestSpells[28].Level);

        Assert.Equal("F1", doc.Characters[0].Name);
        Assert.Equal(107, doc.Characters[0].Gold);
    }

    [Fact]
    public void PcDos_SaveChangesOnlyTheCharacter()
    {
        string? path = CopyImage(PcDosImage);
        if (path == null)
            return;
        byte[] before = File.ReadAllBytes(path);

        var doc = RosterDocument.Open(path);
        doc.Characters[0].Gold = 9999;
        doc.Save(backup: false);

        byte[] after = File.ReadAllBytes(path);
        const int recordStart = 120832; // SAVE1.DSK のスロット 0
        for (int i = 0; i < before.Length; i++)
        {
            if (i < recordStart || i >= recordStart + Character.RecordSize)
                Assert.True(before[i] == after[i], $"位置 {i} が変わっています。");
        }
        Assert.Equal(9999, RosterDocument.Open(path).Characters[0].Gold);
    }

    /// <summary>Game Disk には古いディレクトリの写しが残っているので、正しいほうを選べること。</summary>
    [Fact]
    public void Msx_GameDisk_SkipsStaleDirectory()
    {
        string? path = CopyImage(MsxImage("Game"));
        if (path == null)
            return;

        var doc = RosterDocument.Open(path);
        Assert.Equal(20, doc.Scenario.CharacterCount);
        Assert.Equal(new[] { "F1", "F2", "F3", "P1", "M1", "T1" }, doc.Characters.Take(6).Select(c => c.Name));
        Assert.Equal("SCENARIO.DATA", doc.Scenario.SpellNameSource);
        Assert.Equal("HALITO", doc.Scenario.MageSpells[0].Name);
        Assert.Equal(21, doc.Scenario.MageSpells.Count);
    }

    /// <summary>User Disk は呪文名の表の先頭が壊れているので、ASCII.KRN から読むこと。</summary>
    [Fact]
    public void Msx_UserDisk_FallsBackToKrnForDamagedSpellTable()
    {
        string? path = CopyImage(MsxImage("User"));
        if (path == null)
            return;
        byte[] before = File.ReadAllBytes(path);

        var doc = RosterDocument.Open(path);
        var s = doc.Scenario;
        Assert.Equal("SCENARIO.DATA", s.ItemNameSource);
        Assert.Equal("ASCII.KRN", s.SpellNameSource);
        Assert.Equal(21, s.MageSpells.Count);
        Assert.Equal(29, s.PriestSpells.Count);
        Assert.Equal("HALITO", s.MageSpells[0].Name);
        Assert.Equal("MOGREF", s.MageSpells[1].Name);
        Assert.Equal("NINJA TURTLE", doc.Characters[0].Name);

        doc.Characters[0].Gold = 4321;
        doc.Save(backup: false);
        byte[] after = File.ReadAllBytes(path);
        const int recordStart = 500736; // スロット 0
        for (int i = 0; i < before.Length; i++)
        {
            if (i < recordStart || i >= recordStart + Character.RecordSize)
                Assert.True(before[i] == after[i], $"位置 {i} が変わっています。");
        }
        Assert.Equal(4321, RosterDocument.Open(path).Characters[0].Gold);
    }

    [Fact]
    public void Apple2_SaveRoundTrip()
    {
        string? path = CopyImage(AppleImage);
        if (path == null)
            return;
        byte[] before = File.ReadAllBytes(path);

        var doc = RosterDocument.Open(path);
        doc.Characters[6].Gold = 2468;
        doc.Characters[6].SetAttribute(0, 17);
        doc.Save(backup: false);

        byte[] after = File.ReadAllBytes(path);
        int changed = Enumerable.Range(0, before.Length).Count(i => before[i] != after[i]);
        Assert.InRange(changed, 1, Character.RecordSize);
        var c = RosterDocument.Open(path).Characters[6];
        Assert.Equal("STYX", c.Name);
        Assert.Equal(2468, c.Gold);
        Assert.Equal(17, c.GetAttribute(0));
    }

    [Fact]
    public void Json_RoundTripPreservesRaw()
    {
        string? path = CopyImage(Pc98Image);
        if (path == null)
            return;
        var doc = RosterDocument.Open(path);
        var original = doc.Characters[0];

        string json = Path.Combine(_tempDir, "c.json");
        CharacterFile.Save(json, original, doc.Scenario);
        var loaded = CharacterFile.Load(json);
        Assert.Equal(original.Raw, loaded.Raw);

        string raw = Path.Combine(_tempDir, "c.wchr");
        CharacterFile.Save(raw, original, doc.Scenario);
        Assert.Equal(original.Raw, CharacterFile.Load(raw).Raw);
    }
}
