using System.Text;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;

namespace WizRoster;

/// <summary>
/// 持ち物 8 件の編集。グリッドで一覧し、選んだ行を上下に動かせる。
/// 下の詳細欄は、行を選ぶまでは空で、選んだ行のアイテムの情報を出す。
/// 「(なし)」にした行は、後ろの持ち物を前に詰める(ゲームは先頭から「持ち物の数」だけを読むため)。
/// </summary>
internal sealed class ItemListEditor : UserControl
{
    private const int ColNumber = 0;
    private const int ColItem = 1;
    private const int ColEquipped = 2;
    private const int ColCursed = 3;
    private const int ColIdentified = 4;
    private const int NoItem = -1;

    private readonly DataGridView _grid = new()
    {
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AllowUserToResizeColumns = false,
        AllowUserToOrderColumns = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        EditMode = DataGridViewEditMode.EditOnEnter,
        ScrollBars = ScrollBars.None,
        BackgroundColor = SystemColors.Window,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        Margin = new Padding(3),
    };
    private readonly Button _up = new() { Text = "▲ 上へ", AutoSize = true, Enabled = false };
    private readonly Button _down = new() { Text = "▼ 下へ", AutoSize = true, Enabled = false };
    private readonly TextBox _detail = new()
    {
        Multiline = true,
        ReadOnly = true,
        Height = 96,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
    };

    private ScenarioFile? _scenario;
    private Character? _character;
    private List<ValueItem> _choices = [new ValueItem(NoItem, "(なし)")];
    private bool _loading;

    public ItemListEditor()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BuildGrid();

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        buttons.Controls.Add(_up);
        buttons.Controls.Add(_down);

