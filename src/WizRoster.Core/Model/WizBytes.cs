using System.Buffers.Binary;
using System.Text;

namespace WizRoster.Core.Model;

/// <summary>p-System(Pascal)のデータ型を読み書きする補助関数。値はすべてリトルエンディアン。</summary>
public static class WizBytes
{
    /// <summary>TWIZLONG の最大値(各ワード 0〜9999 の 3 ワード)。</summary>
    public const long WizLongMax = 999_999_999_999L;

    public static ushort U16(ReadOnlySpan<byte> b, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(b[offset..]);

    public static short S16(ReadOnlySpan<byte> b, int offset) => BinaryPrimitives.ReadInt16LittleEndian(b[offset..]);

    public static void SetU16(Span<byte> b, int offset, int value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(b[offset..], unchecked((ushort)value));

    /// <summary>STRING[n]: 先頭 1 バイトが長さ。</summary>
    public static string PascalString(ReadOnlySpan<byte> b, int offset, int maxLength)
    {
        int len = Math.Min(b[offset], maxLength);
        return Encoding.Latin1.GetString(b.Slice(offset + 1, len));
    }

    /// <summary>STRING[n] を書く。残りは 0 で埋める。</summary>
    public static void SetPascalString(Span<byte> b, int offset, int maxLength, string value)
    {
        if (value.Length > maxLength)
            throw new ArgumentException($"{maxLength} 文字までです。", nameof(value));
        var area = b.Slice(offset, maxLength + 1);
        area.Clear();
        area[0] = (byte)value.Length;
        Encoding.Latin1.GetBytes(value, area[1..]);
    }

    /// <summary>TWIZLONG: 3 ワード。下位ワードが先頭で、各ワードは 0〜9999 の 10000 進数。</summary>
    public static long WizLong(ReadOnlySpan<byte> b, int offset) =>
        U16(b, offset + 4) * 100_000_000L + U16(b, offset + 2) * 10_000L + U16(b, offset);

    public static void SetWizLong(Span<byte> b, int offset, long value)
    {
        if (value is < 0 or > WizLongMax)
            throw new ArgumentOutOfRangeException(nameof(value));
        SetU16(b, offset, (int)(value % 10_000));
        SetU16(b, offset + 2, (int)(value / 10_000 % 10_000));
        SetU16(b, offset + 4, (int)(value / 100_000_000));
    }

    /// <summary>PACKED ARRAY OF 0..31 などの 5 ビット値。1 ワードに 3 つ詰め、ワードをまたがない。</summary>
    public static int Packed5(ReadOnlySpan<byte> b, int offset, int index)
    {
        int word = U16(b, offset + index / 3 * 2);
        return (word >> (index % 3 * 5)) & 0x1F;
    }

    public static void SetPacked5(Span<byte> b, int offset, int index, int value)
    {
        if (value is < 0 or > 31)
            throw new ArgumentOutOfRangeException(nameof(value));
        int pos = offset + index / 3 * 2;
        int shift = index % 3 * 5;
        int word = U16(b, pos);
        word = (word & ~(0x1F << shift)) | (value << shift);
        SetU16(b, pos, word);
    }

    /// <summary>PACKED ARRAY OF BOOLEAN: 要素 n はバイト n/8 のビット n%8。</summary>
    public static bool Bit(ReadOnlySpan<byte> b, int offset, int index) => (b[offset + index / 8] & (1 << (index % 8))) != 0;

    public static void SetBit(Span<byte> b, int offset, int index, bool value)
    {
        ref byte target = ref b[offset + index / 8];
        if (value)
            target |= (byte)(1 << (index % 8));
        else
            target &= (byte)~(1 << (index % 8));
    }
}
