"""UCSD p-code(II.1 / Apple Pascal 1.1 系)の簡易逆アセンブラ(調査用)。

オペランドの種類:
  ub = 符号なし 1 バイト、sb = 符号付き 1 バイト(ジャンプ)、
  b  = 1 バイト(<128)または 2 バイト(先頭の最上位ビットが立つ: ((b0&0x7f)<<8)|b1)、
  db = 読み捨て 1 バイト、w = 2 バイト(リトルエンディアン)
"""
import struct

NO_OPERAND = {
    128: 'ABI', 129: 'ABR', 130: 'ADI', 131: 'ADR', 132: 'LAND', 133: 'DIF', 134: 'DVI', 135: 'DVR',
    136: 'CHK', 137: 'FLO', 138: 'FLT', 139: 'INN', 140: 'INT', 141: 'LOR', 142: 'MODI', 143: 'MPI',
    144: 'MPR', 145: 'NGI', 146: 'NGR', 147: 'LNOT', 148: 'SRS', 149: 'SBI', 150: 'SBR', 151: 'SGS',
    152: 'SQI', 153: 'SQR', 154: 'STO', 155: 'IXS', 156: 'UNI', 157: 'S2P', 159: 'LDCN',
    186: 'LDP', 187: 'STP', 190: 'LDB', 191: 'STB', 195: 'EQUI', 196: 'GEQI', 197: 'GRTI',
    200: 'LEQI', 201: 'LESI', 203: 'NEQI', 214: 'XIT', 215: 'NOP',
}
OPS = {  # 名前, オペランドの並び
    158: ('CSP', 'ub'), 160: ('ADJ', 'ub'), 161: ('FJP', 'sb'), 162: ('INC', 'b'), 163: ('IND', 'b'),
    164: ('IXA', 'b'), 165: ('LAO', 'b'), 167: ('LAE', 'ub b'), 168: ('MOV', 'b'), 169: ('LDO', 'b'),
    170: ('SAS', 'ub'), 171: ('SRO', 'b'), 173: ('RNP', 'ub'), 174: ('CIP', 'ub'),
    178: ('LDA', 'db b'), 182: ('LOD', 'db b'), 184: ('STR', 'db b'), 185: ('UJP', 'sb'),
    188: ('LDM', 'ub'), 189: ('STM', 'ub'), 192: ('IXP', 'ub ub'), 193: ('RBP', 'ub'), 194: ('CBP', 'ub'),
    198: ('LLA', 'b'), 199: ('LDCI', 'w'), 202: ('LDL', 'b'), 204: ('STL', 'b'), 205: ('CXP', 'ub ub'),
    206: ('CLP', 'ub'), 207: ('CGP', 'ub'), 209: ('STE', 'ub b'), 211: ('EFJ', 'sb'), 212: ('NFJ', 'sb'),
    213: ('BPT', 'b'),
}
COMPARE = {175: 'EQU', 176: 'GEQ', 177: 'GRT', 180: 'LEQ', 181: 'LES', 183: 'NEQ'}


def _b(code, pc):
    b0 = code[pc]
    if b0 & 0x80:
        return ((b0 & 0x7F) << 8) | code[pc + 1], pc + 2
    return b0, pc + 1


def decode(code: bytes, pc: int):
    """(次の pc, 文字列) を返す。"""
    op = code[pc]
    start = pc
    pc += 1
    if op < 128:
        return pc, f'SLDC {op}'
    if 216 <= op <= 231:
        return pc, f'SLDL {op - 215}'
    if 232 <= op <= 247:
        return pc, f'SLDO {op - 231}'
    if op >= 248:
        return pc, f'SIND {op - 248}'
    if op in NO_OPERAND:
        return pc, NO_OPERAND[op]
    if op in COMPARE:
        kind = code[pc]; pc += 1
        text = f'{COMPARE[op]} type{kind}'
        if kind in (10, 12):
            size, pc = _b(code, pc)
            text += f' size{size}'
        return pc, text
    if op == 166:  # LSA: 文字列定数
        n = code[pc]
        s = code[pc + 1:pc + 1 + n].decode('latin1')
        return pc + 1 + n, f'LSA {s!r}'
    if op == 179:  # LDC: ワード境界に揃えた定数
        n = code[pc]; pc += 1
        if pc % 2:
            pc += 1
        words = struct.unpack_from(f'<{n}H', code, pc)
        return pc + 2 * n, f'LDC {list(words)}'
    if op == 208:  # LPA
        n = code[pc]
        return pc + 1 + n, f'LPA {code[pc + 1:pc + 1 + n].hex()}'
    if op == 172:  # XJP: ワード境界に揃えて min, max, UJP, 表
        if pc % 2:
            pc += 1
        lo, hi = struct.unpack_from('<hh', code, pc)
        pc += 4
        pc += 2  # UJP sb(表の範囲外)
        pc += 2 * (hi - lo + 1)
        return pc, f'XJP {lo}..{hi}'
    if op in OPS:
        name, spec = OPS[op]
        args = []
        for kind in spec.split():
            if kind == 'ub':
                args.append(code[pc]); pc += 1
            elif kind == 'db':
                pc += 1
            elif kind == 'sb':
                v = code[pc]; pc += 1
                v = v - 256 if v >= 128 else v
                args.append(f'{v:+d}' + (f'->{pc + v}' if v >= 0 else '(jtab)'))
            elif kind == 'b':
                v, pc = _b(code, pc); args.append(v)
            elif kind == 'w':
                args.append(struct.unpack_from('<h', code, pc)[0]); pc += 2
        return pc, f'{name} ' + ' '.join(str(a) for a in args)
    return pc, f'??? {op}'


def disassemble(code: bytes, start: int, end: int):
    pc = start
    out = []
    while pc < end:
        try:
            nxt, text = decode(code, pc)
        except Exception as e:  # 範囲外など
            out.append((pc, f'<error {e}>'))
            break
        out.append((pc, text))
        pc = nxt
    return out
