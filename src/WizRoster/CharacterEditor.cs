using System.Text;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;
using static WizRoster.UiHelpers;

namespace WizRoster;

/// <summary>
/// キャラクタ 1 人分の編集画面。20 個のタブで 1 つを使い回す。
/// 入力はすぐに <see cref="Character"/> の生データへ書き込む。
/// </summary>
internal sealed class CharacterEditor : UserControl
{
    private ScenarioFile? _scenario;
    private Character? _character;
    private bool _loading;

    // 基本
    private readonly TextBox _name = new() { MaxLength = 15, Width = 160 };
    private readonly TextBox _password = new() { MaxLength = 15, Width = 160 };
    private readonly ComboBox _race = EnumCombo(GameNames.Races);
    private readonly ComboBox _class = EnumCombo(GameNames.Classes);
    private readonly ComboBox _align = EnumCombo(GameNames.Alignments);
    private readonly ComboBox _status = EnumCombo(GameNames.Statuses);
    private readonly NumericUpDown _age = Num(0, short.MaxValue);
    private readonly Label _ageYears = Label("");
    private readonly CheckBox _inMaze = new() { Text = "迷宮内にいる", AutoSize = true };
    private readonly NumericUpDown _level = Num(0, short.MaxValue);
    private readonly NumericUpDown _maxLevel = Num(0, short.MaxValue);
    private readonly NumericUpDown _hp = Num(0, short.MaxValue);
    private readonly NumericUpDown _hpMax = Num(0, short.MaxValue);
    private readonly NumericUpDown _ac = Num(short.MinValue, short.MaxValue);
    private readonly NumericUpDown _gold = Num(0, WizBytes.WizLongMax, 140);
    private readonly NumericUpDown _exp = Num(0, WizBytes.WizLongMax, 140);

    private readonly NumericUpDown[] _attrs = Enumerable.Range(0, 6).Select(_ => Num(0, 31, 60)).ToArray();
    private readonly NumericUpDown[] _saves = Enumerable.Range(0, 5).Select(_ => Num(0, 31, 60)).ToArray();

    // 持ち物
    private readonly ComboBox[] _itemCombo = new ComboBox[Character.MaxItems];
    private readonly CheckBox[] _itemEquipped = new CheckBox[Character.MaxItems];
    private readonly CheckBox[] _itemCursed = new CheckBox[Character.MaxItems];
    private readonly CheckBox[] _itemIdentified = new CheckBox[Character.MaxItems];
    private readonly TextBox _itemDetail = new()
    {
        Multiline = true,
        ReadOnly = true,
        Height = 80,
        Dock = DockStyle.Top,
        ScrollBars = ScrollBars.Vertical,
    };
    private int _detailRow;

    // 呪文
    private readonly CheckedListBox _mageSpells = new() { CheckOnClick = true, IntegralHeight = false, Height = 220, Width = 180 };
    private readonly CheckedListBox _priestSpells = new() { CheckOnClick = true, IntegralHeight = false, Height = 220, Width = 180 };
    private readonly NumericUpDown[] _mageSp = Enumerable.Range(0, 7).Select(_ => Num(0, 9, 40)).ToArray();
    private readonly NumericUpDown[] _priestSp = Enumerable.Range(0, 7).Select(_ => Num(0, 9, 40)).ToArray();

