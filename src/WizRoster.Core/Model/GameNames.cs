namespace WizRoster.Core.Model;

/// <summary>画面表示用の日本語名。値はゲーム内の番号に対応する。</summary>
public static class GameNames
{
    public static readonly string[] Races = ["未設定", "人間", "エルフ", "ドワーフ", "ノーム", "ホビット"];

    public static readonly string[] Classes = ["戦士", "魔術師", "僧侶", "盗賊", "司教", "侍", "君主", "忍者"];

    public static readonly string[] Statuses = ["正常", "恐怖", "睡眠", "麻痺", "石化", "死亡", "灰", "消滅"];

    public static readonly string[] Alignments = ["なし", "善", "中立", "悪"];

    public static readonly string[] Attributes = ["力", "知恵", "信仰心", "体力", "素早さ", "運"];

    /// <summary>LUCKSKIL(セービングスロー)5 種。並びは推測。</summary>
    public static readonly string[] SavingThrows = ["即死", "毒", "石化", "ブレス", "呪文"];

    /// <summary>アイテムの種類(TOBJTYPE)。</summary>
    public static readonly string[] ItemTypes = ["武器", "鎧", "盾", "兜", "小手", "特殊", "その他"];

    public static string Lookup(string[] names, int value) =>
        value >= 0 && value < names.Length ? names[value] : $"不明({value})";
}
