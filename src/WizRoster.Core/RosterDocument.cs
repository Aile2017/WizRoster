using WizRoster.Core.Disk;
using WizRoster.Core.Krn;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;

namespace WizRoster.Core;

/// <summary>1 つのファイルに入っているディスクのうち、開けるもの(D88 など)。</summary>
public sealed record DiskChoice(int Index, string Label, bool HasCharacters, bool IsWriteProtected);

/// <summary>
/// 開いているイメージ 1 つ分の編集状態。編集は <see cref="Characters"/> のコピーに対して行い、
/// <see cref="Save"/> でイメージに書き戻す。
/// </summary>
public sealed class RosterDocument
{
    private readonly Character[] _saved;
    private readonly int[] _savedStock;

    private RosterDocument(string path, IBlockImage image, ScenarioFile scenario, string volumeName,
        Character[] characters, IReadOnlyList<DiskChoice> disks, GameInfo game, MonsterPictures? pictures)
    {
        MonsterPictures = pictures;
        FilePath = path;
        Image = image;
        Scenario = scenario;
        VolumeName = volumeName;
        Disks = disks;
        Game = game;
        _saved = characters;
        Characters = _saved.Select(c => c.Clone()).ToArray();
        EmptyTemplate = _saved.FirstOrDefault(c => c.IsEmpty)?.Clone() ?? Character.CreateBlank();
        _savedStock = Enumerable.Range(0, scenario.Items.Count).Select(scenario.ReadBoltacStock).ToArray();
        BoltacStock = (int[])_savedStock.Clone();
    }

    public string FilePath { get; private set; }
    public IBlockImage Image { get; }
    public ScenarioFile Scenario { get; }
    public string VolumeName { get; }
    public Character[] Characters { get; }

    /// <summary>ボルタック商店の在庫数(アイテム番号ごと。−1 = 無限、0 = 店に並ばない)。編集用のコピー。</summary>
    public int[] BoltacStock { get; }

    /// <summary>モンスターの絵(読める形式のときだけ。今は PC-98・PC-8801 版)。</summary>
    public MonsterPictures? MonsterPictures { get; }

    /// <summary>機種とシナリオ。</summary>
    public GameInfo Game { get; }

    /// <summary>同じファイルの中で開けるディスクの一覧(D88 で複数入っているとき)。普通は空か 1 件。</summary>
    public IReadOnlyList<DiskChoice> Disks { get; }

    /// <summary>書き込み禁止のディスク。編集はできるが保存はできない。</summary>
    public bool IsReadOnly => Image.IsWriteProtected;

    /// <summary>空きスロットに書く内容。開いたイメージの空きスロットの内容を使う。</summary>
    public Character EmptyTemplate { get; }

    /// <summary>
    /// イメージを開く。1 つのファイルに複数のディスクがあるとき(D88)は、
    /// <paramref name="diskIndex"/> を指定しなければキャラクタの入っているディスクを選ぶ。
    /// </summary>
    public static RosterDocument Open(string path, int? diskIndex = null)
    {
        Exception? lastError = null;
        var found = new List<(ImageLoader.OpenResult Opened, ScenarioFile Scenario, Character[] Characters)>();
        foreach (var opened in ImageLoader.OpenCandidates(File.ReadAllBytes(path)))
        {
            if (found.Any(f => f.Opened.Image.DiskIndex == opened.Image.DiskIndex))
                continue; // そのディスクは読めた候補がもうある
            try
            {
                var scenario = new ScenarioFile(opened.Image, opened.ScenarioEntry, LoadMessages(opened));
                var characters = Enumerable.Range(0, scenario.CharacterCount).Select(scenario.ReadCharacter).ToArray();
                found.Add((opened, scenario, characters));
            }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException)
            {
                lastError = ex; // 古いディレクトリの写しなど。次の候補を試す
            }
        }
        if (found.Count == 0)
            throw new InvalidDataException(lastError?.Message ?? ImageLoader.NotFoundMessage, lastError);

        var chosen = diskIndex is int wanted
            ? found.FirstOrDefault(f => f.Opened.Image.DiskIndex == wanted)
            : found.FirstOrDefault(f => f.Characters.Any(c => !c.IsEmpty));
        if (chosen.Scenario == null)
        {
            if (diskIndex != null)
                throw new InvalidDataException($"ディスク {diskIndex + 1} は開けません。");
            chosen = found[0];
        }

        var disks = found
            .Where(f => f.Opened.Image.DiskLabel != null)
            .Select(f => new DiskChoice(f.Opened.Image.DiskIndex, f.Opened.Image.DiskLabel!,
                f.Characters.Any(c => !c.IsEmpty), f.Opened.Image.IsWriteProtected))
            .ToList();
        return new RosterDocument(path, chosen.Opened.Image, chosen.Scenario, chosen.Opened.Directory.VolumeName,
            chosen.Characters, disks, GameInfo.Identify(chosen.Opened.Directory, chosen.Scenario),
            MonsterPictures.Load(chosen.Opened.Directory, chosen.Opened.Image));
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

    public bool IsStockDirty => !BoltacStock.AsSpan().SequenceEqual(_savedStock);

    public bool IsAnyDirty => IsStockDirty || Enumerable.Range(0, Characters.Length).Any(IsDirty);

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
    /// <exception cref="InvalidOperationException">書き込み禁止のディスクのとき。</exception>
    public string? Save(string? path = null, bool backup = true)
    {
        if (IsReadOnly)
            throw new InvalidOperationException(
                $"{Image.DiskLabel ?? "このディスク"}は書き込み禁止(マスターディスク)のため保存できません。");
        path ??= FilePath;
        for (int i = 0; i < Characters.Length; i++)
            Scenario.WriteCharacter(i, Characters[i]);
        for (int i = 0; i < BoltacStock.Length; i++)
        {
            if (BoltacStock[i] != _savedStock[i])
                Scenario.WriteBoltacStock(i, BoltacStock[i]);
        }

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
        BoltacStock.CopyTo(_savedStock, 0);
        FilePath = path;
        return backupPath;
    }
}
