# 01. ディスクイメージと UCSD ボリューム

どの機種も、ゲームは UCSD p-System の上で動き、ディスクは UCSD のボリューム(512 バイトのブロック)になっている。
機種ごとに違うのは「イメージファイルのどこに、どの順でブロックが並んでいるか」だけで、
ボリュームの中(ディレクトリ・ファイル)は共通の形式で読める。

## 1. UCSD のボリュームとディレクトリ

- ブロックは 512 バイト。ブロック 0・1 は起動用。
- ディレクトリはブロック 2〜5(2048 バイト)。先頭 26 バイトがボリューム情報、以降 26 バイトずつファイルのエントリ。

| 位置 | ボリューム情報 | ファイルのエントリ |
|---|---|---|
| +0 | 先頭ブロック(0) | 開始ブロック |
| +2 | 終わりのブロック(6) | 終わりのブロック(このブロックは含まない) |
| +4 | 種別(0) | 種別(下位 4 ビット。2 = コード、5 = データ) |
| +6 | ボリューム名(長さ付き、7 文字まで) | ファイル名(長さ付き、15 文字まで) |
| +14 | ボリュームのブロック数 | − |
| +16 | ファイルの数 | − |

ファイルの場所は毎回このディレクトリから求める。Filer などでファイルが動いても追従できる。

## 2. 機種ごとのイメージ形式

| 機種 | 形式 | ブロック b の場所 | 確かさ |
|---|---|---|---|
| PC-98 | FDI(2HD: 77 シリンダ × 2 面 × 26 セクタ × 256 バイト) | ヘッダ(+8 の値、4096 バイト)の後、シリンダ 0(13,312 バイト)を飛ばした位置から連続: `17408 + b × 512` | 確認(ゲーム) |
| PC-DOS | 生イメージ(320KB の `WIZn.DSK`、640KB の `SAVEn.DSK`) | `b × 512` | 確認(データ) |
| MSX2 | 生イメージ 720KB(Game Disk / User Disk) | ディスク後半の `368640 + b × 512` | 確認(データ) |
| Apple II | 140KB `.dsk`(DOS 3.3 のセクタ順) | トラック `b ÷ 8` の、ProDOS 論理セクタ `2k` と `2k+1`(`k = b mod 8`)。DOS 3.3 の論理セクタへの変換表: `0,14,13,12,11,10,9,8,7,6,5,4,3,2,1,15` | 確認(データ) |
| PC-8801 | D88(2D: 40 シリンダ × 2 面 × 16 セクタ × 256 バイト。1 ファイルに 2 枚) | セクタを「トラックの順 → セクタ番号 R の順」に並べ、シリンダ 0(32 セクタ)を飛ばした位置から 2 セクタで 1 ブロック | 確認(データ) |

### 2.1 FDI のヘッダ(PC-98)

`+8` ヘッダの大きさ、`+12` データの大きさ、`+16` セクタの大きさ、`+20` セクタ数、`+24` 面数、`+28` シリンダ数(どれも 4 バイト)。
PC-98 版はブートセクタがシリンダ 0 を飛ばしてブロックを数える。

### 2.2 D88(PC-8801)

- ディスク 1 枚ごとに 0x2B0 バイトのヘッダ: `+0` ディスク名(17)、`+0x1A` 書き込み禁止(0x10)、`+0x1B` 媒体(0x00 = 2D)、
  `+0x1C` ディスクの大きさ、`+0x20` トラックの位置の表(最大 164 件)。
- 各セクタに 16 バイトの見出し(`C, H, R, N`、`+4` トラック内のセクタ数、`+14` データの大きさ)と、続いてデータ。
- 1 つのファイルにディスクが続けて入る。手元の PC-8801 版は 2 枚で、1 枚目「Duplicate_Disk」(書き込み可、キャラクタを保存する側)、
  2 枚目「Wizardry#n」(書き込み禁止のマスター)。
- PC-8801 版 #4 の D88 は、セクタの大きさがそろっていない(256 と 512)。#4 は対象外。

### 2.3 MSX2 の注意

