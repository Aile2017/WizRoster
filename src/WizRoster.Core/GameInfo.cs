using WizRoster.Core.Scenario;
using WizRoster.Core.Ucsd;

namespace WizRoster.Core;

/// <summary>開いたディスクがどの機種の、どのシナリオか。</summary>
/// <param name="Platform">機種名。分からなければ null。</param>
/// <param name="PlatformReason">機種を判断した手がかり(画面表示用)。</param>
/// <param name="Scenario">シナリオ番号(1〜3)。分からなければ null。</param>
public sealed record GameInfo(string? Platform, string PlatformReason, int? Scenario)
{
    private static readonly string[] ScenarioNames = ["", "狂王の試練場", "ダイヤモンドの騎士", "リルガミンの遺産"];

    public string ScenarioName => Scenario is int n ? $"#{n} {ScenarioNames[n]}" : "シナリオ不明";

    /// <summary>例: 「PC-DOS 版 #2 ダイヤモンドの騎士」。</summary>
    public string DisplayName => $"{Platform ?? "機種不明"}{(Platform != null ? " 版" : "")} {ScenarioName}";

    /// <summary>
    /// ディレクトリのファイル構成と、SCENARIO.DATA の目次から判別する。
    /// ファイル名や、イメージの形式(生イメージ・D88 など)だけでは機種は決まらないので使わない。
    /// </summary>
    public static GameInfo Identify(UcsdDirectory directory, ScenarioFile scenario)
    {
        var (platform, reason) = IdentifyPlatform(directory);
        return new GameInfo(platform, reason, IdentifyScenario(scenario));
    }

    private static (string?, string) IdentifyPlatform(UcsdDirectory dir)
    {
        bool Has(string name) => dir.Find(name) != null;

        // 機種ごとにしかないファイル(#1〜#3 の全イメージで確認)。
        // DERF は MSX2 版と PC-DOS 版の #2/#3 の両方に、SYSTEM.INTERP は PC-98 版と PC-DOS 版の両方にあるので決め手にしない。
        if (Has("SYSTEM.8801"))
            return ("PC-8801", "SYSTEM.8801 がある");
        if (Has("HAS.STROPS") || Has("HAS.CACHE"))
            return ("PC-DOS", "HAS.STROPS / HAS.CACHE がある");
        if (Has("NOFORMAT"))
            return ("MSX2", "NOFORMAT がある");
        if (Has("SCENARIO.MESGS") && !Has("ASCII.KRN"))
            return ("Apple II", "SCENARIO.MESGS があり ASCII.KRN がない");
        if (Has("SYSTEM.INTERP") && Has("KANJI.KRN"))
            return ("PC-98", "SYSTEM.INTERP と KANJI.KRN がある");
        return (null, "機種を示すファイルが見つからない");
    }

    /// <summary>
    /// アイテム数で判別し、シナリオ名で確かめる。
    /// PC-98 版・PC-8801 版の #1 はシナリオ名の欄が書き換わっているので、アイテム数を優先する。
    /// </summary>
    private static int? IdentifyScenario(ScenarioFile scenario)
    {
        int? byCount = scenario.RecordCounts[(int)Zone.Item] switch
        {
            101 => 1,
            130 => 2,
            104 => 3,
            _ => null,
        };
        string title = scenario.Title.ToUpperInvariant();
        int? byTitle = title.Contains("PROVING GROUNDS") ? 1
            : title.Contains("KNIGHT OF DIAMONDS") ? 2
            : title.Contains("LLYLGAMYN") ? 3
            : null;
        return byCount ?? byTitle;
    }
}
