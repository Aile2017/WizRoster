using System.Text;
using WizRoster.Core.Model;

namespace WizRoster.Core.Krn;

/// <summary>
/// アスキー移植版(PC-98 / PC-DOS など)のメッセージファイル ASCII.KRN。
/// ゲームの文字列をメッセージ番号で引けるように持っている。
/// <list type="bullet">
/// <item>ヘッダ 4 ワード: 索引表の開始ブロック, 索引表のバイト数, 範囲木の開始ブロック, 範囲木のバイト数</item>
/// <item>索引表: 1 件 1 ワード。値 ×2 がメッセージのバイト位置</item>
/// <item>範囲木: 1 件 5 ワード(開始番号, 終了番号, 索引の先頭, 左, 右)。番号 m は索引の (先頭 + m − 開始番号) 番目</item>
/// <item>メッセージ: 1 バイトの文字数に続いて本文。本文は
///   <c>文字[i] = (暗号[i] − 23·(i+1) − 67·(m mod 51)) mod 256</c> で戻す</item>
/// </list>
/// </summary>
public sealed class KrnMessageFile
{
    public const string FileName = "ASCII.KRN";

    private const int BlockSize = 512;
    private readonly byte[] _data;
    private readonly ushort[] _index;
    private readonly List<(int Lo, int Hi, int Base)> _ranges = [];

    public KrnMessageFile(byte[] data)
    {
        _data = data;
        if (data.Length < 8)
            throw new InvalidDataException($"{FileName} が短すぎます。");
        int indexBlock = WizBytes.U16(data, 0);
        int indexBytes = WizBytes.U16(data, 2);
        int treeBlock = WizBytes.U16(data, 4);
        int treeBytes = WizBytes.U16(data, 6);
        if ((long)indexBlock * BlockSize + indexBytes > data.Length || (long)treeBlock * BlockSize + treeBytes > data.Length)
            throw new InvalidDataException($"{FileName} のヘッダが読めません。");

        _index = new ushort[indexBytes / 2];
        for (int i = 0; i < _index.Length; i++)
            _index[i] = WizBytes.U16(data, indexBlock * BlockSize + i * 2);
        DataEnd = indexBlock * BlockSize;

        for (int off = treeBlock * BlockSize; off + 10 <= treeBlock * BlockSize + treeBytes; off += 10)
        {
            int lo = WizBytes.U16(data, off);
            int hi = WizBytes.U16(data, off + 2);
            int b = WizBytes.U16(data, off + 4);
            if (lo <= hi && hi < 0x8000)
                _ranges.Add((lo, hi, b));
        }
    }

    private int DataEnd { get; }

    /// <summary>メッセージ番号の一覧。</summary>
    public IEnumerable<int> MessageNumbers => _ranges.SelectMany(r => Enumerable.Range(r.Lo, r.Hi - r.Lo + 1)).Where(Has).Order();

    public bool Has(int number) => Slot(number) >= 0;

    /// <summary>メッセージを復号して返す(略記はそのまま)。ないときは null。</summary>
    public string? Get(int number)
    {
        int slot = Slot(number);
        if (slot < 0)
            return null;
        int start = _index[slot] * 2;
        if (start >= DataEnd)
            return null;
        int length = Math.Min(_data[start], DataEnd - start - 1);
        var text = new byte[length];
        int key = 67 * (number % 51);
        for (int i = 0; i < length; i++)
            text[i] = unchecked((byte)(_data[start + 1 + i] - 23 * (i + 1) - key));
        return Encoding.Latin1.GetString(text);
    }

    /// <summary>
    /// アイテム名などで使う小文字の略記を展開する(推測)。
    /// w = " OF ", x = "POTION", y = "SCROLL"。
    /// </summary>
    public static string ExpandAbbreviations(string text)
    {
        var sb = new StringBuilder(text.Length + 8);
        foreach (char ch in text)
        {
            sb.Append(ch switch
            {
                'w' => " OF ",
                'x' => "POTION",
                'y' => "SCROLL",
                _ => ch.ToString(),
            });
        }
        return sb.ToString();
    }

    private int Slot(int number)
    {
        foreach (var (lo, hi, b) in _ranges)
        {
            if (number >= lo && number <= hi)
            {
                int slot = b + number - lo;
                return slot < _index.Length ? slot : -1;
            }
        }
        return -1;
    }
}
