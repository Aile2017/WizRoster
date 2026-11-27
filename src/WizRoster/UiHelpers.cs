namespace WizRoster;

/// <summary>コンボボックスの 1 項目。値と表示文字列の組。</summary>
internal sealed record ValueItem(int Value, string Text, bool IsUnknown = false)
{
    public override string ToString() => Text;
}

internal static class UiHelpers
{
    public static ComboBox EnumCombo(IReadOnlyList<string> names, int width = 140)
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width };
        FillEnumCombo(cb, names);
        return cb;
    }

    /// <summary>「値: 名前」の項目を入れ直す(ディスクごとに名前が変わるため)。</summary>
    public static void FillEnumCombo(ComboBox cb, IReadOnlyList<string> names)
    {
        cb.BeginUpdate();
        cb.Items.Clear();
        for (int i = 0; i < names.Count; i++)
            cb.Items.Add(new ValueItem(i, $"{i}: {names[i]}"));
        cb.EndUpdate();
    }

    /// <summary>値の項目を選ぶ。一覧にない値なら、<paramref name="unknownLabel"/>(省略時は「不明な値」)の項目を足して選ぶ。</summary>
    public static void SelectValue(ComboBox cb, int value, string? unknownLabel = null)
    {
        for (int i = cb.Items.Count - 1; i >= 0; i--)
        {
            if (cb.Items[i] is ValueItem { IsUnknown: true })
                cb.Items.RemoveAt(i);
        }
        for (int i = 0; i < cb.Items.Count; i++)
        {
            if (cb.Items[i] is ValueItem v && v.Value == value)
            {
                cb.SelectedIndex = i;
                return;
            }
        }
        cb.SelectedIndex = cb.Items.Add(new ValueItem(value, unknownLabel ?? $"{value}: (不明な値)", IsUnknown: true));
    }

    public static int SelectedValue(ComboBox cb) => (cb.SelectedItem as ValueItem)?.Value ?? 0;

    public static NumericUpDown Num(decimal min, decimal max, int width = 80) => new()
    {
        Minimum = min,
        Maximum = max,
        Width = width,
        TextAlign = HorizontalAlignment.Right,
        ThousandsSeparator = max >= 100_000,
    };

    /// <summary>範囲外の値は端に寄せて設定する(壊れたデータを開いても例外にしない)。</summary>
    public static void SetNum(NumericUpDown n, decimal value) => n.Value = Math.Clamp(value, n.Minimum, n.Maximum);

    public static TableLayoutPanel FormTable(int columns = 2) => new()
    {
        ColumnCount = columns,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Top,
        Padding = new Padding(2),
    };

    public static void AddRow(TableLayoutPanel t, string label, params Control[] controls)
    {
        int row = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(Label(label), 0, row);
        if (controls.Length == 1)
        {
            t.Controls.Add(controls[0], 1, row);
            return;
        }
        var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        flow.Controls.AddRange(controls);
        t.Controls.Add(flow, 1, row);
    }

    public static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 6, 3, 3),
    };

    public static GroupBox Group(string title, Control content)
    {
        var g = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(6),
        };
        content.Dock = DockStyle.Fill;
        g.Controls.Add(content);
        return g;
    }
}
