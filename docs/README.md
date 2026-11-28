# WizRoster ドキュメント

## ディスクの解析結果(`analysis/`)

UCSD p-System 版 Wizardry #1〜#3(PC-98・PC-DOS・MSX2・Apple II・PC-8801)のディスクイメージを解析した結果。

| 文書 | 内容 |
|---|---|
| [01-disk-images.md](analysis/01-disk-images.md) | イメージの形式(FDI・生イメージ・Apple II・D88)、UCSD のボリュームとディレクトリ、機種ごとのファイル構成、機種とシナリオの見分け方 |
| [02-scenario-data.md](analysis/02-scenario-data.md) | `SCENARIO.DATA` の目次、データの配置、呪文名の表、経験値の表、種族・職業などの名前、#2・#3 の違い |
| [03-character-record.md](analysis/03-character-record.md) | キャラクタのレコード(208 バイト)の全項目 |
| [04-items-and-spells.md](analysis/04-items-and-spells.md) | アイテムのレコード(78 / 46 バイト)、#1 のアイテムと呪文の一覧、機種間の比較 |
| [05-ascii-krn.md](analysis/05-ascii-krn.md) | アスキー移植版のメッセージファイル `ASCII.KRN` の構造と暗号 |
| [06-game-program.md](analysis/06-game-program.md) | ゲーム本体(p-code)の解析: セービングスロー、毒、迷宮への置き去り、呪文の回数、レベルアップの HP、戦闘の能力、ボルタック商店の在庫など |
| [07-verification.md](analysis/07-verification.md) | ゲームで確かめたこと、テストで確かめたこと、まだ分かっていないこと |

確かさの表し方(各文書で共通):

- **確認(ゲーム)**: 書き換えて、ゲームで動作を確かめた
- **確認(プログラム)**: ゲーム本体の p-code を逆アセンブルして確かめた
- **確認(データ)**: 複数のディスク・キャラクタの値を突き合わせて確かめた
- **推測**: 値の傾向や Wizardry の一般的な知識からの推測
- **不明**: 意味が分からない

## プログラムの仕様(`spec/`)

| 文書 | 内容 |
|---|---|
| [01-features.md](spec/01-features.md) | 画面と機能、入力チェック、警告、保存、書き出し・読み込みの形式 |
| [02-design.md](spec/02-design.md) | 構成(プロジェクト・主なクラス)、処理の流れ、テスト、ビルドと配布 |

## 調査用のスクリプト

`tools/pcode/`: ゲーム本体の p-code を逆アセンブルする Python スクリプト([説明](../tools/pcode/README.md))。
