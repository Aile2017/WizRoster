namespace WizRoster.Core.Model;

/// <summary>画面表示用の日本語名。値はゲーム内の番号に対応する。</summary>
public static class GameNames
{
    public static readonly string[] Races = ["未設定", "人間", "エルフ", "ドワーフ", "ノーム", "ホビット"];

    public static readonly string[] Classes = ["戦士", "魔術師", "僧侶", "盗賊", "司教", "侍", "君主", "忍者"];

    public static readonly string[] Statuses = ["正常", "恐怖", "睡眠", "麻痺", "石化", "死亡", "灰", "消滅"];

    public static readonly string[] Alignments = ["なし", "善", "中立", "悪"];

    public static readonly string[] Attributes = ["力", "知恵", "信仰心", "体力", "素早さ", "運"];

    /// <summary>
    /// LUCKSKIL(セービングスロー)5 種の用途。Apple II 版 #1〜#3 と PC-98 版 #1 のプログラム(p-code)を解析して確かめた。
    /// 値は小さいほど避けやすく、ゲームの計算では 1 以上になる。
    /// </summary>
    public static readonly string[] SavingThrows = ["毒・麻痺・即死", "石化", "未使用", "ブレス・ガス", "呪文・罠"];

    /// <summary>セービングスローの説明(画面のツールチップ用)。</summary>
    public static readonly string[] SavingThrowDetails =
    [
        "敵の打撃による毒・麻痺・クリティカル(即死)。0〜19 の乱数がこの値より大きければ避ける。\n職業: 戦士 −3、侍・君主 −2、忍者 −3。種族: 人間 −1",
        "敵の打撃による石化。0〜19 の乱数がこの値より大きければ避ける。\n職業: 僧侶 −3、司教・君主 −2、忍者 −2。種族: ノーム −2",
        "どの判定にも使われていない(値の計算だけはされる)。\n職業: 司教 −2、忍者 −4。種族: エルフ −2",
        "ブレス(ダメージ半減)と宝箱のガス爆弾。0〜19 の乱数がこの値以上なら避ける。\n職業: 盗賊 −3、忍者 −3。種族: ドワーフ −4",
        "呪文と宝箱の罠(職業別に効くもの)。\n職業: 魔術師 −3、司教・侍 −2、忍者 −2。種族: ホビット −3",
    ];

    /// <summary>セービングスローの計算式(画面表示用)。</summary>
    public const string SavingThrowFormula =
        "ゲームの計算: 20 − レベル÷5 − 運÷6 から、職業と種族の分を引く(どれも 1 未満にはならない)。作成直後は 16。\n" +
        "値は 0〜31 まで入るが、ゲームの計算で出る範囲は 1〜19。";

    /// <summary>アイテムの種類(TOBJTYPE)。</summary>
    public static readonly string[] ItemTypes = ["武器", "鎧", "盾", "兜", "小手", "特殊", "その他"];

    public static string Lookup(string[] names, int value) =>
        value >= 0 && value < names.Length ? names[value] : $"不明({value})";
}
