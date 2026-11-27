# 04. アイテムと呪文

## 1. アイテムのレコード(78 バイト)

Wizardry のソースの型名は TOBJREC。名前なしの版(46 バイト)は、先頭の名前 32 バイトがないだけで、残りの並びは同じ。

| 位置 | 46 バイト版 | 名前 | 内容 | 確かさ |
|---:|---:|---|---|---|
| +0 | − | NAME | 名前 STRING[15] | 確認(データ) |
| +16 | − | NAMEUNK | 鑑定前の名前 STRING[15] | 確認(データ) |
| +32 | +0 | OBJTYPE | 種類: 0 武器、1 鎧、2 盾、3 兜、4 小手、5 特殊(巻物・薬など)、6 その他(指輪・護符など) | 確認(データ) |
| +34 | +2 | ALIGN | 属性(0 = なし、1〜3 = 専用の属性。違う属性で装備すると呪われる) | 確認(データ) |
| +36 | +4 | CURSED | 呪われたアイテム(BOOLEAN) | 確認(データ) |
| +38 | +6 | SPECIAL | 特殊な力 | 推測 |
| +40 | +8 | CHANGETO | 使うと変わる先のアイテム | 推測 |
| +42 | +10 | CHGCHANC | 変わる確率(%) | 推測 |
| +44 | +12 | PRICE | 価格 TWIZLONG | 確認(データ) |
| +50 | +18 | BOLTACXX | ボルタック商店の在庫数(−1 = 無限) | 確認(データ。機種間で違う唯一の値) |
| +52 | +20 | SPELLPWR | 使うと唱える呪文の番号(キャラクタの呪文のビットと同じ番号。例: ROD OF FLAME = 7 = MAHALITO) | 確認(データ) |
| +54 | +22 | CLASSUSE | 装備できる職業のビット列(ビット n = 職業 n) | 確認(データ) |
| +56 | +24 | HEALPTS | 装備しているときの HP 回復量 | 推測 |
| +58 | +26 | WEPVSTY2 | 防御の対象のビット列 | 推測 |
| +60 | +28 | WEPVSTY3 | 抵抗のビット列 | 推測 |
| +62 | +30 | ARMORMOD | AC の修正 | 確認(データ) |
| +64 | +32 | WEPHITMD | 命中の修正 | 確認(データ) |
| +66 | +34 | WEPHPDAM | ダメージ(ダイスの数、ダイスの面、加算) | 確認(データ。LONG SWORD = 1d8) |
| +72 | +40 | XTRASWNG | 追加の攻撃回数 | 推測 |
| +74 | +42 | CRITHITM | クリティカル(首をはねる) | 推測 |
| +76 | +44 | WEPVSTYP | 倍打の対象のビット列 | 推測 |

## 2. 機種間の比較(#1)

- 5 機種とも 101 件で、**名前以外の値(性能)は番号ごとに完全に一致した**(ボルタック商店の在庫数を除く)。
  キャラクタのレコードも同じ形式なので、同じシナリオどうしなら機種をまたいでキャラクタを移せる。
- 名前は機種によって違うものがある(下の表で太字)。
  - PC-98・MSX2・PC-8801 版の 86 番は `MURASAMA BLADE`(Apple II 版は `MURAMASA BLADE`)。
  - PC-DOS 版は `ASCII.KRN` の独自の名前(`POTION OF DIOS`、`COLD CHAIN MAIL`、`PRIESTS RING` など)。
- #1・#2・#3 は、同じ番号でも別のアイテム。シナリオをまたいで移すと持ち物が別のものになる。

## 3. #1 のアイテムの一覧

