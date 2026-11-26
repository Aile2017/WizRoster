using WizRoster.Core.Krn;

namespace WizRoster.Tests;

public class KrnTests
{
    /// <summary>暗号化の逆(テスト用)。文字[i] + 23·(i+1) + 67·(m mod 51)。</summary>
    private static byte[] Encrypt(int number, string text)
    {
        var b = new byte[1 + text.Length];
        b[0] = (byte)text.Length;
        for (int i = 0; i < text.Length; i++)
            b[1 + i] = unchecked((byte)(text[i] + 23 * (i + 1) + 67 * (number % 51)));
        return b;
    }

    /// <summary>メッセージ 2 つ(番号 5001, 5002)だけの小さな ASCII.KRN を作る。</summary>
    private static byte[] BuildFile()
    {
        var data = new byte[3 * 512];
        byte[] m1 = Encrypt(5001, "*HALITO");
        byte[] m2 = Encrypt(5002, "xwDIOS");
        m1.CopyTo(data, 8);
        int second = 8 + (m1.Length + 1) / 2 * 2;
        m2.CopyTo(data, second);

        // ヘッダ: 索引はブロック 1、範囲木はブロック 2
        void W(int off, int v) => BitConverter.GetBytes((ushort)v).CopyTo(data, off);
        W(0, 1); W(2, 4); W(4, 2); W(6, 20);
        W(512, 8 / 2); W(514, second / 2);
        // 先頭は見出しの節(使わない)、次が 5001〜5002 → 索引 0〜1
        W(1024, 0x8001); W(1026, 0x8001);
        W(1034, 5001); W(1036, 5002); W(1038, 0);
        return data;
    }

    [Fact]
    public void Get_DecryptsByNumber()
    {
        var krn = new KrnMessageFile(BuildFile());
        Assert.Equal("*HALITO", krn.Get(5001));
        Assert.Equal("xwDIOS", krn.Get(5002));
        Assert.Null(krn.Get(5003));
        Assert.Equal(new[] { 5001, 5002 }, krn.MessageNumbers);
    }

    [Theory]
    [InlineData("xwDIOS", "POTION OF DIOS")]
    [InlineData("ywKATINO", "SCROLL OF KATINO")]
    [InlineData("RINGwHEALING", "RING OF HEALING")]
    [InlineData("LONG SWORD", "LONG SWORD")]
    public void ExpandAbbreviations(string raw, string expected) =>
        Assert.Equal(expected, KrnMessageFile.ExpandAbbreviations(raw));
}
