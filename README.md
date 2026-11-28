# WizRoster

UCSD p-System 版 Wizardry #1〜#3 のキャラクタ編集ツール(Windows / C# WinForms / .NET 10)。

ディスクイメージの中の `SCENARIO.DATA` から、キャラクタ 20 人分と、アイテム・呪文などのマスタデータを読み、編集して書き戻す。
ファイルの場所は p-System のディレクトリから、データの位置は `SCENARIO.DATA` の目次から求めるので、機種ごとの固定の位置は持たない。

## 対応するディスク

| 機種 | イメージ | シナリオ |
|---|---|---|
| PC-98 | FDI | #1 狂王の試練場 / #2 ダイヤモンドの騎士 / #3 リルガミンの遺産 |
| PC-DOS | 生イメージ | 同上 |
| MSX2 | 720KB 生イメージ | 同上 |
| Apple II | 140KB `.dsk`(DOS 3.3 順) | 同上 |
| PC-8801 | D88 | 同上 |

## 主な機能

- 20 人分をタブで切り替えて編集(基本の値、能力値、セービングスロー、毒、持ち物、呪文、そのほかの項目の生の値)
- アイテム・呪文・種族などの名前は、開いたディスクから読む
- ゲームのルールに合わない点を警告(職業の条件、装備できない職業、経験値とレベルの関係など)
- スロット間のコピー・入れ替え、1 人分の書き出し・読み込み(`.wchr` / `.json`)
- ボルタック商店の在庫数の編集
- 保存の前に元のファイルを `.bak_日時` として残す

## 使い方

```
dotnet run --project src/WizRoster -- <イメージのパス>
```

ファイルはメニュー・ドラッグ&ドロップでも開ける。エミュレータがイメージを開いている間は保存しないこと。
単体の exe の作り方は [docs/spec/02-design.md](docs/spec/02-design.md) を参照。

## ドキュメント

- [docs/README.md](docs/README.md): 目次
- ディスクの解析結果: [docs/analysis/](docs/analysis/)(イメージの形式、`SCENARIO.DATA`、キャラクタ・アイテムのレコード、`ASCII.KRN`、ゲーム本体の解析)
- プログラムの仕様: [docs/spec/](docs/spec/)(画面と機能、構成とつくり)

## 構成

- `src/WizRoster.Core`: ディスクイメージ・シナリオ・キャラクタの読み書き(画面に依存しない)
- `src/WizRoster`: WinForms の画面
- `tests/WizRoster.Tests`: xUnit のテスト(手元の実イメージがあれば使う)
- `tools/pcode`: ゲーム本体の p-code を調べる Python スクリプト
