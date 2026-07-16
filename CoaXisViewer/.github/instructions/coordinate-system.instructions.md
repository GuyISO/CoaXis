---
description: "Use when: 座標系をまたぐデータ（UI表示・DB/IPC/ファイルI/O・内部計算）を実装または変更するとき"
applyTo: "src/**/*.cs"
---

# Coordinate System Policy

## Scope

- Godot C# 実装で座標値・法線・姿勢を扱う処理

## Rules

1. 内部計算（Node3D Transform、当たり判定、ベクトル演算、距離計算）は Godot 座標系で行う。
2. 外部境界（UI表示、DB保存、IPC payload、ファイル出力）は CATIA 座標系で扱う。
3. 境界変換は `CoordinateSystemUtility` を必ず使う。
4. 単位換算（m <-> mm）が必要な場合は、境界層でのみ行う。

## Prohibited

- ドメインサービス内部で表示用の CATIA 変換や mm 換算を混在させること。
- 変換式を各所へ直書きすること。