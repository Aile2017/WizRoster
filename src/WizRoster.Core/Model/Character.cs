namespace WizRoster.Core.Model;

/// <summary>持ち物 1 件(8 バイト)。</summary>
public readonly record struct Possession(bool Equipped, bool Cursed, bool Identified, int ItemIndex);

/// <summary>
/// キャラクタ 1 人分のレコード(TCHAR, 208 バイト)。
/// 値はすべて <see cref="Raw"/> に直接読み書きするので、意味の分からない部分もそのまま残る。
/// </summary>
public sealed class Character
{
    public const int RecordSize = 208;
    public const int MaxItems = 8;
    public const int SpellBitCount = 51;

    // 項目の位置
    public const int OffName = 0;
    public const int OffPassword = 16;
    public const int OffInMaze = 32;
    public const int OffRace = 34;
    public const int OffClass = 36;
    public const int OffAge = 38;
    public const int OffStatus = 40;
    public const int OffAlign = 42;
    public const int OffAttrib = 44;
    public const int OffSaves = 48;
    public const int OffGold = 52;
    public const int OffItemCount = 58;
    public const int OffItems = 60;
    public const int OffExp = 124;
    public const int OffMaxLevel = 130;
    public const int OffLevel = 132;
    public const int OffHp = 134;
    public const int OffHpMax = 136;
    public const int OffSpells = 138;
    public const int OffMageSp = 146;
    public const int OffPriestSp = 160;
    public const int OffArmorClass = 176;

    public Character(byte[] raw)
    {
        if (raw.Length != RecordSize)
            throw new ArgumentException($"キャラクタは {RecordSize} バイトです。", nameof(raw));
        Raw = raw;
    }

    public byte[] Raw { get; }

    public Character Clone() => new((byte[])Raw.Clone());

    /// <summary>名前が空なら空きスロット。</summary>
    public bool IsEmpty => Raw[OffName] == 0;

    public string Name
    {
        get => WizBytes.PascalString(Raw, OffName, 15);
        set => WizBytes.SetPascalString(Raw, OffName, 15, value);
    }

    public string Password
    {
        get => WizBytes.PascalString(Raw, OffPassword, 15);
        set => WizBytes.SetPascalString(Raw, OffPassword, 15, value);
    }

    public bool InMaze
    {
        get => Word(OffInMaze) != 0;
        set => SetWord(OffInMaze, value ? 1 : 0);
    }

    public int Race { get => Word(OffRace); set => SetWord(OffRace, value); }
    public int Class { get => Word(OffClass); set => SetWord(OffClass, value); }
    /// <summary>年齢(週)。</summary>
    public int Age { get => Word(OffAge); set => SetWord(OffAge, value); }
    public int Status { get => Word(OffStatus); set => SetWord(OffStatus, value); }
    public int Alignment { get => Word(OffAlign); set => SetWord(OffAlign, value); }

    /// <summary>能力値(0:力 1:知恵 2:信仰心 3:体力 4:素早さ 5:運)。0〜31。</summary>
    public int GetAttribute(int index) => WizBytes.Packed5(Raw, OffAttrib, CheckIndex(index, 6));
    public void SetAttribute(int index, int value) => WizBytes.SetPacked5(Raw, OffAttrib, CheckIndex(index, 6), value);

    /// <summary>セービングスロー(LUCKSKIL)5 つ。0〜31。</summary>
    public int GetSavingThrow(int index) => WizBytes.Packed5(Raw, OffSaves, CheckIndex(index, 5));
    public void SetSavingThrow(int index, int value) => WizBytes.SetPacked5(Raw, OffSaves, CheckIndex(index, 5), value);

    public long Gold { get => WizBytes.WizLong(Raw, OffGold); set => WizBytes.SetWizLong(Raw, OffGold, value); }
    public long Experience { get => WizBytes.WizLong(Raw, OffExp); set => WizBytes.SetWizLong(Raw, OffExp, value); }

    public int ItemCount { get => Word(OffItemCount); set => SetWord(OffItemCount, value); }

    public Possession GetItem(int slot)
    {
        int o = OffItems + CheckIndex(slot, MaxItems) * 8;
        return new Possession(Word(o) != 0, Word(o + 2) != 0, Word(o + 4) != 0, Word(o + 6));
    }