    // その他
    private readonly DataGridView _rawGrid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.CellSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };
    private readonly TextBox _hex = new()
    {
        Multiline = true,
        ReadOnly = true,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9.5f),
        ScrollBars = ScrollBars.Vertical,
        WordWrap = false,
    };
    private readonly ListBox _warnings = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };

    public CharacterEditor()
    {
        Dock = DockStyle.Fill;
        BuildLayout();
        WireEvents();
        Enabled = false;
    }

    /// <summary>入力で内容が変わったとき。</summary>
    public event EventHandler? CharacterChanged;

    public Character? Character => _character;

    /// <summary>マスタデータを切り替える(イメージを開いたとき)。</summary>
    public void SetScenario(ScenarioFile scenario)
    {
        _scenario = scenario;
        _loading = true;
        try
        {
            foreach (var cb in _itemCombo)
            {
                cb.Items.Clear();
                cb.Items.Add(new ValueItem(-1, "(なし)"));
                foreach (var item in scenario.Items)
                    cb.Items.Add(new ValueItem(item.Index, $"{item.Index:D3} {item.Name}"));
            }
            FillSpellList(_mageSpells, scenario.MageSpells);
            FillSpellList(_priestSpells, scenario.PriestSpells);
        }
        finally
        {
            _loading = false;
        }
    }

    public void Bind(Character character)
    {
        _character = character;
        Enabled = true;
        LoadAll();
    }

    /// <summary>空きスロットを表示している間は、どのキャラクタも編集しない。</summary>
    public void Unbind()
    {
        _character = null;
        Enabled = false;
    }

    /// <summary>キャラクタの内容を画面に読み直す(外から書き換えたとき)。</summary>
    public void Reload() => LoadAll();

    // ---- 便利な一括操作 ----

    public void LearnAllSpells(SpellSchool school)
    {
        if (_character == null || _scenario == null)
            return;
        foreach (var s in school == SpellSchool.Mage ? _scenario.MageSpells : _scenario.PriestSpells)
            _character.SetKnowsSpell(s.Bit, true);
        AfterBulkChange();
    }

    public void ForgetAllSpells()
    {
        if (_character == null)
            return;
        for (int bit = 0; bit < Character.SpellBitCount; bit++)
            _character.SetKnowsSpell(bit, false);
        AfterBulkChange();
    }

    public void HealFull()
    {
        if (_character == null)
            return;
        _character.Hp = _character.HpMax;
        AfterBulkChange();
    }

    public void CureStatus()
    {
        if (_character == null)
            return;
        _character.Status = 0;
        if (_character.Hp <= 0)
            _character.Hp = _character.HpMax;
        AfterBulkChange();
    }

    public void IdentifyAndUncurseItems()
    {
        if (_character == null)
            return;
        _character.SetItems(_character.GetItems().Select(p => p with { Cursed = false, Identified = true }).ToList());
        AfterBulkChange();
    }

    private void AfterBulkChange()
    {
        LoadAll();
        CharacterChanged?.Invoke(this, EventArgs.Empty);
    }

    // ---- 画面の組み立て ----

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            AutoScroll = true,
            AutoScrollMinSize = new Size(1250, 740), // これより狭いとスクロールする
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(Column(BuildBasic(), BuildAttributes()), 0, 0);
        root.Controls.Add(Column(BuildItems(), BuildSpells()), 1, 0);
        root.Controls.Add(BuildRight(), 2, 0);
        Controls.Add(root);
    }

    private static Control Column(params Control[] groups)
    {
        var col = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
        };
        foreach (var g in groups)
        {
            col.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            col.Controls.Add(g);
        }
        return col;
    }

    private Control BuildBasic()
    {
        var t = FormTable();
        AddRow(t, "名前", _name);
        AddRow(t, "パスワード", _password);
        AddRow(t, "種族", _race);
        AddRow(t, "職業", _class);
        AddRow(t, "属性", _align);
        AddRow(t, "状態", _status);
        AddRow(t, "年齢(週)", _age, _ageYears);
        AddRow(t, "", _inMaze);
        AddRow(t, "レベル", _level, Label("最高"), _maxLevel);
        AddRow(t, "HP", _hp, Label("/"), _hpMax);
        AddRow(t, "AC", _ac);
        AddRow(t, "所持金", _gold);
        AddRow(t, "経験値", _exp);
        return Group("基本", t);
    }

    private Control BuildAttributes()
    {
        var t = FormTable(4);
        for (int i = 0; i < 6; i += 2)
        {
            int row = t.RowCount++;
            t.Controls.Add(Label(GameNames.Attributes[i]), 0, row);
            t.Controls.Add(_attrs[i], 1, row);
            t.Controls.Add(Label(GameNames.Attributes[i + 1]), 2, row);
            t.Controls.Add(_attrs[i + 1], 3, row);
        }
        var attrGroup = Group("能力値(0〜31。ゲームの上限は 18)", t);

        var s = FormTable(4);
        for (int i = 0; i < 5; i++)
        {
            int row = i / 2;
            if (row >= s.RowCount)
                s.RowCount = row + 1;
            s.Controls.Add(Label(GameNames.SavingThrows[i]), i % 2 * 2, row);
            s.Controls.Add(_saves[i], i % 2 * 2 + 1, row);
        }
        var saveGroup = Group("セービングスロー(0〜31。項目の並びは推測)", s);
        return Column(attrGroup, saveGroup);
    }

    private Control BuildItems()
    {
        var t = new TableLayoutPanel { ColumnCount = 5, AutoSize = true, Dock = DockStyle.Top };
        string[] headers = ["#", "アイテム", "装備", "呪い", "鑑定"];
        for (int c = 0; c < headers.Length; c++)
            t.Controls.Add(Label(headers[c]), c, 0);
        for (int i = 0; i < Character.MaxItems; i++)
        {
            _itemCombo[i] = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 230,
                MaxDropDownItems = 25,
                Tag = i,
            };
            _itemEquipped[i] = new CheckBox { AutoSize = true, Tag = i, Anchor = AnchorStyles.None };
            _itemCursed[i] = new CheckBox { AutoSize = true, Tag = i, Anchor = AnchorStyles.None };
            _itemIdentified[i] = new CheckBox { AutoSize = true, Tag = i, Anchor = AnchorStyles.None };
            t.Controls.Add(Label((i + 1).ToString()), 0, i + 1);
            t.Controls.Add(_itemCombo[i], 1, i + 1);
            t.Controls.Add(_itemEquipped[i], 2, i + 1);
            t.Controls.Add(_itemCursed[i], 3, i + 1);
            t.Controls.Add(_itemIdentified[i], 4, i + 1);
        }

        var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Top };
        panel.Controls.Add(t);
        panel.Controls.Add(_itemDetail);
        _itemDetail.Width = 380;
        return Group("持ち物(空けた欄は前に詰めます)", panel);
    }

    private Control BuildSpells()
    {
        var t = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top };
        t.Controls.Add(Label("魔術師"), 0, 0);
        t.Controls.Add(Label("僧侶"), 1, 0);
        t.Controls.Add(_mageSpells, 0, 1);
        t.Controls.Add(_priestSpells, 1, 1);

        var sp = new TableLayoutPanel { ColumnCount = 8, AutoSize = true, Dock = DockStyle.Top };
        sp.Controls.Add(Label("回数"), 0, 0);
        for (int lv = 0; lv < 7; lv++)
            sp.Controls.Add(Label($"Lv{lv + 1}"), lv + 1, 0);
        sp.Controls.Add(Label("魔術師"), 0, 1);
        sp.Controls.Add(Label("僧侶"), 0, 2);
        for (int lv = 0; lv < 7; lv++)
        {
            sp.Controls.Add(_mageSp[lv], lv + 1, 1);
            sp.Controls.Add(_priestSp[lv], lv + 1, 2);
        }

        var note = Label("※ 回数はゲームが覚えている呪文とレベルから計算し直すことがあります。");
        note.ForeColor = SystemColors.GrayText;

        var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Top };
        panel.Controls.Add(t);
        panel.Controls.Add(sp);
        panel.Controls.Add(note);
        return Group("呪文", panel);
    }

    private Control BuildRight()
    {
        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Offset", HeaderText = "位置", ReadOnly = true, FillWeight = 12 });
        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Field", HeaderText = "名前", ReadOnly = true, FillWeight = 25 });
        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "値", FillWeight = 15 });
        _rawGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desc", HeaderText = "説明", ReadOnly = true, FillWeight = 48 });
        foreach (var f in RawFields.Extra)
        {
            int row = _rawGrid.Rows.Add($"+{f.Offset}", f.Name, "", f.Description);
            _rawGrid.Rows[row].Tag = f;
        }

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var rawPage = new TabPage("その他(生の値)");
        rawPage.Controls.Add(_rawGrid);
        var hexPage = new TabPage("16 進(208 バイト)");
        hexPage.Controls.Add(_hex);
        tabs.TabPages.Add(rawPage);
        tabs.TabPages.Add(hexPage);

        var warnGroup = new GroupBox { Text = "警告(ゲームのルールに合わない点)", Dock = DockStyle.Fill };
        warnGroup.Controls.Add(_warnings);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            MinimumSize = new Size(300, 300),
        };
        split.Panel1.Controls.Add(tabs);
        split.Panel2.Controls.Add(warnGroup);
        split.HandleCreated += (_, _) => split.SplitterDistance = Math.Max(100, split.Height * 6 / 10);
        return split;
    }

    // ---- イベント ----

    private void WireEvents()
    {
        _name.TextChanged += (_, _) => Apply(c => c.Name = AsciiOnly(_name));
        _password.TextChanged += (_, _) => Apply(c => c.Password = AsciiOnly(_password));
        _race.SelectedIndexChanged += (_, _) => Apply(c => c.Race = SelectedValue(_race));
        _class.SelectedIndexChanged += (_, _) => Apply(c => c.Class = SelectedValue(_class));
        _align.SelectedIndexChanged += (_, _) => Apply(c => c.Alignment = SelectedValue(_align));
        _status.SelectedIndexChanged += (_, _) => Apply(c => c.Status = SelectedValue(_status));
        _age.ValueChanged += (_, _) => Apply(c => c.Age = (int)_age.Value);
        _inMaze.CheckedChanged += (_, _) => Apply(c => c.InMaze = _inMaze.Checked);
        _level.ValueChanged += (_, _) => Apply(c => c.Level = (int)_level.Value);
        _maxLevel.ValueChanged += (_, _) => Apply(c => c.MaxLevel = (int)_maxLevel.Value);
        _hp.ValueChanged += (_, _) => Apply(c => c.Hp = (int)_hp.Value);
        _hpMax.ValueChanged += (_, _) => Apply(c => c.HpMax = (int)_hpMax.Value);
        _ac.ValueChanged += (_, _) => Apply(c => c.ArmorClass = (int)_ac.Value);
        _gold.ValueChanged += (_, _) => Apply(c => c.Gold = (long)_gold.Value);
        _exp.ValueChanged += (_, _) => Apply(c => c.Experience = (long)_exp.Value);

        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            _attrs[i].ValueChanged += (_, _) => Apply(c => c.SetAttribute(idx, (int)_attrs[idx].Value));
        }
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            _saves[i].ValueChanged += (_, _) => Apply(c => c.SetSavingThrow(idx, (int)_saves[idx].Value));
        }
        for (int lv = 0; lv < 7; lv++)
        {
            int level = lv + 1;
            var m = _mageSp[lv];
            var p = _priestSp[lv];
            m.ValueChanged += (_, _) => Apply(c => c.SetMageSpellPoints(level, (int)m.Value));
            p.ValueChanged += (_, _) => Apply(c => c.SetPriestSpellPoints(level, (int)p.Value));
        }

        for (int i = 0; i < Character.MaxItems; i++)
        {
            int row = i;
            _itemCombo[i].SelectedIndexChanged += (_, _) =>
            {
                if (_loading)
                    return;
                ShowItemDetail(row);
                ApplyItems();
            };
            _itemCombo[i].Enter += (_, _) => ShowItemDetail(row);
            _itemEquipped[i].CheckedChanged += (_, _) => ApplyItems();
            _itemCursed[i].CheckedChanged += (_, _) => ApplyItems();
            _itemIdentified[i].CheckedChanged += (_, _) => ApplyItems();
        }

        _mageSpells.ItemCheck += OnSpellCheck;
        _priestSpells.ItemCheck += OnSpellCheck;

        _rawGrid.CellValidating += OnRawCellValidating;
        _rawGrid.CellValueChanged += OnRawCellValueChanged;
    }

    /// <summary>名前・パスワードは ASCII だけ。それ以外の文字は取り除く。</summary>
    private static string AsciiOnly(TextBox box)
    {
        string filtered = new(box.Text.Where(ch => ch is >= ' ' and <= '~').ToArray());
        if (filtered != box.Text)
        {
            int caret = Math.Max(0, box.SelectionStart - (box.Text.Length - filtered.Length));
            box.Text = filtered;
            box.SelectionStart = Math.Min(caret, filtered.Length);
        }
        return filtered;
    }

    private void Apply(Action<Character> change)
    {
        if (_loading || _character == null)
            return;
        change(_character);
        AfterEdit();
    }

    private void AfterEdit()
    {
        UpdateDerived();
        CharacterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyItems()
    {
        if (_loading || _character == null)
            return;

        // 「(なし)」の欄は飛ばして前に詰める
        var items = new List<Possession>();
        for (int i = 0; i < Character.MaxItems; i++)
        {
            if (_itemCombo[i].SelectedIndex <= 0)
                continue;
            items.Add(new Possession(_itemEquipped[i].Checked, _itemCursed[i].Checked, _itemIdentified[i].Checked,
                SelectedValue(_itemCombo[i])));
        }
        _character.SetItems(items);
        LoadItems();
        AfterEdit();
    }

    private void OnSpellCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_loading || _character == null || sender is not CheckedListBox list)
            return;
        if (list.Items[e.Index] is SpellDef spell)
        {
            _character.SetKnowsSpell(spell.Bit, e.NewValue == CheckState.Checked);
            AfterEdit();
        }
    }

    private void OnRawCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (_loading || e.ColumnIndex != 2 || _rawGrid.Rows[e.RowIndex].Tag is not RawField f)
            return;
        string text = e.FormattedValue?.ToString() ?? "";
        int min = f.Signed ? short.MinValue : 0;
        int max = f.Signed ? short.MaxValue : ushort.MaxValue;
        if (!TryParseNumber(text, out int v) || v < min || v > max)
        {
            _rawGrid.Rows[e.RowIndex].ErrorText = $"{min}〜{max} の数値を入れてください(16 進は 0x を付ける)。";
            e.Cancel = true;
        }
        else
        {
            _rawGrid.Rows[e.RowIndex].ErrorText = "";
        }
    }

    private void OnRawCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_loading || e.RowIndex < 0 || e.ColumnIndex != 2 || _rawGrid.Rows[e.RowIndex].Tag is not RawField f)
            return;
        if (TryParseNumber(_rawGrid.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? "", out int v))
            Apply(c => c.SetWord(f.Offset, v));
    }

    private static bool TryParseNumber(string text, out int value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(text[2..], System.Globalization.NumberStyles.HexNumber, null, out value);
        return int.TryParse(text, out value);
    }

    // ---- 画面への読み込み ----

    private void LoadAll()
    {
        var c = _character;
        if (c == null)
            return;
        _loading = true;
        try
        {
            _name.Text = c.Name;
            _password.Text = c.Password;
            SelectValue(_race, c.Race);
            SelectValue(_class, c.Class);
            SelectValue(_align, c.Alignment);
            SelectValue(_status, c.Status);
            SetNum(_age, c.Age);
            _inMaze.Checked = c.InMaze;
            SetNum(_level, c.Level);
            SetNum(_maxLevel, c.MaxLevel);
            SetNum(_hp, c.Hp);
            SetNum(_hpMax, c.HpMax);
            SetNum(_ac, c.ArmorClass);
            SetNum(_gold, c.Gold);
            SetNum(_exp, c.Experience);
            for (int i = 0; i < 6; i++)
                SetNum(_attrs[i], c.GetAttribute(i));
            for (int i = 0; i < 5; i++)
                SetNum(_saves[i], c.GetSavingThrow(i));
            for (int lv = 1; lv <= 7; lv++)
            {
                SetNum(_mageSp[lv - 1], c.GetMageSpellPoints(lv));
                SetNum(_priestSp[lv - 1], c.GetPriestSpellPoints(lv));
            }
            LoadSpellChecks(_mageSpells);
            LoadSpellChecks(_priestSpells);
            LoadItems();
        }
        finally
        {
            _loading = false;
        }
        UpdateDerived();
    }

    private void LoadItems()
    {
        if (_character == null)
            return;
        bool wasLoading = _loading;
        _loading = true;
        try
        {
            var items = _character.GetItems();
            for (int i = 0; i < Character.MaxItems; i++)
            {
                bool present = i < items.Count;
                if (present)
                    SelectValue(_itemCombo[i], items[i].ItemIndex, ItemLabel(items[i].ItemIndex));
                else
                    _itemCombo[i].SelectedIndex = _itemCombo[i].Items.Count > 0 ? 0 : -1;
                _itemEquipped[i].Checked = present && items[i].Equipped;
                _itemCursed[i].Checked = present && items[i].Cursed;
                _itemIdentified[i].Checked = present && items[i].Identified;
                _itemEquipped[i].Enabled = _itemCursed[i].Enabled = _itemIdentified[i].Enabled = present;
            }
        }
        finally
        {
            _loading = wasLoading;
        }
        ShowItemDetail(_detailRow);
    }

    /// <summary>アイテム表にない番号の表示名。1000 が足された番号なら元のアイテム名を出す。</summary>
    private string? ItemLabel(int number)
    {
        if (_scenario == null || !_scenario.IsFlaggedItemNumber(number))
            return null;
        var item = _scenario.ResolveItem(number)!;
        return $"{item.Index:D3} {item.Name} (+1000)";
    }

    private void LoadSpellChecks(CheckedListBox list)
    {
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is SpellDef s)
                list.SetItemChecked(i, _character!.KnowsSpell(s.Bit));
        }
    }

    private static void FillSpellList(CheckedListBox list, IEnumerable<SpellDef> spells)
    {
        list.Items.Clear();
        foreach (var s in spells)
            list.Items.Add(s);
    }

    /// <summary>入力欄以外の表示(年齢、生の値、16 進、警告)を更新する。</summary>
    private void UpdateDerived()
    {
        var c = _character;
        if (c == null)
            return;

        _ageYears.Text = $"({c.Age / 52} 歳 {c.Age % 52} 週)";

        bool wasLoading = _loading;
        _loading = true;
        try
        {
            foreach (DataGridViewRow row in _rawGrid.Rows)
            {
                if (row.Tag is RawField f && !row.Cells[2].IsInEditMode)
                    row.Cells[2].Value = (f.Signed ? c.SignedWord(f.Offset) : c.Word(f.Offset)).ToString();
            }
        }
        finally
        {
            _loading = wasLoading;
        }

        _hex.Text = HexDump(c.Raw);

        _warnings.BeginUpdate();
        _warnings.Items.Clear();
        if (_scenario != null)
        {
            foreach (string w in CharacterRules.Check(c, _scenario))
                _warnings.Items.Add(w);
        }
        if (_warnings.Items.Count == 0)
            _warnings.Items.Add("(なし)");
        _warnings.EndUpdate();
    }

    private void ShowItemDetail(int row)
    {
        _detailRow = row;
        if (_scenario == null || _itemCombo[row].SelectedIndex <= 0)
        {
            _itemDetail.Text = $"持ち物 {row + 1}: (なし)";
            return;
        }
        int number = SelectedValue(_itemCombo[row]);
        var item = _scenario.ResolveItem(number);
        if (item == null)
        {
            _itemDetail.Text = $"持ち物 {row + 1}: アイテム表にない番号です。";
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"持ち物 {row + 1}: {item.Name}(未鑑定名 {item.UnknownName})");
        sb.AppendLine($"種類 {GameNames.Lookup(GameNames.ItemTypes, item.Type)}  属性 {GameNames.Lookup(GameNames.Alignments, item.Alignment)}  " +
                      $"呪い {(item.Cursed ? "あり" : "なし")}  価格 {item.Price:N0}");
        sb.AppendLine($"AC {item.ArmorMod:+0;-0;0}  命中 {item.HitMod:+0;-0;0}  ダメージ {item.DamageDice}d{item.DamageSides}{item.DamageBonus:+0;-0;+0}  " +
                      $"追加攻撃 {item.ExtraSwings}  クリティカル {(item.CriticalHit ? "あり" : "なし")}");
        string spell = item.SpellPower > 0 ? _scenario.FindSpell(item.SpellPower)?.Name ?? $"#{item.SpellPower}" : "-";
        sb.AppendLine($"装備できる職業 {item.ClassUseText()}");
        sb.Append($"呪文 {spell}  HP 回復 {item.HealPoints}  変化先 {item.ChangeTo}({item.ChangeChance}%)  特殊 {item.Special}");
        if (item.NameNote != null)
            sb.AppendLine().Append(item.NameNote);
        if (_scenario.IsFlaggedItemNumber(number))
            sb.AppendLine().Append($"番号は {number}(アイテム {item.Index} に 1000 を足した形)。足す意味は未確認なので、そのまま残します。");
        _itemDetail.Text = sb.ToString();
    }

    private static string HexDump(byte[] data)
    {
        var sb = new StringBuilder();
        for (int off = 0; off < data.Length; off += 16)
        {
            int n = Math.Min(16, data.Length - off);
            sb.Append($"+{off,3}  ");
            for (int i = 0; i < 16; i++)
                sb.Append(i < n ? $"{data[off + i]:X2} " : "   ");
            sb.Append(' ');
            for (int i = 0; i < n; i++)
            {
                byte b = data[off + i];
                sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
