"""Wizardry(UCSD p-System 版)のゲーム本体を調べるコマンド(調査用)。

使い方(Python 3.10 以降。WizRoster のリポジトリの外のイメージを読むだけで、書き込みはしない):

  python -I tools/pcode/wizpcode.py segments <イメージ>
      セグメントと手続きの数の一覧
  python -I tools/pcode/wizpcode.py disasm <イメージ> <セグメント名> <手続き番号>
      手続きを逆アセンブルする
  python -I tools/pcode/wizpcode.py saves <イメージ>
      セービングスロー(キャラクタ +48)を読み書きする箇所、汎用の判定の呼び出し元、職業・種族の補正の表
  python -I tools/pcode/wizpcode.py field <イメージ> <ワード位置>
      キャラクタのレコードの <ワード位置>(例: 100 = +200)を INC / IND で読み書きする箇所

対応: Apple II の DOS 3.3 順 .dsk(シナリオディスク)、PC-98 の FDI、生イメージ。D88 は未対応。
"""
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import disasm  # noqa: E402
import ucsd  # noqa: E402


def all_lines(code: bytes):
    result = {}
    for _, name, seg in ucsd.segments(code):
        try:
            procs = ucsd.procedures(seg)
        except Exception:
            continue
        result[name] = (seg, {p: disasm.disassemble(seg, e, x + 2) for p, (e, x, a) in procs.items()})
    return result


def strings_of(lines):
    return [t[4:] for _, t in lines if t.startswith('LSA')]


def cmd_segments(code):
    for i, name, seg in ucsd.segments(code):
        try:
            n = len(ucsd.procedures(seg))
        except Exception:
            n = '?'
        print(f'{i:2} {name:8} {len(seg):6} バイト  手続き {n}')


def cmd_disasm(code, segname, proc):
    seg, procs = all_lines(code)[segname]
    for pc, t in procs[int(proc)]:
        print(f'{pc:6} {t}')


def cmd_field(code, word):
    word = int(word)
    for name, (seg, procs) in all_lines(code).items():
        for p, lines in procs.items():
            for k, (pc, t) in enumerate(lines):
                if t in (f'INC {word}', f'IND {word}'):
                    ctx = ' ; '.join(t2 for _, t2 in lines[max(0, k - 4):k + 10])
                    print(f'{name:8} p{p:2} @{pc:5} {strings_of(lines)[:3]}\n    {ctx}')


def cmd_saves(code):
    procs_by_seg = all_lines(code)
    generic = []
    print('-- セービングスロー(+48 = 24 ワード目、5 ビット×3/ワード)を読み書きする箇所')
    for name, (seg, procs) in procs_by_seg.items():
        for p, lines in procs.items():
            for i, (pc, t) in enumerate(lines):
                if t != 'IXP 3 5' or i < 2:
                    continue
                base, idx = lines[i - 2][1], lines[i - 1][1]
                if base not in ('INC 24', 'LDA 25'):
                    continue
                nxt = lines[i + 1][1]
                print(f'  {name:8} p{p:2} @{pc:5} {base:7} 番号={idx:8} -> {nxt:6} {strings_of(lines)[:3]}')
                if idx.startswith(('SLDL', 'LDL')) and nxt == 'LDP' and name != 'UTILITIE':
                    generic.append((name, p))

    for name, p in generic:
        print(f'-- 汎用の判定 {name} p{p} の呼び出し元 [効果, 判定に使う番号, 抵抗ビット] 文字列')
        for q, lines in procs_by_seg[name][1].items():
            for i, (pc, t) in enumerate(lines):
                if t in (f'CLP {p}', f'CIP {p}'):
                    ctx = [t2 for _, t2 in lines[max(0, i - 6):i]]
                    consts = [c.split()[1] for c in ctx if c.startswith('SLDC')][-3:]
                    msg = next((c[4:] for c in reversed(ctx) if c.startswith('LSA')), '(メッセージ番号)')
                    print(f'     {consts} {msg}')

    seg, util = procs_by_seg['UTILITIE']
    # セービングスローを 1 つ読んで書き戻す手続き(+48 を参照し、IXP 3 5 が 2 回、STP がある)
    writer = next(p for p, lines in util.items()
                  if (txt := [t for _, t in lines]).count('IXP 3 5') == 2 and 'STP' in txt and 'INC 24' in txt)
    print(f'-- 補正の表(UTILITIE p{writer}(番号, 引く値) を呼ぶ分岐。最初の分岐が職業 0〜7、次が種族 1〜5)')
    for p, lines in util.items():
        if not any(t == f'CLP {writer}' for _, t in lines):
            continue
        for pc, t in lines:
            if not t.startswith('XJP'):
                continue
            q = pc + 1
            q += q % 2
            lo, hi = struct.unpack_from('<hh', seg, q)
            for k in range(hi - lo + 1):
                entry = q + 6 + 2 * k
                target = entry - struct.unpack_from('<H', seg, entry)[0]
                calls, prev = [], []
                for pc2, t2 in lines:
                    if pc2 < target:
                        continue
                    if t2.startswith('UJP') and pc2 > target:
                        break
                    if t2 == f'CLP {writer}':
                        calls.append(tuple(int(s.split()[1]) for s in prev[-2:]))
                    prev.append(t2)
                print(f'   case {lo + k}: {calls}')


def main(argv):
    if len(argv) < 3:
        print(__doc__)
        return 1
    command, image = argv[1], argv[2]
    code = ucsd.game_code(ucsd.load_volume(image))
    if command == 'segments':
        cmd_segments(code)
    elif command == 'disasm':
        cmd_disasm(code, argv[3], argv[4])
    elif command == 'saves':
        cmd_saves(code)
    elif command == 'field':
        cmd_field(code, argv[3])
    else:
        print(__doc__)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
