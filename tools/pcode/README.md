# tools/pcode

Wizardry(UCSD p-System 版)のゲーム本体(p-code)を調べるための Python スクリプト。
セービングスローや毒の扱いは、これで逆アセンブルして確かめた(`docs/analysis/06-game-program.md`)。

- `ucsd.py`: ディスクイメージからボリュームとコードファイルを取り出す。セグメントと手続きの一覧
- `disasm.py`: UCSD p-code(II.1 / Apple Pascal 1.1 系)の簡易逆アセンブラ
- `wizpcode.py`: コマンド
- `d6502.py`: Apple II 版の機械語の手続き(6502)の簡易逆アセンブラ。戦闘の絵(#3)を描く文字描画などを読むのに使った

```
python -I tools/pcode/wizpcode.py segments <イメージ>
python -I tools/pcode/wizpcode.py disasm   <イメージ> <セグメント名> <手続き番号>
python -I tools/pcode/wizpcode.py saves    <イメージ>
python -I tools/pcode/wizpcode.py field    <イメージ> <ワード位置>
python -I tools/pcode/d6502.py   <イメージ> <セグメント名> <開始バイト> <終了バイト>
```

- PC-DOS 版の画面ドライバ(`SYSTEM.INTERP`、8086)は `ndisasm -b16` で読んだ(`docs/analysis/08-monsters.md` 3.5)。
- 読み込みだけで、イメージには書き込まない。
- 対応: Apple II の DOS 3.3 順 `.dsk`(シナリオディスク)、PC-98 の FDI、生イメージ(PC-DOS、MSX2)。D88 は未対応。
- アスキー移植版(PC-98 など)は文字列を `ASCII.KRN` のメッセージ番号で出すので、逆アセンブルに文字列は出ない。
