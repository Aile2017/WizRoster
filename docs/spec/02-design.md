# 02. 構成とつくり

## 1. プロジェクト

| プロジェクト | 内容 |
|---|---|
| `src/WizRoster.Core`(.NET 10 クラスライブラリ) | ディスクイメージ・UCSD ディレクトリ・`ASCII.KRN`・`SCENARIO.DATA`・キャラクタの読み書き。画面に依存しない |
| `src/WizRoster`(.NET 10 WinForms) | 画面 |
| `tests/WizRoster.Tests`(xUnit) | テスト |
| `tools/pcode/`(Python) | ゲーム本体の p-code を調べるスクリプト(アプリとは独立) |

## 2. 主なクラス(WizRoster.Core)

| 名前空間 | クラス | 役割 |
|---|---|---|
| `Disk` | `IBlockImage` | 512 バイトのブロックの読み書き。書き込み禁止、ディスクの番号・名前(D88) |
| | `LinearImage` | ブロックが連続しているイメージ(FDI、生イメージ、MSX2 など)。先頭の位置を持つ |
| | `AppleDosOrderImage` | Apple II の DOS 3.3 順 `.dsk`(セクタの並べ替え) |
| | `D88Disk`、`D88Image` | D88 の解析と、ディスク 1 枚のブロックの読み書き |
| | `ImageLoader` | 形式を判別し、`SCENARIO.DATA` を含むボリュームの候補を返す |
| `Ucsd` | `UcsdDirectory` | ディレクトリ(ブロック 2〜5) |
| `Krn` | `KrnMessageFile` | `ASCII.KRN` の索引と復号、略記の展開 |
| `Scenario` | `ScenarioFile` | 目次、レコードの位置、アイテム・呪文名・経験値の表・名前の読み込み、キャラクタの読み書き |
| | `NameTables` | 種族・職業・状態・属性の名前(`ASCII.KRN` → 目次 → 既定) |
| `Model` | `Character` | 208 バイトのレコード。値は `Raw` に直接読み書きする(不明な項目もそのまま残る) |
| | `ItemDef`、`SpellDef` | アイテム・呪文の定義 |
| | `ExperienceTable` | 経験値の表 |
| | `CharacterRules` | 警告の判定 |
| | `ValueRanges` | 入力の範囲(画面・JSON・警告で共通) |
| | `GameNames`、`RawFields`、`WizBytes` | 既定の名前・説明、「その他」欄の項目、データ型の読み書き |
| `IO` | `CharacterFile` | `.wchr` / `.json` の書き出しと読み込み(JSON の範囲チェック) |
| (ルート) | `RosterDocument` | 開いているイメージ 1 つ分の編集状態(20 人の作業用コピー、変更の有無、保存) |
| | `GameInfo` | 機種とシナリオの判別 |

画面(WizRoster): `MainForm`(メニュー・タブ・ステータスバー)、`CharacterEditor`(1 人分の編集。20 個のタブで 1 つを使い回す)、
`ItemListEditor`(持ち物の一覧表)、`SlotPickerDialog`、`UiHelpers`。

## 3. 開くときの流れ

1. `ImageLoader.OpenCandidates` が、ファイルの形式から読み方の候補を作る。
   - D88 なら、ディスクごとに D88 の候補だけを作る(連続したイメージとして扱うとセクタの見出しを壊すため)。
   - FDI ならヘッダとシリンダ 0 を飛ばした位置、140KB なら Apple II の DOS 3.3 順、それから先頭 0 の生イメージ。
   - 最後に、ファイル名 `SCENARIO.DATA` を探してディレクトリの位置から先頭を逆算した候補(MSX2 など)。
2. 各候補でディレクトリを読み、`SCENARIO.DATA` があるものを返す。
3. `RosterDocument.Open` が、候補ごとに `ScenarioFile` を作り、目次まで正しく読めた最初のものを採用する(古いディレクトリの写しを避けるため)。
   D88 はディスクごとに 1 つ採用し、キャラクタの入っているディスクを選ぶ。
4. `ScenarioFile` が、目次 → アイテム(78 / 46 バイト。名前がなければ `ASCII.KRN`)→ 呪文名(表を探す。壊れていれば `ASCII.KRN`)→ 経験値の表 → 名前、の順に読む。
5. `GameInfo.Identify` が、ディレクトリのファイル構成とアイテム数から機種とシナリオを決める。

## 4. 保存の流れ

1. 書き込み禁止のディスクなら止める。
2. 20 人分のレコードを、イメージのバイト列の該当ブロックに書き込む(`IBlockImage.WriteBlock` 経由。Apple II・D88 はセクタ単位に分けて書く)。
3. 保存先が既にあれば `<名前>.bak_日時` にコピーする。
4. `<名前>.tmp` に書いてから、保存先に置き換える。

## 5. テスト

```
dotnet test
```

手元の実イメージがあればそれを使い、なければ何もせずに通す(既定の場所はテストのコードに書いてある)。主なイメージの場所は環境変数で変えられる。

| 環境変数 | 指すもの |
|---|---|
| `WIZROSTER_PC98` | PC-98 版 #1 の FDI |
| `WIZROSTER_APPLE2` | Apple II 版 #1 のシナリオディスク(2 枚目の `.dsk`) |
| `WIZROSTER_PCDOS` | PC-DOS 版 #1 の保存ディスク(`SAVE1.DSK`) |
| `WIZROSTER_MSX` | MSX2 版 #1 の Game Disk・User Disk のあるフォルダ |
| `WIZROSTER_PC88` | PC-8801 版 #1 の D88 |

機種・シナリオの判別、#2・#3、名前、経験値、セービングスローのテストは、テストのコードに書いた場所の #1〜#3 のイメージを直接使う(読み込みだけ)。
保存のテストは、イメージを一時フォルダにコピーしてから行う。

## 6. ビルドと配布

```
dotnet build
dotnet run --project src/WizRoster -- <イメージ>
```

単体の exe を作る(出力先の `publish/` は `.gitignore` で除外):

```
# .NET ランタイム込み(約 49 MB。どの Windows でも動く)
dotnet publish src/WizRoster -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o publish\standalone

# ランタイム別(約 0.3 MB。.NET 10 デスクトップランタイムが必要)
dotnet publish src/WizRoster -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o publish\runtime-required
```

環境変数 `Platform=x64` が設定されている環境では、`dotnet build` の出力が `bin\x64\Debug\…` と `bin\Debug\…` に分かれることがある。
