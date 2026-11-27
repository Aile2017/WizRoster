using WizRoster.Core.Krn;
using WizRoster.Core.Model;

namespace WizRoster.Core.Scenario;

/// <summary>
/// 種族・職業・状態・属性の名前。ディスクにあるものを使う。
/// <list type="number">
/// <item>ASCII.KRN(アスキー移植版): 種族 10000+(値−1)、属性 10010+(値−1)、職業 10030+値、状態 10040+値</item>
/// <item>SCENARIO.DATA の目次: +106 種族×6、+166 職業×8、+246 状態×8、+326 属性×4(各 STRING[9])。
///   アスキー移植版の #1 は属性の欄が壊れているので、英字として正しい名前だけを使う</item>
/// <item>どちらにもないときは、日本語の既定の名前(<see cref="GameNames"/>)</item>
/// </list>
/// </summary>
public sealed class NameTables
{
    private NameTables(string[] races, string[] classes, string[] statuses, string[] alignments, string source)
    {
        Races = races;
        Classes = classes;
        Statuses = statuses;
        Alignments = alignments;
        Source = source;
    }

    public IReadOnlyList<string> Races { get; }
    public IReadOnlyList<string> Classes { get; }
    public IReadOnlyList<string> Statuses { get; }
    public IReadOnlyList<string> Alignments { get; }

    /// <summary>名前をどこから読んだか(画面表示用)。</summary>
    public string Source { get; }

    /// <summary>日本語の既定の名前だけの表(ディスクがないとき)。</summary>
    public static NameTables Default { get; } = new(
        GameNames.Races, GameNames.Classes, GameNames.Statuses, GameNames.Alignments, "既定(日本語)");

    public string Race(int value) => Lookup(Races, value);
    public string Class(int value) => Lookup(Classes, value);
    public string Status(int value) => Lookup(Statuses, value);
    public string Alignment(int value) => Lookup(Alignments, value);

    public static string Lookup(IReadOnlyList<string> names, int value) =>
        value >= 0 && value < names.Count ? names[value] : $"不明({value})";

    internal static NameTables Load(byte[] toc, KrnMessageFile? messages)
    {
        var used = new SortedSet<string>();

        string[] Build(string[] defaults, int tocOffset, Func<int, int?> krnNumber)
        {
            var result = new string[defaults.Length];
            for (int v = 0; v < defaults.Length; v++)
            {
                string? name = krnNumber(v) is int m ? Clean(messages?.Get(m)) : null;
                if (name != null)
                {
                    used.Add(KrnMessageFile.FileName);
                }
                else
                {
                    name = Clean(WizBytes.PascalString(toc, tocOffset + v * 10, 9));
                    if (name != null)
                        used.Add("SCENARIO.DATA");
                }
                if (name == null)
                    used.Add("既定");
                result[v] = name ?? defaults[v];
            }
            return result;
        }

        var races = Build(GameNames.Races, 106, v => v >= 1 ? 10000 + v - 1 : null);
        var classes = Build(GameNames.Classes, 166, v => 10030 + v);
        var statuses = Build(GameNames.Statuses, 246, v => 10040 + v);
        var aligns = Build(GameNames.Alignments, 326, v => v >= 1 ? 10010 + v - 1 : null);
        return new NameTables(races, classes, statuses, aligns, string.Join(" + ", used));
    }

    /// <summary>英大文字の名前として正しければ前後の空白を除いて返す。壊れていれば null。</summary>
    private static string? Clean(string? text)
    {
        if (text == null)
            return null;
        string s = text.Trim();
        if (s.Length is < 2 or > 15)
            return null;
        if (!char.IsAsciiLetterUpper(s[0]))
            return null;
        foreach (char ch in s)
        {
            if (!(char.IsAsciiLetterUpper(ch) || ch is ' ' or '-' or '.' or '/' or '\''))
                return null;
        }
        return s;
    }
}