| 番号 | Apple II | PC-98 | PC-DOS | MSX2 | PC-8801 | 種類 | 装備できる職業 |
|---:|---|---|---|---|---|---|---|
| 0 | BROKEN ITEM | BROKEN ITEM | BROKEN ITEM | BROKEN ITEM | BROKEN ITEM | その他 | なし |
| 1 | LONG SWORD | LONG SWORD | LONG SWORD | LONG SWORD | LONG SWORD | 武器 | 戦士・侍・君主・忍者 |
| 2 | SHORT SWORD | SHORT SWORD | SHORT SWORD | SHORT SWORD | SHORT SWORD | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 3 | ANOINTED MACE | ANOINTED MACE | ANOINTED MACE | ANOINTED MACE | ANOINTED MACE | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 4 | ANOINTED FLAIL | ANOINTED FLAIL | ANOINTED FLAIL | ANOINTED FLAIL | ANOINTED FLAIL | 武器 | 戦士・僧侶・侍・君主・忍者 |
| 5 | STAFF | STAFF | STAFF | STAFF | STAFF | 武器 | 全職業 |
| 6 | DAGGER | DAGGER | DAGGER | DAGGER | DAGGER | 武器 | 戦士・魔術師・盗賊・侍・君主・忍者 |
| 7 | SMALL SHIELD | SMALL SHIELD | SMALL SHIELD | SMALL SHIELD | SMALL SHIELD | 盾 | 戦士・僧侶・盗賊・司教・侍・君主・忍者 |
| 8 | LARGE SHIELD | LARGE SHIELD | LARGE SHIELD | LARGE SHIELD | LARGE SHIELD | 盾 | 戦士・僧侶・侍・君主・忍者 |
| 9 | ROBES | ROBES | ROBES | ROBES | ROBES | 鎧 | 全職業 |
| 10 | LEATHER ARMOR | LEATHER ARMOR | LEATHER ARMOR | LEATHER ARMOR | LEATHER ARMOR | 鎧 | 戦士・僧侶・盗賊・司教・侍・君主・忍者 |
| 11 | CHAIN MAIL | CHAIN MAIL | CHAIN MAIL | CHAIN MAIL | CHAIN MAIL | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 12 | BREAST PLATE | BREAST PLATE | BREAST PLATE | BREAST PLATE | BREAST PLATE | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 13 | PLATE MAIL | PLATE MAIL | PLATE MAIL | PLATE MAIL | PLATE MAIL | 鎧 | 戦士・侍・君主・忍者 |
| 14 | HELM | HELM | HELM | HELM | HELM | 兜 | 戦士・侍・君主・忍者 |
| 15 | DIOS POTION | DIOS POTION | **POTION OF DIOS** | DIOS POTION | DIOS POTION | 特殊 | 全職業 |
| 16 | LATUMOFIS POT. | LATUMOFIS POT. | **POTION OF LATUMOFIS** | LATUMOFIS POT. | LATUMOFIS POT. | 特殊 | 全職業 |
| 17 | LONG SWORD+1 | LONG SWORD+1 | LONG SWORD+1 | LONG SWORD+1 | LONG SWORD+1 | 武器 | 戦士・侍・君主・忍者 |
| 18 | SHORT SWORD+1 | SHORT SWORD+1 | SHORT SWORD+1 | SHORT SWORD+1 | SHORT SWORD+1 | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 19 | MACE+1 | MACE+1 | MACE+1 | MACE+1 | MACE+1 | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 20 | STAFF OF MOGREF | STAFF OF MOGREF | STAFF OF MOGREF | STAFF OF MOGREF | STAFF OF MOGREF | 武器 | 魔術師・司教 |
| 21 | SCROLL/KANTINO | SCROLL/KANTINO | **SCROLL OF KATINO** | SCROLL/KANTINO | SCROLL/KANTINO | 特殊 | 全職業 |
| 22 | LEATHER + 1 | LEATHER + 1 | **LEATHER+1** | LEATHER + 1 | LEATHER + 1 | 鎧 | 戦士・僧侶・盗賊・司教・侍・君主・忍者 |
| 23 | CHAIN MAIL + 1 | CHAIN MAIL + 1 | **CHAIN MAIL+1** | CHAIN MAIL + 1 | CHAIN MAIL + 1 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 24 | PLATE MAIL + 1 | PLATE MAIL + 1 | **PLATE MAIL+1** | PLATE MAIL + 1 | PLATE MAIL + 1 | 鎧 | 戦士・侍・君主・忍者 |
| 25 | SHIELD + 1 | SHIELD + 1 | **SHIELD+1** | SHIELD + 1 | SHIELD + 1 | 盾 | 戦士・僧侶・盗賊・侍・君主・忍者 |
| 26 | BREAST PLATE +1 | BREAST PLATE +1 | **BREAST PLATE+1** | BREAST PLATE +1 | BREAST PLATE +1 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 27 | SCROLL/BADIOS | SCROLL/BADIOS | **SCROLL OF BADIOS** | SCROLL/BADIOS | SCROLL/BADIOS | 特殊 | 全職業 |
| 28 | SCROLL/HALITO | SCROLL/HALITO | **SCROLL OF HALITO** | SCROLL/HALITO | SCROLL/HALITO | 特殊 | 全職業 |
| 29 | LONG SWORD -1 | LONG SWORD -1 | **LONG SWORD-1** | LONG SWORD -1 | LONG SWORD -1 | 武器 | 戦士・侍・君主・忍者 |
| 30 | SHORT SWORD -1 | SHORT SWORD -1 | **SHORT SWORD-1** | SHORT SWORD -1 | SHORT SWORD -1 | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 31 | MACE -1 | MACE -1 | **MACE-1** | MACE -1 | MACE -1 | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 32 | STAFF +2 | STAFF +2 | **STAFF+2** | STAFF +2 | STAFF +2 | 武器 | 全職業 |
| 33 | DRAGON SLAYER | DRAGON SLAYER | **SLAYER OF DRAGONS** | DRAGON SLAYER | DRAGON SLAYER | 武器 | 戦士・侍・君主・忍者 |
| 34 | HELM + 1 | HELM + 1 | **HELM+1** | HELM + 1 | HELM + 1 | 兜 | 戦士・侍・君主・忍者 |
| 35 | LEATHER -1 | LEATHER -1 | **LEATHER-1** | LEATHER -1 | LEATHER -1 | 鎧 | 戦士・僧侶・盗賊・司教・侍・君主 |
| 36 | CHAIN -1 | CHAIN -1 | **CHAIN-1** | CHAIN -1 | CHAIN -1 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 37 | BREAST PLATE-1 | BREAST PLATE-1 | BREAST PLATE-1 | BREAST PLATE-1 | BREAST PLATE-1 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 38 | SHIELD -1 | SHIELD -1 | **SHIELD-1** | SHIELD -1 | SHIELD -1 | 盾 | 戦士・僧侶・盗賊・侍・君主 |
| 39 | JEWELED AMULET | JEWELED AMULET | **AMULET OF JEWELS** | JEWELED AMULET | JEWELED AMULET | その他 | 全職業 |
| 40 | SCROLL/BADIOS | SCROLL/BADIOS | **SCROLL OF BADIOS** | SCROLL/BADIOS | SCROLL/BADIOS | 特殊 | 全職業 |
| 41 | POTION OF SOPIC | POTION OF SOPIC | POTION OF SOPIC | POTION OF SOPIC | POTION OF SOPIC | 特殊 | 全職業 |
| 42 | LONG SWORD + 2 | LONG SWORD + 2 | **LONG SWORD+2** | LONG SWORD + 2 | LONG SWORD + 2 | 武器 | 戦士・侍・君主・忍者 |
| 43 | SHORT SWORD +2 | SHORT SWORD +2 | **SHORT SWORD+2** | SHORT SWORD +2 | SHORT SWORD +2 | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 44 | MACE + 2 | MACE + 2 | **MACE+2** | MACE + 2 | MACE + 2 | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 45 | SCROLL/LOMILWA | SCROLL/LOMILWA | **SCROLL OF LOMILWA** | SCROLL/LOMILWA | SCROLL/LOMILWA | 特殊 | 全職業 |
| 46 | SCROLL/DILTO | SCROLL/DILTO | **SCROLL OF DILTO** | SCROLL/DILTO | SCROLL/DILTO | 特殊 | 全職業 |
| 47 | COPPER GLOVES | COPPER GLOVES | **GLOVES OF COPPER** | COPPER GLOVES | COPPER GLOVES | 小手 | 戦士・侍・君主・忍者 |
| 48 | LEATHER + 2 | LEATHER + 2 | **LEATHER+2** | LEATHER + 2 | LEATHER + 2 | 鎧 | 戦士・僧侶・盗賊・司教・侍・君主・忍者 |
| 49 | CHAIN + 2 | CHAIN + 2 | **CHAIN+2** | CHAIN + 2 | CHAIN + 2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 50 | PLATE MAIL + 2 | PLATE MAIL + 2 | **PLATE MAIL+2** | PLATE MAIL + 2 | PLATE MAIL + 2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 51 | SHIELD + 2 | SHIELD + 2 | **SHIELD+2** | SHIELD + 2 | SHIELD + 2 | 盾 | 戦士・僧侶・盗賊・侍・君主・忍者 |
| 52 | HELM +2 (EVIL) | HELM +2 (EVIL) | **HELM+2 (E)** | HELM +2 (EVIL) | HELM +2 (EVIL) | 兜 | 戦士・侍・君主・忍者 |
| 53 | POTION OF DIAL | POTION OF DIAL | POTION OF DIAL | POTION OF DIAL | POTION OF DIAL | 特殊 | 全職業 |
| 54 | RING OF PORFIC | RING OF PORFIC | RING OF PORFIC | RING OF PORFIC | RING OF PORFIC | その他 | 全職業 |
| 55 | WERE SLAYER | WERE SLAYER | WERE SLAYER | WERE SLAYER | WERE SLAYER | 武器 | 戦士・侍・君主・忍者 |
| 56 | MAGE MASHER | MAGE MASHER | MAGE MASHER | MAGE MASHER | MAGE MASHER | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 57 | MACE PRO POISON | MACE PRO POISON | **MACE OF POISON** | MACE PRO POISON | MACE PRO POISON | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 58 | STAFF/MONTINO | STAFF/MONTINO | **STAFF OF MONTINO** | STAFF/MONTINO | STAFF/MONTINO | 武器 | 全職業 |
| 59 | BLADE CUSINART' | BLADE CUSINART' | BLADE CUSINART' | BLADE CUSINART' | BLADE CUSINART' | 武器 | 戦士・侍・君主・忍者 |
| 60 | AMULET/MANIFO | AMULET/MANIFO | **AMULET OF MANIFO** | AMULET/MANIFO | AMULET/MANIFO | その他 | 僧侶 |
| 61 | ROD OF FLAME | ROD OF FLAME | ROD OF FLAME | ROD OF FLAME | ROD OF FLAME | その他 | 魔術師・司教・侍 |
| 62 | EVIL CHAIN + 2 | EVIL CHAIN + 2 | **CHAIN+2 (E)** | EVIL CHAIN + 2 | EVIL CHAIN + 2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 63 | NEUT P-MAIL + 2 | NEUT P-MAIL + 2 | **PLATE+2 (N)** | NEUT P-MAIL + 2 | NEUT P-MAIL + 2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 64 | EVIL SHIELD +3 | EVIL SHIELD +3 | **SHIELD+3 (E)** | EVIL SHIELD +3 | EVIL SHIELD +3 | 盾 | 戦士・僧侶・盗賊・侍・君主・忍者 |
| 65 | AMULET/MAKANITO | AMULET/MAKANITO | **AMULET OF MAKANITO** | AMULET/MAKANITO | AMULET/MAKANITO | その他 | 全職業 |
| 66 | DIADEM OF MALOR | DIADEM OF MALOR | **HELM OF MALOR** | DIADEM OF MALOR | DIADEM OF MALOR | 兜 | 全職業 |
| 67 | SCROLL/BADIAL | SCROLL/BADIAL | **SCROLL OF BADIAL** | SCROLL/BADIAL | SCROLL/BADIAL | 特殊 | 全職業 |
| 68 | SHORT SWORD -2 | SHORT SWORD -2 | **SHORT SWORD-2** | SHORT SWORD -2 | SHORT SWORD -2 | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 69 | DAGGER + 2 | DAGGER + 2 | **DAGGER+2** | DAGGER + 2 | DAGGER + 2 | 武器 | 戦士・魔術師・盗賊・侍・君主・忍者 |
| 70 | MACE -2 | MACE -2 | **MACE-2** | MACE -2 | MACE -2 | 武器 | 戦士・僧侶・司教・侍・君主・忍者 |
| 71 | STAFF -2 | STAFF -2 | **STAFF-2** | STAFF -2 | STAFF -2 | 武器 | 全職業 |
| 72 | DAGGER OF SPEED | DAGGER OF SPEED | DAGGER OF SPEED | DAGGER OF SPEED | DAGGER OF SPEED | 武器 | 魔術師・忍者 |
| 73 | CURSED ROBE | CURSED ROBE | **ROBE OF CURSES** | CURSED ROBE | CURSED ROBE | 鎧 | 全職業 |
| 74 | LEATHER -2 | LEATHER -2 | **LEATHER-2** | LEATHER -2 | LEATHER -2 | 鎧 | 戦士・僧侶・盗賊・司教・侍・君主・忍者 |
| 75 | CHAIN -2 | CHAIN -2 | **CHAIN-2** | CHAIN -2 | CHAIN -2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 76 | BREAST PLATE -2 | BREAST PLATE -2 | **BREAST PLATE-2** | BREAST PLATE -2 | BREAST PLATE -2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 77 | SHIELD - 2 | SHIELD - 2 | **SHIELD-2** | SHIELD - 2 | SHIELD - 2 | 盾 | 戦士・僧侶・盗賊・侍・君主・忍者 |
| 78 | CURSED HELMET | CURSED HELMET | **HELM OF CURSES** | CURSED HELMET | CURSED HELMET | 兜 | 戦士・侍・君主・忍者 |
| 79 | BREAST PLATE +2 | BREAST PLATE +2 | **BREAST PLATE+2** | BREAST PLATE +2 | BREAST PLATE +2 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 80 | SILVER GLOVES | SILVER GLOVES | **GLOVES OF SILVER** | SILVER GLOVES | SILVER GLOVES | 小手 | 戦士・侍・君主・忍者 |
| 81 | EVIL SWORD + 3 | EVIL SWORD + 3 | **SWORD+3 (E)** | EVIL SWORD + 3 | EVIL SWORD + 3 | 武器 | 戦士・侍・君主・忍者 |
| 82 | EVIL SSWORD + 3 | EVIL SSWORD + 3 | **S-SWORD+3 (E)** | EVIL SSWORD + 3 | EVIL SSWORD + 3 | 武器 | 戦士・盗賊・侍・君主・忍者 |
| 83 | THIEVES DAGGER | THIEVES DAGGER | **DAGGER OF THIEVES** | THIEVES DAGGER | THIEVES DAGGER | 武器 | 盗賊・忍者 |
| 84 | BREAST PLATE +3 | BREAST PLATE +3 | **BREAST PLATE+3** | BREAST PLATE +3 | BREAST PLATE +3 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 85 | LORDS GARB | LORDS GARB | **GARB OF LORDS** | LORDS GARB | LORDS GARB | 鎧 | 君主 |
| 86 | MURAMASA BLADE | **MURASAMA BLADE** | **MURAMASA BLADE!** | **MURASAMA BLADE** | **MURASAMA BLADE** | 武器 | 侍 |
| 87 | SHURIKEN | SHURIKEN | **SHURIKENS** | SHURIKEN | SHURIKEN | 武器 | 忍者 |
| 88 | CHAIN PRO FIRE | CHAIN PRO FIRE | **COLD CHAIN MAIL** | CHAIN PRO FIRE | CHAIN PRO FIRE | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 89 | EVIL PLATE + 3 | EVIL PLATE + 3 | **PLATE+3 (E)** | EVIL PLATE + 3 | EVIL PLATE + 3 | 鎧 | 戦士・僧侶・侍・君主・忍者 |
| 90 | SHIELD + 3 | SHIELD + 3 | **SHIELD+3** | SHIELD + 3 | SHIELD + 3 | 盾 | 戦士・僧侶・盗賊・侍・君主・忍者 |
| 91 | RING OF HEALING | RING OF HEALING | RING OF HEALING | RING OF HEALING | RING OF HEALING | その他 | 全職業 |
| 92 | RING PRO UNDEAD | RING PRO UNDEAD | **PRIESTS RING** | RING PRO UNDEAD | RING PRO UNDEAD | その他 | 全職業 |
| 93 | DEADLY RING | DEADLY RING | **RING OF DEATH!** | DEADLY RING | DEADLY RING | その他 | 全職業 |
| 94 | WERDNA'S AMULET | WERDNA'S AMULET | **AMULET OF WERDNA** | WERDNA'S AMULET | WERDNA'S AMULET | その他 | 全職業 |
| 95 | STATUETTE/BEAR | STATUETTE/BEAR | **STATUE OF BEAR** | STATUETTE/BEAR | STATUETTE/BEAR | 特殊 | なし |
| 96 | STATUETTE/FROG | STATUETTE/FROG | **STATUE OF FROG** | STATUETTE/FROG | STATUETTE/FROG | 特殊 | なし |
| 97 | BRONZE KEY | BRONZE KEY | **KEY OF BRONZE** | BRONZE KEY | BRONZE KEY | 特殊 | なし |
| 98 | SILVER KEY | SILVER KEY | **KEY OF SILVER** | SILVER KEY | SILVER KEY | 特殊 | なし |
| 99 | GOLD KEY | GOLD KEY | **KEY OF GOLD** | GOLD KEY | GOLD KEY | 特殊 | なし |
| 100 | BLUE RIBBON | BLUE RIBBON | BLUE RIBBON | BLUE RIBBON | BLUE RIBBON | 特殊 | なし |

