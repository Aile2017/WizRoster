"""UCSD p-System のディスクイメージとコードファイルを読む(調査用)。

- ディスクイメージ: Apple II の DOS 3.3 順 .dsk、PC-98 の FDI、生イメージ(先頭 0 / MSX2 の 368640 バイト目)。
  D88 には対応していない(WizRoster 本体の D88Disk を使うこと)。
- コードファイル(II 系 / Apple Pascal): ブロック 0 がセグメント辞書。
  +0 から 16 件の (開始ブロック, 長さ[バイト])、+64 から 16 件の名前(8 文字)。
- セグメントの末尾: 最後のワードが (セグメント番号, 手続きの数)。その前に、各手続きの属性表への
  自己相対ポインタが並ぶ(手続き 1 が最後のワードの直前)。属性表: (手続き番号, レキシカルレベル)、
  その前のワードが入口、さらに前が出口(どちらも自己相対)。
"""
import struct

BLOCK = 512
APPLE_DSK_SIZE = 35 * 16 * 256
_PRODOS_TO_DOS = [0, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 15]


def _apple_linear(d: bytes) -> bytes:
    out = bytearray()
    for b in range(280):
        t, k = divmod(b, 8)
        for s in (2 * k, 2 * k + 1):
            o = (t * 16 + _PRODOS_TO_DOS[s]) * 256
            out += d[o:o + 256]
    return bytes(out)


def _directory(volume: bytes):
    """ブロック 2 のディレクトリ。正しく読めなければ None。"""
    o = 2 * BLOCK
    if len(volume) < o + 2048:
        return None
    first, last = struct.unpack_from('<HH', volume, o)
    nlen = volume[o + 6]
    n = struct.unpack_from('<H', volume, o + 16)[0]
    if first != 0 or last != 6 or not 1 <= nlen <= 7 or n > 77:
        return None
    files = {}
    for i in range(1, n + 1):
        e = o + 26 * i
        start, end = struct.unpack_from('<HH', volume, e)
        ln = volume[e + 6]
        if not 1 <= ln <= 15:
            return None
        files[volume[e + 7:e + 7 + ln].decode('latin1')] = (start, end)
    return files


def load_volume(path: str) -> bytes:
    """イメージを、ブロック 0 から順に並んだボリュームとして返す。"""
    d = open(path, 'rb').read()
    candidates = []
    if len(d) == APPLE_DSK_SIZE:
        candidates.append(_apple_linear(d))
    if len(d) >= 32:
        hdr, size, secsize, secs, heads, cyls = struct.unpack_from('<6I', d, 8)
        if 32 <= hdr <= 65536 and hdr + size == len(d) and secsize * secs * heads * cyls == size:
            candidates.append(d[hdr + secsize * secs * heads:])  # PC-98: シリンダ 0 を飛ばす
            candidates.append(d[hdr:])
    candidates.append(d)
    candidates.append(d[368640:])  # MSX2: ボリュームはディスク後半
    for v in candidates:
        files = _directory(v)
        if files and ('SCENARIO.DATA' in files):
            return v
    raise ValueError('UCSD ボリュームが見つかりません(D88 は未対応)')


def read_file(volume: bytes, name: str) -> bytes:
    files = _directory(volume)
    start, end = files[name]
    return volume[start * BLOCK:end * BLOCK]


def game_code(volume: bytes) -> bytes:
    """ゲーム本体のコードファイル(Apple II は WIZARDRY.CODE、アスキー移植版は SYSTEM.PASCAL)。"""
    files = _directory(volume)
    for name in ('WIZARDRY.CODE', 'SYSTEM.PASCAL'):
        if name in files:
            return read_file(volume, name)
    raise KeyError('ゲーム本体のコードファイルがありません')


def segments(code: bytes):
    """(辞書の番号, 名前, セグメントのバイト列) の一覧。"""
    result = []
    for i in range(16):
        blk, length = struct.unpack_from('<HH', code, i * 4)
        name = code[64 + i * 8:64 + i * 8 + 8].decode('latin1').rstrip()
        if length:
            result.append((i, name, code[blk * BLOCK:blk * BLOCK + length]))
    return result


def procedures(seg: bytes):
    """{手続き番号: (入口, 出口, 属性表の位置)}。"""
    last = len(seg) - 2
    nprocs = seg[last + 1]
    procs = {}
    for p in range(1, nprocs + 1):
        ptr = last - 2 * p
        rel = struct.unpack_from('<H', seg, ptr)[0]
        attr = ptr - rel
        if rel == 0 or attr < 4:
            continue
        enter = attr - 2 - struct.unpack_from('<H', seg, attr - 2)[0]
        exit_ = attr - 4 - struct.unpack_from('<H', seg, attr - 4)[0]
        procs[p] = (enter, exit_, attr)
    return procs
