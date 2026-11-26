using WizRoster.Core.Disk;
using WizRoster.Core.Krn;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;

namespace WizRoster.Core;

/// <summary>
/// 開いているイメージ 1 つ分の編集状態。編集は <see cref="Characters"/> のコピーに対して行い、
/// <see cref="Save"/> でイメージに書き戻す。
/// </summary>
public sealed class RosterDocument
{
    private readonly Character[] _saved;

    private RosterDocument(string path, IBlockImage image, ScenarioFile scenario, string volumeName)
    {
        FilePath = path;
        Image = image;
        Scenario = scenario;
        VolumeName = volumeName;
        _saved = Enumerable.Range(0, scenario.CharacterCount).Select(scenario.ReadCharacter).ToArray();
        Characters = _saved.Select(c => c.Clone()).ToArray();
        EmptyTemplate = _saved.FirstOrDefault(c => c.IsEmpty)?.Clone() ?? Character.CreateBlank();
    }

    public string FilePath { get; private set; }
    public IBlockImage Image { get; }
    public ScenarioFile Scenario { get; }
    public string VolumeName { get; }
    public Character[] Characters { get; }

    /// <summary>空きスロットに書く内容。開いたイメージの空きスロットの内容を使う。</summary>
    public Character EmptyTemplate { get; }

    public static RosterDocument Open(string path)
    {
        var opened = ImageLoader.Open(path);
        var scenario = new ScenarioFile(opened.Image, opened.ScenarioEntry, LoadMessages(opened));
        return new RosterDocument(path, opened.Image, scenario, opened.Directory.VolumeName);
    }

    /// <summary>同じボリュームの ASCII.KRN(アスキー移植版のメッセージファイル)。なければ null。</summary>
    private static KrnMessageFile? LoadMessages(ImageLoader.OpenResult opened)
    {
        var entry = opened.Directory.Find(KrnMessageFile.FileName);
        if (entry == null || entry.LastBlock > opened.Image.BlockCount)
            return null;
        try
        {
            return new KrnMessageFile(opened.Image.ReadBytes(entry.FirstBlock, 0, entry.BlockLength * BlockImageExtensions.BlockSize));
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public bool IsDirty(int slot) => !Characters[slot].Raw.AsSpan().SequenceEqual(_saved[slot].Raw);

    public bool IsAnyDirty => Enumerable.Range(0, Characters.Length).Any(IsDirty);

    /// <summary>スロットの編集を取り消して、最後に保存(または読み込み)した内容に戻す。</summary>
    public void Revert(int slot) => _saved[slot].Raw.CopyTo(Characters[slot].Raw, 0);

    public void CopySlot(int from, int to) => Characters[from].Raw.CopyTo(Characters[to].Raw, 0);

    public void SwapSlots(int a, int b)
    {
        byte[] tmp = (byte[])Characters[a].Raw.Clone();
        Characters[b].Raw.CopyTo(Characters[a].Raw, 0);
        tmp.CopyTo(Characters[b].Raw, 0);
    }

    public void ClearSlot(int slot) => EmptyTemplate.Raw.CopyTo(Characters[slot].Raw, 0);

    public void Replace(int slot, Character character) => character.Raw.CopyTo(Characters[slot].Raw, 0);

    /// <summary>
    /// 全スロットをイメージに書き込み、ファイルに保存する。
    /// 保存先が既にあれば、先に「元の名前.bak_日時」としてコピーを残す。
    /// </summary>
    /// <returns>作ったバックアップのパス(作らなかったときは null)。</returns>
    public string? Save(string? path = null, bool backup = true)
    {
        path ??= FilePath;
        for (int i = 0; i < Characters.Length; i++)
            Scenario.WriteCharacter(i, Characters[i]);

        string? backupPath = null;
        if (backup && File.Exists(path))
        {
            backupPath = $"{path}.bak_{DateTime.Now:yyyyMMdd_HHmmss}";
            File.Copy(path, backupPath, overwrite: false);
        }

        string temp = path + ".tmp";
        File.WriteAllBytes(temp, Image.FileBytes);
        File.Move(temp, path, overwrite: true);

        for (int i = 0; i < Characters.Length; i++)
            Characters[i].Raw.CopyTo(_saved[i].Raw, 0);
        FilePath = path;
        return backupPath;
    }
}