## 4. #1 の呪文の一覧

呪文の番号は、キャラクタの「覚えている呪文」のビットと、アイテムの SPELLPWR に共通。#1〜#3・全機種で同じ並び。

| ビット | 系統 | Lv | 名前 |
|---:|---|---:|---|
| 1 | 魔術師 | 1 | HALITO |
| 2 | 魔術師 | 1 | MOGREF |
| 3 | 魔術師 | 1 | KATINO |
| 4 | 魔術師 | 1 | DUMAPIC |
| 5 | 魔術師 | 2 | DILTO |
| 6 | 魔術師 | 2 | SOPIC |
| 7 | 魔術師 | 3 | MAHALITO |
| 8 | 魔術師 | 3 | MOLITO |
| 9 | 魔術師 | 4 | MORLIS |
| 10 | 魔術師 | 4 | DALTO |
| 11 | 魔術師 | 4 | LAHALITO |
| 12 | 魔術師 | 5 | MAMORLIS |
| 13 | 魔術師 | 5 | MAKANITO |
| 14 | 魔術師 | 5 | MADALTO |
| 15 | 魔術師 | 6 | LAKANITO |
| 16 | 魔術師 | 6 | ZILWAN |
| 17 | 魔術師 | 6 | MASOPIC |
| 18 | 魔術師 | 6 | HAMAN |
| 19 | 魔術師 | 7 | MALOR |
| 20 | 魔術師 | 7 | MAHAMAN |
| 21 | 魔術師 | 7 | TILTOWAIT |
| 22 | 僧侶 | 1 | KALKI |
| 23 | 僧侶 | 1 | DIOS |
| 24 | 僧侶 | 1 | BADIOS |
| 25 | 僧侶 | 1 | MILWA |
| 26 | 僧侶 | 1 | PORFIC |
| 27 | 僧侶 | 2 | MATU |
| 28 | 僧侶 | 2 | CALFO |
| 29 | 僧侶 | 2 | MANIFO |
| 30 | 僧侶 | 2 | MONTINO |
| 31 | 僧侶 | 3 | LOMILWA |
| 32 | 僧侶 | 3 | DIALKO |
| 33 | 僧侶 | 3 | LATUMAPIC |
| 34 | 僧侶 | 3 | BAMATU |
| 35 | 僧侶 | 4 | DIAL |
| 36 | 僧侶 | 4 | BADIAL |
| 37 | 僧侶 | 4 | LATUMOFIS |
| 38 | 僧侶 | 4 | MAPORFIC |
| 39 | 僧侶 | 5 | DIALMA |
| 40 | 僧侶 | 5 | BADIALMA |
| 41 | 僧侶 | 5 | LITOKAN |
| 42 | 僧侶 | 5 | KANDI |
| 43 | 僧侶 | 5 | DI |
| 44 | 僧侶 | 5 | BADI |
| 45 | 僧侶 | 6 | LORTO |
| 46 | 僧侶 | 6 | MADI |
| 47 | 僧侶 | 6 | MABADI |
| 48 | 僧侶 | 6 | LOKTOFEIT |
| 49 | 僧侶 | 7 | MALIKTO |
| 50 | 僧侶 | 7 | KADORTO |
