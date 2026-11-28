using System.Text;
using WizRoster.Core;
using WizRoster.Core.Model;
using WizRoster.Core.Scenario;

namespace WizRoster;

/// <summary>
/// ディスクから読んだアイテム表・モンスター表を一覧で見る画面(読み取り専用)。
/// 列の見出しで並べ替えができ、行を選ぶと下にすべての項目を出す。
/// </summary>
internal sealed class GameDataDialog : Form
{
    public enum Page { Items, Monsters }

    private readonly RosterDocument _doc;
    private readonly ScenarioFile _scenario;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _itemGrid = NewGrid();
    private readonly DataGridView _monsterGrid = NewGrid();
    private readonly TextBox _itemDetail = NewDetail();
    private readonly TextBox _monsterDetail = NewDetail();
    private readonly PictureBox _monsterPicture = new()
    {
        Size = new Size(PictureWidth, PictureHeight),
        BackColor = Color.Black,
        SizeMode = PictureBoxSizeMode.CenterImage,
        Margin = new Padding(6, 0, 0, 0),
    };
    private readonly Label _pictureNote = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(6, 4, 0, 0) };

    // 絵は 96×80 を 3 倍で出す(200 ライン用の 96×40 は縦を 2 倍にしてから)
    private const int PictureScale = 3;
    private const int PictureWidth = 96 * PictureScale;
    private const int PictureHeight = 80 * PictureScale;

    public GameDataDialog(RosterDocument doc)
    {
        _doc = doc;
        _scenario = doc.Scenario;
        Text = $"ゲームデータ - {doc.Game.DisplayName}";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Size = new Size(1200, 800);
        MinimumSize = new Size(700, 450);

        BuildItemGrid();
        BuildMonsterGrid();
        _itemGrid.SelectionChanged += (_, _) => _itemDetail.Text = SelectedIndex(_itemGrid) is int i ? ItemText(_scenario.Items[i]) : "";
        _monsterGrid.SelectionChanged += (_, _) => ShowMonster(SelectedIndex(_monsterGrid));

        _tabs.TabPages.Add(MakePage($"アイテム({_scenario.Items.Count})", _itemGrid, _itemDetail));
        _tabs.TabPages.Add(MakePage($"モンスター({_scenario.Monsters.Count})", _monsterGrid, MonsterBottom()));
        Controls.Add(_tabs);
    }

    public void ShowPage(Page page) => _tabs.SelectedIndex = (int)page;

    // ---- 部品 ----

    private static DataGridView NewGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        BackgroundColor = SystemColors.Window,
    };

    private static TextBox NewDetail() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("BIZ UDゴシック", 10f),
        BackColor = SystemColors.Window,
    };

    private static TabPage MakePage(string title, DataGridView grid, Control detail)
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(grid);
        split.Panel2.Controls.Add(detail);
        var page = new TabPage(title);
        page.Controls.Add(split);
        page.Layout += (_, _) =>
        {
            // 下の詳細欄は絵が収まる高さ(ウィンドウを開いた最初だけ合わせる)
            if (split.Tag == null && split.Height > 400)
            {
                split.SplitterDistance = split.Height - (PictureHeight + 30);
                split.Tag = true;
            }
        };
        return page;
    }

    private static void AddColumn(DataGridView grid, string header, Type type, bool right = false)
    {
        var col = new DataGridViewTextBoxColumn { HeaderText = header, ValueType = type, SortMode = DataGridViewColumnSortMode.Automatic };
        if (right)
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns.Add(col);
    }

    /// <summary>選んでいる行のデータ番号(並べ替えても変わらないように、行の Tag に入れてある)。</summary>
    private static int? SelectedIndex(DataGridView grid) =>
        grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is int i ? i : null;

    private static string Signed(int v) => v == 0 ? "0" : v.ToString("+0;-0");

    // ---- アイテム ----

    private void BuildItemGrid()
    {
        var g = _itemGrid;
        AddColumn(g, "番号", typeof(int), right: true);
        AddColumn(g, "名前", typeof(string));
        AddColumn(g, "未鑑定名", typeof(string));
        AddColumn(g, "種類", typeof(string));
        AddColumn(g, "属性", typeof(string));
        AddColumn(g, "呪い", typeof(string));
        AddColumn(g, "価格", typeof(long), right: true);
        AddColumn(g, "在庫", typeof(int), right: true);
        AddColumn(g, "AC 補正", typeof(int), right: true);
        AddColumn(g, "命中", typeof(int), right: true);
        AddColumn(g, "ダメージ", typeof(string));
        AddColumn(g, "攻撃回数", typeof(int), right: true);
        AddColumn(g, "クリティカル", typeof(string));
        AddColumn(g, "呪文", typeof(string));
        AddColumn(g, "装備できる職業", typeof(string));
        g.Columns[6].DefaultCellStyle.Format = "N0";

        foreach (var it in _scenario.Items)
        {
            int row = g.Rows.Add(
                it.Index, it.Name, it.UnknownName,
                GameNames.Lookup(GameNames.ItemTypes, it.Type),
                AlignmentName(it.Alignment),
                it.Cursed ? "呪い" : "",
                it.Price,
                it.Index < _doc.BoltacStock.Length ? _doc.BoltacStock[it.Index] : it.BoltacStock,
                it.ArmorMod, it.HitMod,
                it.DamageDice > 0 ? new Dice(it.DamageDice, it.DamageSides, it.DamageBonus).ToString() : "",
                it.ExtraSwings,
                it.CriticalHit ? "あり" : "",
                SpellName(it.SpellPower),
                it.ClassUseText(_scenario.Names.Classes));
            g.Rows[row].Tag = it.Index;
        }
        g.Columns[8].ToolTipText = "装備すると AC がこの値だけ下がる(良くなる)。";
    }

    private string AlignmentName(int value) => value == 0 ? "" : _scenario.Names.Alignment(value);

    private string SpellName(int bit) => bit > 0 ? _scenario.FindSpell(bit)?.Name ?? $"#{bit}" : "";

    private string ItemText(ItemDef it)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{it.Index:D3} {it.Name}  (未鑑定名 {it.UnknownName})");
        sb.AppendLine($"種類 {GameNames.Lookup(GameNames.ItemTypes, it.Type)}  属性 {(it.Alignment == 0 ? "なし" : _scenario.Names.Alignment(it.Alignment))}  " +
                      $"呪い {(it.Cursed ? "あり" : "なし")}  価格 {it.Price:N0}  " +
                      $"ボルタックの在庫 {StockText(it.Index < _doc.BoltacStock.Length ? _doc.BoltacStock[it.Index] : it.BoltacStock)}");
        sb.AppendLine($"装備できる職業 {it.ClassUseText(_scenario.Names.Classes)}");
        sb.AppendLine($"AC 補正 {it.ArmorMod}(装備で AC {Signed(-it.ArmorMod)})  命中 {Signed(it.HitMod)}  " +
                      $"ダメージ {(it.DamageDice > 0 ? new Dice(it.DamageDice, it.DamageSides, it.DamageBonus).ToString() : "-")}  " +
                      $"攻撃回数 {it.ExtraSwings}  クリティカル {(it.CriticalHit ? "あり" : "なし")}");
        sb.AppendLine($"倍打の対象 {GameNames.BitNames(it.PurposedVs, GameNames.MonsterClasses)}");
        sb.AppendLine($"防御の対象 {GameNames.BitNames(it.ProtectVs, GameNames.MonsterClasses)}");
        sb.AppendLine($"抵抗 {GameNames.BitNames(it.ResistVs, GameNames.ResistBits)}");
        sb.AppendLine($"HP 回復 {it.HealPoints}(1 歩ごと。持っているだけで効く)");
        string change = it.ChangeChance > 0 || it.ChangeTo > 0
            ? $"{(_scenario.FindItem(it.ChangeTo) is { } to ? $"{to.Index:D3} {to.Name}" : it.ChangeTo.ToString())}({it.ChangeChance}%)"
            : "なし";
        sb.AppendLine($"呪文 {(it.SpellPower > 0 ? SpellName(it.SpellPower) : "なし")}  使ったあとの変化 {change}  特殊 {it.Special}");
        if (it.NameNote != null)
            sb.AppendLine(it.NameNote);
        return sb.ToString();
    }

    private static string StockText(int stock) => stock switch
    {
        -1 => "無限",
        0 => "0(店に並ばない)",
        _ => stock.ToString(),
    };

    // ---- モンスター ----

    private void BuildMonsterGrid()
    {
        var g = _monsterGrid;
        AddColumn(g, "番号", typeof(int), right: true);
        AddColumn(g, "名前", typeof(string));
        AddColumn(g, "不確定名", typeof(string));
        AddColumn(g, "種族", typeof(string));
        AddColumn(g, "Lv", typeof(int), right: true);
        AddColumn(g, "HP", typeof(string));
        AddColumn(g, "AC", typeof(int), right: true);
        AddColumn(g, "出現数", typeof(string));
        AddColumn(g, "攻撃", typeof(string));
        AddColumn(g, "魔術師", typeof(int), right: true);
        AddColumn(g, "僧侶", typeof(int), right: true);
        AddColumn(g, "ブレス", typeof(string));
        AddColumn(g, "呪文無効化%", typeof(int), right: true);
        AddColumn(g, "レベル吸収", typeof(int), right: true);
        AddColumn(g, "特殊能力", typeof(string));
        AddColumn(g, "絵", typeof(int), right: true);
        g.Columns[4].ToolTipText = "HP のダイスの数(モンスターのレベルにあたる)。";
        g.Columns[9].ToolTipText = "唱える魔術師の呪文のレベル(0 = 唱えない)。";
        g.Columns[10].ToolTipText = "唱える僧侶の呪文のレベル(0 = 唱えない)。";

        foreach (var m in _scenario.Monsters)
        {
            int row = g.Rows.Add(
                m.Index, m.Name, m.UnknownName,
                GameNames.Lookup(GameNames.MonsterClasses, m.Class),
                m.HitPoints.Count, m.HitPoints.ToString(), m.ArmorClass, m.GroupSize.ToString(),
                AttackText(m),
                m.MageSpellLevel, m.PriestSpellLevel,
                m.Breath == 0 ? "" : GameNames.Lookup(GameNames.Breaths, m.Breath),
                m.SpellResistance, m.LevelDrain,
                m.SpecialBits == 0 ? "" : GameNames.BitNames(m.SpecialBits, GameNames.MonsterSpecials),
                m.Picture);
            g.Rows[row].Tag = m.Index;
        }
    }

    private Control MonsterBottom()
    {
        var right = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
        right.Controls.Add(_monsterPicture);
        right.Controls.Add(_pictureNote);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(_monsterDetail, 0, 0);
        layout.Controls.Add(right, 1, 0);
        return layout;
    }

    private void ShowMonster(int? index)
    {
        var old = _monsterPicture.Image;
        _monsterPicture.Image = null;
        old?.Dispose();
        if (index is not int i)
        {
            _monsterDetail.Text = "";
            _pictureNote.Text = "";
            return;
        }
        var m = _scenario.Monsters[i];
        _monsterDetail.Text = MonsterText(m);
        if (_doc.MonsterPictures is not { } pictures)
        {
            _pictureNote.Text = "この機種の絵はまだ読めません";
            return;
        }
        if (pictures.Get(m.Picture) is not { } pic)
        {
            _pictureNote.Text = $"絵 {m.Picture} はありません";
            return;
        }
        _monsterPicture.Image = ToBitmap(pic);
        _pictureNote.Text = $"{pictures.FileName} の {m.Picture} 枚目";
    }

    /// <summary>色番号の絵を拡大したビットマップにする(縦長のドットは縦を伸ばす)。</summary>
    private static Bitmap ToBitmap(MonsterPicture pic)
    {
        int sx = PictureScale, sy = PictureScale * pic.PixelAspect;
        var bmp = new Bitmap(pic.Width * sx, pic.Height * sy);
        for (int y = 0; y < pic.Height; y++)
        {
            for (int x = 0; x < pic.Width; x++)
            {
                var (r, g, b) = MonsterPicture.Palette[pic.Pixels[y * pic.Width + x] & 7];
                var c = Color.FromArgb(r, g, b);
                for (int dy = 0; dy < sy; dy++)
                {
                    for (int dx = 0; dx < sx; dx++)
                        bmp.SetPixel(x * sx + dx, y * sy + dy, c);
                }
            }
        }
        return bmp;
    }

    private static string AttackText(MonsterDef m) =>
        m.Attacks.Count == 0 ? "" : string.Join(" / ", m.Attacks.Select(a => a.ToString()));

    private string MonsterName(int index) =>
        index >= 0 && index < _scenario.Monsters.Count ? $"{index:D3} {_scenario.Monsters[index].Name}" : index.ToString();

    private string MonsterText(MonsterDef m)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{m.Index:D3} {m.Name} / {m.NamePlural}");
        sb.AppendLine($"    不確定名 {m.UnknownName} / {m.UnknownNamePlural}");
        sb.AppendLine($"種族 {GameNames.Lookup(GameNames.MonsterClasses, m.Class)}  Lv {m.HitPoints.Count}  " +
                      $"HP {m.HitPoints}({m.HitPoints.Min}〜{m.HitPoints.Max})  AC {m.ArmorClass}  " +
                      $"出現数 {m.GroupSize}({Math.Max(1, m.GroupSize.Min)}〜{m.GroupSize.Max})");
        sb.AppendLine($"攻撃 {m.Attacks.Count} 回: {(m.Attacks.Count == 0 ? "なし" : AttackText(m))}");
        sb.AppendLine($"呪文 魔術師 Lv{m.MageSpellLevel}  僧侶 Lv{m.PriestSpellLevel}  " +
                      $"ブレス {GameNames.Lookup(GameNames.Breaths, m.Breath)}  呪文無効化 {m.SpellResistance}%");
        sb.AppendLine($"レベル吸収 {m.LevelDrain}  HP 回復 {m.HealPoints}  " +
                      $"出現の上限 {(m.Unique < 0 ? "なし" : m.Unique.ToString())}");
        sb.AppendLine($"抵抗 {GameNames.BitNames(m.ResistBits, GameNames.ResistBits)}");
        sb.AppendLine($"特殊能力 {GameNames.BitNames(m.SpecialBits, GameNames.MonsterSpecials)}");
        sb.AppendLine($"仲間 {(m.TeamPercent > 0 ? $"{MonsterName(m.Team)}({m.TeamPercent}%)" : "なし")}");
        sb.AppendLine($"宝 徘徊 {m.Reward1}  玄室 {m.Reward2}(宝の表の番号)");
        sb.AppendLine($"経験値 {(m.Experience == 0 ? "0(データは 0。ゲームが計算していると思われる。未解析)" : m.Experience.ToString("N0"))}  絵の番号 {m.Picture}");
        return sb.ToString();
    }
}
