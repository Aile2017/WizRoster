namespace WizRoster.Core.Model;

/// <summary>
/// 入力できる値の範囲。画面の入力欄、JSON の読み込み、範囲外の警告で同じ値を使う。
/// p-System の Pascal の INTEGER は符号付き 16 ビットなので、32,768 以上はゲームからは負の数に見える。
/// </summary>
public static class ValueRanges
{
    public const int IntegerMax = short.MaxValue;   // 32,767
    public const int IntegerMin = short.MinValue;   // −32,768
    public const int Packed5Max = 31;
    public const int SpellPointsMax = 9;
    public const int NameMaxLength = 15;

    /// <summary>0〜32,767 の項目(位置と名前)。範囲外の警告に使う。</summary>
    public static readonly IReadOnlyList<(int Offset, string Name)> NonNegativeIntegers =
    [
        (Character.OffAge, "年齢"),
        (Character.OffMaxLevel, "最高レベル"),
        (Character.OffLevel, "レベル"),
        (Character.OffHp, "HP"),
        (Character.OffHpMax, "最大 HP"),
    ];

    /// <summary>TWIZLONG の項目(位置と名前)。各ワードが 0〜9,999 でないと形式として正しくない。</summary>
    public static readonly IReadOnlyList<(int Offset, string Name)> WizLongs =
    [
        (Character.OffGold, "所持金"),
        (Character.OffExp, "経験値"),
    ];

    /// <summary>TWIZLONG の 3 ワードが正しい形式か。</summary>
    public static bool IsValidWizLong(ReadOnlySpan<byte> b, int offset) =>
        WizBytes.U16(b, offset) <= 9999 && WizBytes.U16(b, offset + 2) <= 9999 && WizBytes.U16(b, offset + 4) <= 9999;

    /// <summary>名前・パスワードに使える文字(画面と同じく、空白から ~ までの ASCII)。</summary>
    public static bool IsValidNameText(string text) =>
        text.Length <= NameMaxLength && text.All(ch => ch is >= ' ' and <= '~');
}
