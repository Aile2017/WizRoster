namespace WizRoster.Core.Model;

/// <summary>
/// 職業ごとの、レベルに必要な経験値の表(SCENARIO.DATA の区分 7)。
/// 8 職業 × 13 個の TWIZLONG(6 バイト)が並ぶ。職業ごとに
/// <list type="bullet">
/// <item>[0]: レベル 14 以降、1 レベル上がるごとに必要な経験値の増分</item>
/// <item>[1]〜[12]: レベル 2〜13 になるのに必要な経験値(累計)</item>
/// </list>
/// </summary>
public sealed class ExperienceTable
{
    public const int ClassCount = 8;
    public const int EntriesPerClass = 13;
    public const int ByteLength = ClassCount * EntriesPerClass * 6;

    private readonly long[,] _table;

    private ExperienceTable(long[,] table) => _table = table;

    /// <summary>表として正しく読めたときだけ返す(値が増えていく並びになっていること)。</summary>
    public static ExperienceTable? TryParse(ReadOnlySpan<byte> data)
    {
        if (data.Length < ByteLength)
            return null;
        var t = new long[ClassCount, EntriesPerClass];
        for (int c = 0; c < ClassCount; c++)
        {
            for (int i = 0; i < EntriesPerClass; i++)
            {
                int off = (c * EntriesPerClass + i) * 6;
                // 各ワードは 0〜9999 のはず
                if (WizBytes.U16(data, off) > 9999 || WizBytes.U16(data, off + 2) > 9999 || WizBytes.U16(data, off + 4) > 9999)
                    return null;
                t[c, i] = WizBytes.WizLong(data, off);
            }
            if (t[c, 0] <= 0 || t[c, 1] <= 0)
                return null;
            for (int i = 2; i < EntriesPerClass; i++)
            {
                if (t[c, i] <= t[c, i - 1])
                    return null;
            }
        }
        return new ExperienceTable(t);
    }

    public static bool IsValidClass(int classIndex) => classIndex is >= 0 and < ClassCount;

    /// <summary>レベル <paramref name="level"/> になるのに必要な経験値(レベル 1 以下は 0)。上限は TWIZLONG の最大値。</summary>
    public long RequiredExperience(int classIndex, int level)
    {
        CheckClass(classIndex);
        if (level <= 1)
            return 0;
        if (level <= EntriesPerClass)
            return _table[classIndex, level - 1];
        long extra = (long)(level - EntriesPerClass) * _table[classIndex, 0];
        return Math.Min(WizBytes.WizLongMax, _table[classIndex, EntriesPerClass - 1] + extra);
    }

    /// <summary>経験値 <paramref name="experience"/> で到達できる最高のレベル。</summary>
    public int LevelFor(int classIndex, long experience)
    {
        CheckClass(classIndex);
        long top = _table[classIndex, EntriesPerClass - 1];
        if (experience >= top)
            return (int)Math.Min(int.MaxValue, EntriesPerClass + (experience - top) / _table[classIndex, 0]);
        int level = 1;
        for (int l = 2; l <= EntriesPerClass && experience >= _table[classIndex, l - 1]; l++)
            level = l;
        return level;
    }

    /// <summary>画面表示用の説明。例:「経験値は Lv 12 相当 / Lv 13 まであと 127,660」。</summary>
    public string Describe(int classIndex, int level, long experience)
    {
        int byExp = LevelFor(classIndex, experience);
        long next = RequiredExperience(classIndex, level + 1);
        return experience >= next
            ? $"経験値は Lv {byExp} 相当(宿屋でレベルが上がる見込み)"
            : $"経験値は Lv {byExp} 相当 / Lv {level + 1} まであと {next - experience:N0}";
    }

    private static void CheckClass(int classIndex)
    {
        if (!IsValidClass(classIndex))
            throw new ArgumentOutOfRangeException(nameof(classIndex));
    }
}
