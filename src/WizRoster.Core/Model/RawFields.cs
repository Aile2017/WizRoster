namespace WizRoster.Core.Model;

/// <summary>「その他」欄に生の数値で出す項目。名前は Wizardry のソースの変数名と、意味。</summary>
public sealed record RawField(int Offset, string Name, string Description, bool Signed);

public static class RawFields
{
    public static readonly IReadOnlyList<RawField> Extra =
    [
        new(174, "HPCALCMD", "命中の補正(職業・レベル・力と装備から計算)", true),
        new(178, "HEALPTS", "迷宮を 1 歩ごとの HP 回復量(毒の量を引いた分だけ増減)。持ち物の最大値", true),
        new(180, "CRITHITM", "クリティカル(首をはねる)。0 以外で有効。忍者と、装備した武器から", false),
        new(182, "SWINGCNT", "攻撃回数(1〜10)。職業・レベルと装備から", true),
        new(184, "HPDAMRC.1", "ダメージ ダイスの数(素手は 2。武器を持つと武器の値)", true),
        new(186, "HPDAMRC.2", "ダメージ ダイスの面(素手は 2、忍者は 4。武器を持つと武器の値)", true),
        new(188, "HPDAMRC.3", "ダメージ 加算(武器の値 + 力の補正)", true),
        new(190, "WEPVSTY2.0", "防御の対象(敵の種類)ビット列。持ち物の OR", false),
        new(192, "WEPVSTY2.1", "防御の対象 ビット列の戦闘用の写し(戦闘の始めに +190 から)", false),
        new(194, "WEPVSTY3.0", "抵抗 ビット列。持ち物の OR", false),
        new(196, "WEPVSTY3.1", "抵抗 ビット列の戦闘用の写し(戦闘の始めに +194 から)", false),
        new(198, "WEPVSTYP", "倍打の対象(敵の種類)ビット列。装備した武器の値", false),
        new(Character.OffPoison, "POISNAMT / LOSTXYL.X", "毒の量(0 = なし)。迷宮に置き去りのときは位置 X(−1 = 行方不明)", true),
        new(Character.OffLeftMazeY, "LOSTXYL.Y", "迷宮に置き去りのときの位置 Y", true),
        new(Character.OffLeftMazeLevel, "LOSTXYL.階", "迷宮に置き去りのときの階(0 以外なら置き去り。−1 = 行方不明)", true),
        new(Character.OffAwards, "AWARDS", "称号のビット(クリアの印など。ステータス画面で記号になる)", false),
    ];
}