    public void SetItem(int slot, Possession item)
    {
        int o = OffItems + CheckIndex(slot, MaxItems) * 8;
        SetBool(o, item.Equipped);
        SetBool(o + 2, item.Cursed);
        SetBool(o + 4, item.Identified);
        SetWord(o + 6, item.ItemIndex);
    }

    /// <summary>持ち物を前に詰めて書き込み、残りの欄は 0 にする。</summary>
    public void SetItems(IReadOnlyList<Possession> items)
    {
        if (items.Count > MaxItems)
            throw new ArgumentException($"持ち物は {MaxItems} 個までです。", nameof(items));
        for (int i = 0; i < MaxItems; i++)
        {
            if (i < items.Count)
                SetItem(i, items[i]);
            else
                Raw.AsSpan(OffItems + i * 8, 8).Clear();
        }
        ItemCount = items.Count;
    }

    public IReadOnlyList<Possession> GetItems() =>
        Enumerable.Range(0, Math.Clamp(ItemCount, 0, MaxItems)).Select(GetItem).ToList();

    /// <summary>最高到達レベル(MAXLEVAC)。</summary>
    public int MaxLevel { get => Word(OffMaxLevel); set => SetWord(OffMaxLevel, value); }
    public int Level { get => Word(OffLevel); set => SetWord(OffLevel, value); }
    public int Hp { get => Word(OffHp); set => SetWord(OffHp, value); }
    public int HpMax { get => Word(OffHpMax); set => SetWord(OffHpMax, value); }

    /// <summary>アーマークラス(符号付き)。</summary>
    public int ArmorClass { get => WizBytes.S16(Raw, OffArmorClass); set => SetWord(OffArmorClass, value); }

    /// <summary>覚えている呪文のビット(SpellDef.Bit)。</summary>
    public bool KnowsSpell(int bit) => WizBytes.Bit(Raw, OffSpells, CheckIndex(bit, SpellBitCount));
    public void SetKnowsSpell(int bit, bool value) => WizBytes.SetBit(Raw, OffSpells, CheckIndex(bit, SpellBitCount), value);

    /// <summary>魔術師呪文のレベル別回数(level は 1〜7)。</summary>
    public int GetMageSpellPoints(int level) => Word(OffMageSp + (CheckIndex(level - 1, 7) * 2));
    public void SetMageSpellPoints(int level, int value) => SetWord(OffMageSp + CheckIndex(level - 1, 7) * 2, value);

    public int GetPriestSpellPoints(int level) => Word(OffPriestSp + (CheckIndex(level - 1, 7) * 2));
    public void SetPriestSpellPoints(int level, int value) => SetWord(OffPriestSp + CheckIndex(level - 1, 7) * 2, value);

    /// <summary>任意の位置の 16 ビット値(符号なし)。</summary>
    public int Word(int offset) => WizBytes.U16(Raw, offset);

    public int SignedWord(int offset) => WizBytes.S16(Raw, offset);

    public void SetWord(int offset, int value)
    {
        if (value is < short.MinValue or > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        WizBytes.SetU16(Raw, offset, value);
    }

    /// <summary>BOOLEAN を書く。すでに真なら元の値(1 や 0xFFFF)を残す。</summary>
    private void SetBool(int offset, bool value)
    {
        if (value && Word(offset) != 0)
            return;
        SetWord(offset, value ? 1 : 0);
    }

    private static int CheckIndex(int index, int count) =>
        index >= 0 && index < count ? index : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>空きスロットのテンプレートが見つからないときに使う、空のレコード。</summary>
    public static Character CreateBlank() => new(new byte[RecordSize]);

    /// <summary>新規キャラクタの初期値(レベル 1 の人間の戦士)。</summary>
    public static Character CreateNew(string name)
    {
        var c = CreateBlank();
        c.Name = name;
        c.Race = 1;
        c.Class = 0;
        c.Age = 18 * 52;
        c.Alignment = 1;
        for (int i = 0; i < 6; i++)
            c.SetAttribute(i, 10);
        c.Level = 1;
        c.MaxLevel = 1;
        c.Hp = c.HpMax = 10;
        c.ArmorClass = 10;
        c.SetWord(182, 1); // 攻撃回数
        c.SetWord(184, 2); // 素手のダメージ 2d2
        c.SetWord(186, 2);
        return c;
    }
}