- ボリュームはディスク後半(368640 バイト目)から始まる。ボリュームのブロック数は 800 と書かれているが、ファイルは 657 ブロック以内に収まっている。
- #1 の Game Disk には、ディスク前半(110592 バイト目)に**古いディレクトリの写し**が残っている。そこから見た `SCENARIO.DATA` は中身がでたらめ。
  正しいボリュームを選ぶには、`SCENARIO.DATA` の目次まで読めるか確かめる必要がある。
- #1 の User Disk は、呪文名の表(ブロック 178)の先頭 8 バイトが 0Dh で上書きされている(`HALITO\rM` が消えている)。
- 「Wizardry II」フォルダの User Disk は、中身が #3 だった(取り違え)。

## 3. 機種ごとのファイル構成

| 機種 | ボリューム名 | ファイル |
|---|---|---|
| PC-98 | WIZBOOT | `SYSTEM.INTERP`、`SCENARIO.DATA`、`KANA.KEYMAP`、`SYSTEM.PASCAL`(ゲーム本体)、`200.MONSTERS`、`400.MONSTERS`、`KANJI.KRN`、`KANA.KRN`、`ASCII.KRN`、`200/400.CHARSET`、`200/400.TITLE` |
| PC-DOS | WIZBOOT | `SYSTEM.PASCAL`(ゲーム本体)、`ASCII.KRN`、`SCENARIO.DATA`、`200.MONSTERS`、`HAS.CACHE`、`HAS.STROPS`、`KANA.KEYMAP`、`400/200.TITLE`、`400/200.CHARSET`、`SYSTEM.INTERP`、`ZOT`(#1)または `DERF`(#2・#3) |
| MSX2 | WIZBOOT | `DERF`、`WIZARDRY.CODE`(ゲーム本体)、`SCENARIO.DATA`、`KANA.KEYMAP`、`200.MONSTERS`、`ASCII.KRN`、`KANA.KRN`、`400.CHARSET`、`200/400.TITLE`、`200.CHARSET`、`NOFORMAT`(#2・#3 は `FILL` も) |
| PC-8801 | WIZBOOT | `SYSTEM.8801`、`WIZARDRY.CODE`(ゲーム本体)、`KANA.KEYMAP`、`SCENARIO.DATA`、`ASCII.KRN`、`KANA.KRN`、`200.MONSTERS`、`200.CHARSET`、`200.TITLE` |
| Apple II | WSV2.1(#1)、KODBTTM(#2・#3) | シナリオディスク(2 枚目): `WIZARDRY.CODE`、`SCENARIO.DATA`、`SCENARIO.MESGS` |

- Apple II 版の 1 枚目(`… 1.dsk`)は起動ディスクで、`SCENARIO.DATA` はない。
  #1 の起動ディスクは本物の p-System(`SYSTEM.PASCAL`、`SYSTEM.LIBRARY`、`SYSTEM.STARTUP` などがあり、`SYSTEM.STARTUP` がゲームを自動起動する)。
- PC-98 版は `SYSTEM.PASCAL` そのものがゲーム本体で、ブートセクタが `SYSTEM.INTERP` を読んで実行する。コマンドメニューはない。
- PC-DOS 版は `WIZDOS.COM` で DOS 上から動かす(ディスクの BIOS 呼び出しを乗っ取り、イメージファイルに読み書きを回す)。
  `WIZn.DSK` は元のディスクの写しで、`SAVEn.DSK` はそれを 640KB に写してキャラクタを入れたもの。両者のディレクトリは同じ。
- PC-DOS 版の `ZOT` は、ディスクの残りを埋めるためのファイルらしい(中に古い目次の写しなどが残っている)。

## 4. 機種とシナリオの見分け方

ファイル名やイメージの形式では決められない(D88 は PC-98 でも使われる。生イメージは PC-DOS と MSX2 がある。ファイル名の取り違えもあった)。
ディスクの中身で判断する。手元の #1〜#3 の全イメージ(21 個)で確かめた。

| 機種 | 決め手 | 決め手にならないもの |
|---|---|---|
| PC-8801 | `SYSTEM.8801` がある | − |
| PC-DOS | `HAS.STROPS` または `HAS.CACHE` がある | `DERF`(MSX2 にもある)、`SYSTEM.INTERP`(PC-98 にもある) |
| MSX2 | `NOFORMAT` がある | `DERF` |
| Apple II | `SCENARIO.MESGS` があり、`ASCII.KRN` がない | − |
| PC-98 | `SYSTEM.INTERP` と `KANJI.KRN` がある | − |

シナリオは、`SCENARIO.DATA` の目次のアイテム数(101 → #1、130 → #2、104 → #3)で判断し、シナリオ名でも確かめる。
PC-98 版・PC-8801 版・MSX2 版の #1 はシナリオ名の欄が書き換わっている(`1031B`、`1110D` など)ので、アイテム数を優先する。

## 5. 手元のイメージの一覧

WizRoster で開いた結果(`SCENARIO.DATA` の目次の値は [02-scenario-data.md](02-scenario-data.md) を参照)。

| ファイル | 判別 | 形式 | SCENARIO.DATA | RECPER2B | RECPERDK | BLOFF | アイテム | 名前の取得元(アイテム/呪文/種族など) |
|---|---|---|---|---|---|---|---|---|
| Wizardry1.FDI | PC-98 版 #1 狂王の試練場 | PC-98 FDI | ブロック 46〜159 | 6,1,6,6,13,4,2,1 | 1,10,101,24,101,20,20,1 | 0,3,23,57,65,81,91,111 | 101 件 × 78B | SCENARIO.DATA / SCENARIO.DATA / ASCII.KRN + SCENARIO.DATA + 既定 |
| Wizardry2.FDI | PC-98 版 #2 ダイヤモンドの騎士 | PC-98 FDI | ブロック 46〜108 | 6,1,10,6,22,4,1,1 | 1,6,78,24,130,20,0,1 | 0,12,24,40,48,2,60,60 | 130 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| Wizardry3.FDI | PC-98 版 #3 リルガミンの遺産 | PC-98 FDI | ブロック 46〜105 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,1,13,29,37,47,57,57 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| SAVE1.DSK | PC-DOS 版 #1 狂王の試練場 | 生イメージ | ブロック 234〜308 | 6,1,10,6,22,4,1,1 | 1,10,101,24,101,20,0,1 | 0,12,32,54,62,2,72,72 | 101 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| SAVE2.DSK | PC-DOS 版 #2 ダイヤモンドの騎士 | 生イメージ | ブロック 231〜293 | 6,1,10,6,22,4,1,1 | 1,6,78,24,130,20,0,1 | 0,12,24,40,48,2,60,60 | 130 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| SAVE3.DSK | PC-DOS 版 #3 リルガミンの遺産 | 生イメージ | ブロック 237〜297 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,12,24,40,48,2,58,58 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| WIZ1.DSK | PC-DOS 版 #1 狂王の試練場 | 生イメージ | ブロック 234〜308 | 6,1,10,6,22,4,1,1 | 1,10,101,24,101,20,0,1 | 0,12,32,54,62,2,72,72 | 101 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| WIZ2.DSK | PC-DOS 版 #2 ダイヤモンドの騎士 | 生イメージ | ブロック 231〜293 | 6,1,10,6,22,4,1,1 | 1,6,78,24,130,20,0,1 | 0,12,24,40,48,2,60,60 | 130 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| WIZ3.DSK | PC-DOS 版 #3 リルガミンの遺産 | 生イメージ | ブロック 237〜297 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,12,24,40,48,2,58,58 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| APL2_Wozardry I 2.dsk | Apple II 版 #1 狂王の試練場 | Apple II (DOS 3.3 順 .dsk) | ブロック 149〜265 | 6,1,6,6,13,4,2,1 | 1,10,101,24,101,20,20,1 | 0,6,26,60,68,84,94,114 | 101 件 × 78B | SCENARIO.DATA / SCENARIO.DATA / SCENARIO.DATA |
| APL2_Wozardry II 2.dsk | Apple II 版 #2 ダイヤモンドの騎士 | Apple II (DOS 3.3 順 .dsk) | ブロック 148〜262 | 6,1,6,6,13,4,2,1 | 1,6,78,24,130,20,30,1 | 0,6,18,44,52,72,82,112 | 130 件 × 78B | SCENARIO.DATA / SCENARIO.DATA / SCENARIO.DATA |
| APL2_Wozardry III 2.dsk | Apple II 版 #3 リルガミンの遺産 | Apple II (DOS 3.3 順 .dsk) | ブロック 160〜267 | 6,1,6,6,13,4,2,1 | 1,6,78,24,104,20,30,1 | 0,55,3,97,29,45,67,105 | 104 件 × 78B | SCENARIO.DATA / SCENARIO.DATA / SCENARIO.DATA |
| PC88_Wizardry I - Proving Grounds of the Mad Overlord.D88 | PC-8801 版 #1 狂王の試練場 | D88 ディスク 1「Duplicate_Disk」(2D, 先頭セクタ 32) | ブロック 207〜320 | 6,1,6,6,13,4,2,1 | 1,10,101,24,101,20,20,1 | 0,3,23,57,65,81,91,111 | 101 件 × 78B | SCENARIO.DATA / ASCII.KRN / ASCII.KRN + SCENARIO.DATA + 既定 |
| PC88_Wizardry II - Knight of Diamonds.D88 | PC-8801 版 #2 ダイヤモンドの騎士 | D88 ディスク 1「Duplicate_Disk」(2D, 先頭セクタ 32) | ブロック 208〜270 | 6,1,10,6,22,4,1,1 | 1,6,78,24,130,20,0,1 | 0,12,24,40,48,2,60,60 | 130 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| PC88_Wizardry III - Legacy of Llylgamyn.D88 | PC-8801 版 #3 リルガミンの遺産 | D88 ディスク 1「Duplicate_Disk」(2D, 先頭セクタ 32) | ブロック 221〜280 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,1,13,29,37,47,57,57 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| MSX_Wizardry I - Proving Grounds of the Mad Overlord(Game Disk).dsk | MSX2 版 #1 狂王の試練場 | 生イメージ (先頭 368640 バイト) | ブロック 177〜290 | 6,1,6,6,13,4,2,1 | 1,10,101,24,101,20,20,1 | 0,3,23,57,65,81,91,111 | 101 件 × 78B | SCENARIO.DATA / SCENARIO.DATA / ASCII.KRN + SCENARIO.DATA + 既定 |
| MSX_Wizardry I - Proving Grounds of the Mad Overlord(User Disk).dsk | MSX2 版 #1 狂王の試練場 | 生イメージ (先頭 368640 バイト) | ブロック 177〜290 | 6,1,6,6,13,4,2,1 | 1,10,101,24,101,20,20,1 | 0,3,23,57,65,81,91,111 | 101 件 × 78B | SCENARIO.DATA / ASCII.KRN / ASCII.KRN + SCENARIO.DATA + 既定 |
| MSX_Wizardry II - Knight of Diamonds(Game Disk).dsk | MSX2 版 #2 ダイヤモンドの騎士 | 生イメージ (先頭 368640 バイト) | ブロック 180〜242 | 6,1,10,6,22,4,1,1 | 1,6,78,24,130,20,0,1 | 0,12,24,40,48,2,60,60 | 130 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| MSX_Wizardry II - Knight of Diamonds(User Disk).dsk | MSX2 版 #3 リルガミンの遺産 | 生イメージ (先頭 368640 バイト) | ブロック 193〜252 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,1,13,29,37,47,57,57 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| MSX_Wizardry III - Legacy of Llylgamyn(Game Disk).dsk | MSX2 版 #3 リルガミンの遺産 | 生イメージ (先頭 368640 バイト) | ブロック 193〜252 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,1,13,29,37,47,57,57 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
| MSX_Wizardry III - Legacy of Llylgamyn(User Disk).dsk | MSX2 版 #3 リルガミンの遺産 | 生イメージ (先頭 368640 バイト) | ブロック 193〜252 | 6,1,10,6,22,4,1,1 | 1,6,78,24,104,20,0,1 | 0,1,13,29,37,47,57,57 | 104 件 × 46B | ASCII.KRN / ASCII.KRN / ASCII.KRN + SCENARIO.DATA |
