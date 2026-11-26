namespace WizRoster;

/// <summary>コピー先・入れ替え先のスロットを選ぶ小さなダイアログ。</summary>
internal sealed class SlotPickerDialog : Form
{
    private readonly ComboBox _slots = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };

    public SlotPickerDialog(string title, string message, IEnumerable<(int Slot, string Label)> slots)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        foreach (var (slot, label) in slots)
            _slots.Items.Add(new ValueItem(slot, label));
        if (_slots.Items.Count > 0)
            _slots.SelectedIndex = 0;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = message, AutoSize = true, Margin = new Padding(3, 3, 3, 8) });
        layout.Controls.Add(_slots);
        layout.Controls.Add(buttons);
        Controls.Add(layout);
    }

    public int SelectedSlot => UiHelpers.SelectedValue(_slots);
}
