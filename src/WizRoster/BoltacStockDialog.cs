using WizRoster.Core;
using WizRoster.Core.Model;

namespace WizRoster;

/// <summary>
/// ボルタック商店の在庫数の編集。OK で文書の在庫に反映する(イメージへの書き込みは保存のとき)。
/// ゲームは在庫が 0 以外で呪われていない品を店に並べ、買うと 1 減らし、売ると 1 増やす(−1 = 無限はそのまま)。
/// </summary>
internal sealed class BoltacStockDialog : Form
{
    private const int ColStock = 5;
    private const int ColShown = 6;

    private readonly RosterDocument _doc;
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.CellSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };

    public BoltacStockDialog(RosterDocument doc)
    {
        _doc = doc;
        Text = "ボルタック商店の在庫";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Size = new Size(760, 640);
        MinimumSize = new Size(560, 400);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "番号", ReadOnly = true, FillWeight = 8 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名前", ReadOnly = true, FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "種類", ReadOnly = true, FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "価格", ReadOnly = true, FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "呪い", ReadOnly = true, FillWeight = 7 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "在庫", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "店に並ぶ", ReadOnly = true, FillWeight = 12 });
        _grid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns[ColStock].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns[ColStock].DefaultCellStyle.BackColor = SystemColors.Info;

        foreach (var item in doc.Scenario.Items)
        {
            int row = _grid.Rows.Add(
                item.Index.ToString("D3"),
                item.Name,
                GameNames.Lookup(GameNames.ItemTypes, item.Type),
                item.Price.ToString("N0"),
                item.Cursed ? "呪い" : "",
                doc.BoltacStock[item.Index].ToString(),
                "");
            UpdateShown(row);
        }
        _grid.CellValidating += OnCellValidating;
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == ColStock)
                UpdateShown(e.RowIndex);
        };

        var note = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            Text = "在庫: −1 = 無限、0 = 店に並ばない。ゲームは買うと 1 減らし、売ると 1 増やす(無限はそのまま)。\n" +
                   "呪われた品は在庫があっても店に並ばない。変更は「上書き保存」でディスクに書き込みます。",
        };

        var ok = new Button { Text = "OK", AutoSize = true };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, _) =>
        {
            if (!_grid.EndEdit())
                return; // 入力が正しくない欄がある
            for (int i = 0; i < _grid.Rows.Count; i++)
                _doc.BoltacStock[i] = ParseStock(_grid.Rows[i].Cells[ColStock].Value) ?? _doc.BoltacStock[i];
            DialogResult = DialogResult.OK;
        };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_grid, 0, 0);
        layout.Controls.Add(note, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
    }

    private static int? ParseStock(object? value) =>
        int.TryParse(value?.ToString()?.Trim().Replace('−', '-'), out int v) && v is >= -1 and <= short.MaxValue ? v : null;

    private void OnCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.ColumnIndex != ColStock)
            return;
        bool ok = ParseStock(e.FormattedValue) != null;
        _grid.Rows[e.RowIndex].ErrorText = ok ? "" : $"−1〜{short.MaxValue} の数値を入れてください(−1 = 無限)。";
        e.Cancel = !ok;
    }

    private void UpdateShown(int row)
    {
        var item = _doc.Scenario.Items[row];
        int? stock = ParseStock(_grid.Rows[row].Cells[ColStock].Value);
        string shown = stock switch
        {
            null => "",
            0 => "",
            _ when item.Cursed => "×(呪い)",
            -1 => "○(無限)",
            _ => "○",
        };
        _grid.Rows[row].Cells[ColShown].Value = shown;
    }
}