        var layout = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(_grid, 0, 0);
        layout.Controls.Add(buttons, 1, 0);
        layout.Controls.Add(_detail, 0, 1);
        layout.SetColumnSpan(_detail, 2);
        Controls.Add(layout);

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            // コンボボックスとチェックボックスは、選んだ時点で確定させる
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (_loading || e.RowIndex < 0)
                return;
            int row = e.RowIndex;
            // 編集中のセルの中から一覧を作り直すと再入になるので、処理が戻ってから反映する
            BeginInvoke(() => ApplyFromGrid(row));
        };
        _grid.SelectionChanged += (_, _) =>
        {
            if (!_loading)
                ShowSelected();
        };
        _grid.DataError += (_, e) => e.ThrowException = false;
        _up.Click += (_, _) => MoveSelected(-1);
        _down.Click += (_, _) => MoveSelected(+1);
    }

    /// <summary>持ち物を変えたとき(並べ替えを含む)。</summary>
    public event EventHandler? ItemsChanged;

    /// <summary>アイテムの一覧を切り替える(イメージを開いたとき)。</summary>
    public void SetScenario(ScenarioFile scenario)
    {
        _scenario = scenario;
        _choices = [new ValueItem(NoItem, "(なし)"), .. scenario.Items.Select(i => new ValueItem(i.Index, $"{i.Index:D3} {i.Name}"))];
    }

    /// <summary>キャラクタを表示する。行の選択は外し、詳細欄は空にする。</summary>
    public void Bind(Character? character)
    {
        _character = character;
        Reload(selectRow: null);
    }

    private void BuildGrid()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "#",
            Width = 30,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
        });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            HeaderText = "アイテム",
            Width = 250,
            DisplayMember = nameof(ValueItem.Text),
            ValueMember = nameof(ValueItem.Value),
            ValueType = typeof(int),
            MaxDropDownItems = 25,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat,
        });
        foreach (string header in new[] { "装備", "呪い", "鑑定" })
        {
            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = header,
                Width = 46,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }
        for (int i = 0; i < Character.MaxItems; i++)
            _grid.Rows.Add(i + 1, NoItem, false, false, false);

        int height = _grid.ColumnHeadersHeight + _grid.Rows.GetRowsHeight(DataGridViewElementStates.None) + 2;
        int width = _grid.Columns.GetColumnsWidth(DataGridViewElementStates.None) + 2;
        _grid.Size = new Size(width, height);
        _detail.MinimumSize = new Size(width, 0);
    }

    /// <summary>キャラクタの持ち物をグリッドに読み直す。<paramref name="selectRow"/> の行を選ぶ(null なら選択なし)。</summary>
    private void Reload(int? selectRow)
    {
        _loading = true;
        try
        {
            if (_grid.IsCurrentCellInEditMode)
                _grid.EndEdit();
            var items = _character?.GetItems() ?? [];
            for (int i = 0; i < Character.MaxItems; i++)
            {
                var row = _grid.Rows[i];
                bool present = i < items.Count;
                int number = present ? items[i].ItemIndex : NoItem;

                var cell = (DataGridViewComboBoxCell)row.Cells[ColItem];
                cell.Items.Clear();
                cell.Items.AddRange(_choices.ToArray());
                if (present && !_choices.Any(c => c.Value == number))
                    cell.Items.Add(new ValueItem(number, ItemLabel(number), IsUnknown: true));
                cell.Value = number;

                row.Cells[ColEquipped].Value = present && items[i].Equipped;
                row.Cells[ColCursed].Value = present && items[i].Cursed;
                row.Cells[ColIdentified].Value = present && items[i].Identified;
                foreach (int col in new[] { ColEquipped, ColCursed, ColIdentified })
                {
                    row.Cells[col].ReadOnly = !present;
                    row.Cells[col].Style.BackColor = present ? SystemColors.Window : SystemColors.Control;
                }
            }

            if (selectRow is int r && r >= 0 && r < items.Count)
            {
                _grid.CurrentCell = _grid.Rows[r].Cells[ColItem];
                _grid.Rows[r].Selected = true;
            }
            else
            {
                _grid.CurrentCell = null;
                _grid.ClearSelection();
            }
        }
        finally
        {
            _loading = false;
        }
        ShowSelected();
    }

    /// <summary>グリッドの内容をキャラクタに書き込む。「(なし)」の行は飛ばして前に詰める。</summary>
    private void ApplyFromGrid(int editedRow)
    {
        if (_character == null)
            return;
        var items = new List<Possession>();
        int newIndexOfEdited = -1;
        for (int i = 0; i < Character.MaxItems; i++)
        {
            var row = _grid.Rows[i];
            if (row.Cells[ColItem].Value is not int number || number == NoItem)
                continue;
            if (i == editedRow)
                newIndexOfEdited = items.Count;
            items.Add(new Possession(IsChecked(row, ColEquipped), IsChecked(row, ColCursed), IsChecked(row, ColIdentified), number));
        }
        _character.SetItems(items);
        Reload(newIndexOfEdited >= 0 ? newIndexOfEdited : null);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>選んでいる持ち物を上下に動かす。</summary>
    private void MoveSelected(int delta)
    {
        if (_character == null || SelectedRow() is not int r)
            return;
        var items = _character.GetItems().ToList();
        int t = r + delta;
        if (r >= items.Count || t < 0 || t >= items.Count)
            return;
        (items[r], items[t]) = (items[t], items[r]);
        _character.SetItems(items);
        Reload(t);
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private int? SelectedRow() =>
        _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Index : null;

    private static bool IsChecked(DataGridViewRow row, int col) => row.Cells[col].Value is true;

    private void ShowSelected()
    {
        int count = _character?.GetItems().Count ?? 0;
        int? r = SelectedRow();
        _up.Enabled = r is int u && u > 0 && u < count;
        _down.Enabled = r is int d && d < count - 1;
        _detail.Text = r is int row ? DetailText(row) : "";
    }

    /// <summary>アイテム表にない番号の表示名。1000 が足された番号なら元のアイテム名を出す。</summary>
    private string ItemLabel(int number)
    {
        if (_scenario != null && _scenario.IsFlaggedItemNumber(number))
        {
            var item = _scenario.ResolveItem(number)!;
            return $"{item.Index:D3} {item.Name} (+1000)";
        }
        return $"{number}: (不明な値)";
    }

    private string DetailText(int row)
    {
        if (_scenario == null || _grid.Rows[row].Cells[ColItem].Value is not int number || number == NoItem)
            return $"持ち物 {row + 1}: (なし)";
        var item = _scenario.ResolveItem(number);
        if (item == null)
            return $"持ち物 {row + 1}: アイテム表にない番号です。";

        var sb = new StringBuilder();
        sb.AppendLine($"持ち物 {row + 1}: {item.Name}(未鑑定名 {item.UnknownName})");
        sb.AppendLine($"種類 {GameNames.Lookup(GameNames.ItemTypes, item.Type)}  属性 {(item.Alignment == 0 ? "なし" : _scenario.Names.Alignment(item.Alignment))}  " +
                      $"呪い {(item.Cursed ? "あり" : "なし")}  価格 {item.Price:N0}");
        sb.AppendLine($"AC {item.ArmorMod:+0;-0;0}  命中 {item.HitMod:+0;-0;0}  ダメージ {item.DamageDice}d{item.DamageSides}{item.DamageBonus:+0;-0;+0}  " +
                      $"追加攻撃 {item.ExtraSwings}  クリティカル {(item.CriticalHit ? "あり" : "なし")}");
        string spell = item.SpellPower > 0 ? _scenario.FindSpell(item.SpellPower)?.Name ?? $"#{item.SpellPower}" : "-";
        sb.AppendLine($"装備できる職業 {item.ClassUseText(_scenario.Names.Classes)}");
        sb.Append($"呪文 {spell}  HP 回復 {item.HealPoints}  変化先 {item.ChangeTo}({item.ChangeChance}%)  特殊 {item.Special}");
        if (item.NameNote != null)
            sb.AppendLine().Append(item.NameNote);
        if (_scenario.IsFlaggedItemNumber(number))
            sb.AppendLine().Append($"番号は {number}(アイテム {item.Index} に 1000 を足した形)。足す意味は未確認なので、そのまま残します。");
        return sb.ToString();
    }
}
