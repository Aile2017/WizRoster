using WizRoster.Core;
using WizRoster.Core.IO;
using WizRoster.Core.Model;

namespace WizRoster;

internal sealed class MainForm : Form
{
    private const string AppTitle = "WizRoster";
    private const string ImageFilter =
        "ディスクイメージ (*.fdi;*.d88;*.dsk;*.do;*.po;*.img;*.hdm)|*.fdi;*.d88;*.dsk;*.do;*.po;*.img;*.hdm|すべてのファイル (*.*)|*.*";
    private const string CharacterFilter =
        "キャラクタ 生データ (*.wchr)|*.wchr|キャラクタ JSON (*.json)|*.json";

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly CharacterEditor _editor = new();
    private readonly Panel _emptyPanel = new() { Dock = DockStyle.Fill };
    private readonly Label _emptyLabel = new() { AutoSize = true, Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 11f) };
    private readonly ToolStripStatusLabel _statusFormat = new();
    private readonly ToolStripStatusLabel _statusPath = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly List<ToolStripItem> _docCommands = [];
    private readonly ToolStripMenuItem _diskMenu = new("ディスクの切り替え(&D)") { Enabled = false };

    private RosterDocument? _doc;

    public MainForm(string? initialPath)
    {
        Text = AppTitle;
        Size = new Size(1360, 900);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Yu Gothic UI", 9f);
        AllowDrop = true;

        BuildEmptyPanel();
        _tabs.SelectedIndexChanged += (_, _) => ShowSelectedSlot();
        _editor.CharacterChanged += (_, _) => RefreshTitles();

        Controls.Add(_tabs);
        Controls.Add(BuildQuickBar());
        Controls.Add(BuildMenu());
        Controls.Add(new StatusStrip { Items = { _statusFormat, _statusPath } });

        DragEnter += (_, e) => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                OpenImage(files[0]);
        };
        FormClosing += (_, e) => e.Cancel = !ConfirmDiscard();

        UpdateCommandState();
        if (initialPath != null)
            Shown += (_, _) => OpenImage(initialPath);
    }

    private int CurrentSlot => _tabs.SelectedIndex;

    // ---- 画面の組み立て ----

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("ファイル(&F)");
        file.DropDownItems.Add(Item("開く(&O)...", Keys.Control | Keys.O, (_, _) => OpenImageDialog(), docCommand: false));
        file.DropDownItems.Add(Item("上書き保存(&S)", Keys.Control | Keys.S, (_, _) => Save(saveAs: false)));
        file.DropDownItems.Add(Item("名前を付けて保存(&A)...", Keys.None, (_, _) => Save(saveAs: true)));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(_diskMenu);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("終了(&X)", Keys.None, (_, _) => Close(), docCommand: false));

        var chr = new ToolStripMenuItem("キャラクタ(&C)");
        chr.DropDownItems.Add(Item("別のスロットへコピー(&C)...", Keys.None, (_, _) => CopyToSlot()));
        chr.DropDownItems.Add(Item("別のスロットと入れ替え(&W)...", Keys.None, (_, _) => SwapWithSlot()));
        chr.DropDownItems.Add(new ToolStripSeparator());
        chr.DropDownItems.Add(Item("書き出し(&E)...", Keys.Control | Keys.E, (_, _) => ExportCharacter()));
        chr.DropDownItems.Add(Item("読み込み(&I)...", Keys.Control | Keys.I, (_, _) => ImportCharacter()));
        chr.DropDownItems.Add(new ToolStripSeparator());
        chr.DropDownItems.Add(Item("新規作成(&N)", Keys.None, (_, _) => CreateCharacter()));
        chr.DropDownItems.Add(Item("削除して空きにする(&D)", Keys.None, (_, _) => DeleteCharacter()));
        chr.DropDownItems.Add(Item("このスロットの変更を取り消す(&R)", Keys.None, (_, _) => RevertSlot()));

        var tools = new ToolStripMenuItem("便利(&T)");
        tools.DropDownItems.Add(Item("魔術師の呪文をすべて覚える", Keys.None, (_, _) => _editor.LearnAllSpells(SpellSchool.Mage)));
        tools.DropDownItems.Add(Item("僧侶の呪文をすべて覚える", Keys.None, (_, _) => _editor.LearnAllSpells(SpellSchool.Priest)));
        tools.DropDownItems.Add(Item("呪文をすべて忘れる", Keys.None, (_, _) => _editor.ForgetAllSpells()));
        tools.DropDownItems.Add(new ToolStripSeparator());
        tools.DropDownItems.Add(Item("HP を全回復", Keys.None, (_, _) => _editor.HealFull()));
        tools.DropDownItems.Add(Item("状態を正常にする", Keys.None, (_, _) => _editor.CureStatus()));
        tools.DropDownItems.Add(Item("持ち物をすべて鑑定し、呪いを解く", Keys.None, (_, _) => _editor.IdentifyAndUncurseItems()));

        menu.Items.AddRange(file, chr, tools);
        MainMenuStrip = menu;
        return menu;
    }

    private ToolStrip BuildQuickBar()
    {
        var bar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        bar.Items.Add(Button("開く", (_, _) => OpenImageDialog(), docCommand: false));
        bar.Items.Add(Button("上書き保存", (_, _) => Save(saveAs: false)));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(Button("魔術師の呪文をすべて", (_, _) => _editor.LearnAllSpells(SpellSchool.Mage)));
        bar.Items.Add(Button("僧侶の呪文をすべて", (_, _) => _editor.LearnAllSpells(SpellSchool.Priest)));
        bar.Items.Add(Button("HP 全回復", (_, _) => _editor.HealFull()));
        bar.Items.Add(Button("状態を正常に", (_, _) => _editor.CureStatus()));
        bar.Items.Add(Button("鑑定・呪い解除", (_, _) => _editor.IdentifyAndUncurseItems()));
        bar.Items.Add(new ToolStripSeparator());
        bar.Items.Add(Button("コピー...", (_, _) => CopyToSlot()));
        bar.Items.Add(Button("入れ替え...", (_, _) => SwapWithSlot()));
        bar.Items.Add(Button("書き出し...", (_, _) => ExportCharacter()));
        bar.Items.Add(Button("読み込み...", (_, _) => ImportCharacter()));
        return bar;
    }

    private ToolStripMenuItem Item(string text, Keys keys, EventHandler onClick, bool docCommand = true)
    {
        var item = new ToolStripMenuItem(text, null, onClick) { ShortcutKeys = keys };
        if (docCommand)
            _docCommands.Add(item);
        return item;
    }

    private ToolStripButton Button(string text, EventHandler onClick, bool docCommand = true)
    {
        var b = new ToolStripButton(text, null, onClick) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        if (docCommand)
            _docCommands.Add(b);
        return b;
    }

    private void BuildEmptyPanel()
    {
        var create = new System.Windows.Forms.Button { Text = "このスロットにキャラクタを作る", AutoSize = true };
        var import = new System.Windows.Forms.Button { Text = "ファイルから読み込む...", AutoSize = true };
        create.Click += (_, _) => CreateCharacter();
        import.Click += (_, _) => ImportCharacter();

        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Location = new Point(40, 40),
        };
        flow.Controls.Add(_emptyLabel);
        flow.Controls.Add(create);
        flow.Controls.Add(import);
        _emptyPanel.Controls.Add(flow);
    }

    // ---- ファイル ----

    private void OpenImageDialog()
    {
        using var dlg = new OpenFileDialog { Filter = ImageFilter, Title = "ディスクイメージを開く" };
        if (_doc != null)
            dlg.InitialDirectory = Path.GetDirectoryName(_doc.FilePath);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            OpenImage(dlg.FileName);
    }

    private void OpenImage(string path, int? diskIndex = null)
    {
        if (!ConfirmDiscard())
            return;
        RosterDocument doc;
        try
        {
            doc = RosterDocument.Open(path, diskIndex);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"開けませんでした。\n{path}\n\n{ex.Message}", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _doc = doc;
        _editor.SetScenario(doc.Scenario);

        _tabs.SuspendLayout();
        _tabs.TabPages.Clear();
        for (int i = 0; i < doc.Characters.Length; i++)
            _tabs.TabPages.Add(new TabPage());
        _tabs.ResumeLayout();

        _statusFormat.Text = $"{doc.Image.FormatName}{(doc.IsReadOnly ? " [書き込み禁止]" : "")} | ボリューム {doc.VolumeName} | " +
                             $"{doc.Scenario.Title} | アイテム名: {doc.Scenario.ItemNameSource} | 呪文名: {doc.Scenario.SpellNameSource}";
        _statusPath.Text = doc.FilePath;
        BuildDiskMenu(doc);
        RefreshTitles();
        UpdateCommandState();
        _tabs.SelectedIndex = 0;
        ShowSelectedSlot();
    }

    /// <summary>1 つのファイルに複数のディスクがあるとき(D88)、切り替えのメニューを作る。</summary>
    private void BuildDiskMenu(RosterDocument doc)
    {
        _diskMenu.DropDownItems.Clear();
        foreach (var disk in doc.Disks)
        {
            string text = disk.Label
                          + (disk.HasCharacters ? "(キャラクタあり)" : "(キャラクタなし)")
                          + (disk.IsWriteProtected ? " [書き込み禁止]" : "");
            var item = new ToolStripMenuItem(text) { Checked = disk.Index == doc.Image.DiskIndex };
            int index = disk.Index;
            item.Click += (_, _) =>
            {
                if (_doc != null && index != _doc.Image.DiskIndex)
                    OpenImage(_doc.FilePath, index);
            };
            _diskMenu.DropDownItems.Add(item);
        }
        _diskMenu.Enabled = doc.Disks.Count > 1;
    }

    private void Save(bool saveAs)
    {
        if (_doc == null)
            return;
        if (_doc.IsReadOnly)
        {
            MessageBox.Show(this,
                $"{_doc.Image.DiskLabel ?? "このディスク"}は書き込み禁止(マスターディスク)のため保存できません。\n" +
                "キャラクタを残したいときは「書き出し」でファイルに保存できます。",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Validate(); // 編集中の欄の値を確定させる
        string path = _doc.FilePath;
        if (saveAs)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = ImageFilter,
                FileName = Path.GetFileName(path),
                InitialDirectory = Path.GetDirectoryName(path),
                Title = "名前を付けて保存",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            path = dlg.FileName;
        }

        try
        {
            string? backup = _doc.Save(path);
            _statusPath.Text = _doc.FilePath;
            RefreshTitles();
            string msg = backup != null ? $"保存しました。元のファイルは次の名前で残しました。\n{backup}" : "保存しました。";
            MessageBox.Show(this, msg, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this,
                $"保存できませんでした。エミュレータがイメージを開いていないか確かめてください。\n\n{ex.Message}",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>未保存の変更があれば確認する。続けてよければ true。</summary>
    private bool ConfirmDiscard()
    {
        if (_doc == null || !_doc.IsAnyDirty)
            return true;
        if (_doc.IsReadOnly)
        {
            return MessageBox.Show(this, "書き込み禁止のディスクなので、変更は保存できません。変更を破棄してよろしいですか?",
                AppTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }
        var r = MessageBox.Show(this, "保存していない変更があります。保存しますか?", AppTitle,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (r == DialogResult.Cancel)
            return false;
        if (r == DialogResult.Yes)
        {
            Save(saveAs: false);
            return !_doc.IsAnyDirty;
        }
        return true;
    }

    // ---- スロットの表示 ----

    private void ShowSelectedSlot()
    {
        if (_doc == null || CurrentSlot < 0)
            return;
        var page = _tabs.TabPages[CurrentSlot];
        var c = _doc.Characters[CurrentSlot];
        if (c.IsEmpty)
        {
            _emptyLabel.Text = $"スロット {CurrentSlot} は空いています。";
            if (_emptyPanel.Parent != page)
                page.Controls.Add(_emptyPanel);
            _editor.Parent?.Controls.Remove(_editor);
            _editor.Unbind();
        }
        else
        {
            _emptyPanel.Parent?.Controls.Remove(_emptyPanel);
            if (_editor.Parent != page)
                page.Controls.Add(_editor);
            _editor.Bind(c);
        }
        UpdateCommandState();
    }

    private void RefreshTitles()
    {
        if (_doc == null)
            return;
        for (int i = 0; i < _doc.Characters.Length && i < _tabs.TabPages.Count; i++)
        {
            var c = _doc.Characters[i];
            string name = c.IsEmpty ? "(空き)" : c.Name;
            string text = $"{(_doc.IsDirty(i) ? "*" : "")}{i}: {name}";
            if (_tabs.TabPages[i].Text != text)
                _tabs.TabPages[i].Text = text;
        }
        string disk = _doc.Image.DiskLabel != null ? $" {_doc.Image.DiskLabel}" : "";
        string ro = _doc.IsReadOnly ? " [書き込み禁止]" : "";
        Text = $"{(_doc.IsAnyDirty ? "*" : "")}{Path.GetFileName(_doc.FilePath)}{disk}{ro} - {AppTitle}";
    }

    private void UpdateCommandState()
    {
        foreach (var item in _docCommands)
            item.Enabled = _doc != null;
    }

    /// <summary>外からキャラクタを書き換えたあとに画面を合わせる。</summary>
    private void AfterSlotChange()
    {
        ShowSelectedSlot();
        RefreshTitles();
    }

    private IEnumerable<(int, string)> OtherSlots() =>
        Enumerable.Range(0, _doc!.Characters.Length)
            .Where(i => i != CurrentSlot)
            .Select(i => (i, $"{i}: {(_doc.Characters[i].IsEmpty ? "(空き)" : _doc.Characters[i].Name)}"));

    // ---- キャラクタの操作 ----

    private bool RequireCharacter()
    {
        if (_doc != null && CurrentSlot >= 0 && !_doc.Characters[CurrentSlot].IsEmpty)
            return true;
        MessageBox.Show(this, "このスロットにはキャラクタがいません。", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void CopyToSlot()
    {
        if (!RequireCharacter())
            return;
        using var dlg = new SlotPickerDialog("別のスロットへコピー", $"スロット {CurrentSlot} のキャラクタをどこへコピーしますか?", OtherSlots());
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        int to = dlg.SelectedSlot;
        if (!_doc!.Characters[to].IsEmpty &&
            MessageBox.Show(this, $"スロット {to}({_doc.Characters[to].Name})を上書きします。よろしいですか?", AppTitle,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }
        _doc.CopySlot(CurrentSlot, to);
        RefreshTitles();
    }

    private void SwapWithSlot()
    {
        if (_doc == null)
            return;
        using var dlg = new SlotPickerDialog("別のスロットと入れ替え", $"スロット {CurrentSlot} とどのスロットを入れ替えますか?", OtherSlots());
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        _doc.SwapSlots(CurrentSlot, dlg.SelectedSlot);
        AfterSlotChange();
    }

    private void ExportCharacter()
    {
        if (!RequireCharacter())
            return;
        Validate();
        var c = _doc!.Characters[CurrentSlot];
        using var dlg = new SaveFileDialog
        {
            Filter = CharacterFilter,
            FileName = SafeFileName(c.Name),
            Title = "キャラクタの書き出し",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        try
        {
            CharacterFile.Save(dlg.FileName, c, _doc.Scenario);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportCharacter()
    {
        if (_doc == null)
            return;
        using var dlg = new OpenFileDialog
        {
            Filter = "キャラクタ (*.wchr;*.json)|*.wchr;*.json|" + CharacterFilter,
            Title = "キャラクタの読み込み",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        Character loaded;
        try
        {
            loaded = CharacterFile.Load(dlg.FileName);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException
                                       or FormatException or ArgumentException)
        {
            MessageBox.Show(this, $"読み込めませんでした。\n\n{ex.Message}", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var current = _doc.Characters[CurrentSlot];
        if (!current.IsEmpty &&
            MessageBox.Show(this, $"スロット {CurrentSlot}({current.Name})を読み込んだキャラクタで上書きします。よろしいですか?", AppTitle,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }
        _doc.Replace(CurrentSlot, loaded);
        AfterSlotChange();
    }

    private void CreateCharacter()
    {
        if (_doc == null)
            return;
        var current = _doc.Characters[CurrentSlot];
        if (!current.IsEmpty &&
            MessageBox.Show(this, $"スロット {CurrentSlot}({current.Name})を新しいキャラクタで上書きします。よろしいですか?", AppTitle,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }
        _doc.Replace(CurrentSlot, Character.CreateNew($"NEW{CurrentSlot}"));
        AfterSlotChange();
    }

    private void DeleteCharacter()
    {
        if (!RequireCharacter())
            return;
        if (MessageBox.Show(this, $"スロット {CurrentSlot}({_doc!.Characters[CurrentSlot].Name})を削除して空きにします。よろしいですか?\n(保存するまではイメージは変わりません)",
                AppTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }
        _doc.ClearSlot(CurrentSlot);
        AfterSlotChange();
    }

    private void RevertSlot()
    {
        if (_doc == null || !_doc.IsDirty(CurrentSlot))
            return;
        _doc.Revert(CurrentSlot);
        AfterSlotChange();
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string s = new(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return s.Length == 0 ? "character" : s;
    }
}
