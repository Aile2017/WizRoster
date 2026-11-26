namespace WizRoster.Core.Model;

/// <summary>「その他」欄に生の数値で出す項目。名前は Wizardry のソースの変数名と、推測した意味。</summary>
public sealed record RawField(int Offset, string Name, string Description, bool Signed);

public static class RawFields
{
    public static readonly IReadOnlyList<RawField> Extra =
    [
        new(174, "HPCALCMD", "命中の補正(推測)", true),
        new(178, "HEALPTS", "毎ターンの HP 回復(推測)", true),
        new(180, "CRITHITM", "クリティカル(首をはねる)。0 以外で有効(推測)", false),
        new(182, "SWINGCNT", "攻撃回数(推測)", true),
        new(184, "HPDAMRC.1", "ダメージ ダイスの数(推測)", true),
        new(186, "HPDAMRC.2", "ダメージ ダイスの面(推測)", true),
        new(188, "HPDAMRC.3", "ダメージ 加算(推測)", true),
        new(190, "WEPVSTY2.0", "防御の対象 ビット列(推測)", false),
        new(192, "WEPVSTY2.1", "防御の対象 ビット列(推測)", false),
        new(194, "WEPVSTY3.0", "抵抗 ビット列(推測)", false),
        new(196, "WEPVSTY3.1", "抵抗 ビット列(推測)", false),
        new(198, "WEPVSTYP", "倍打の対象 ビット列(推測)", false),
        new(200, "LOSTXYL.1", "迷宮での位置 / 毒 / 称号 1(推測)", false),
        new(202, "LOSTXYL.2", "迷宮での位置 / 毒 / 称号 2(推測)", false),
        new(204, "LOSTXYL.3", "迷宮での位置 / 毒 / 称号 3(推測)", false),
        new(206, "LOSTXYL.4", "迷宮での位置 / 毒 / 称号 4(推測)", false),
    ];
}
