# IPC Protocol Guide

## 1. 目的
Editor と Viewer 間でやり取りする IPC メッセージの最小仕様を定義する。

## 2. エンベロープ形式

```json
{
  "command": "LoadModel",
  "payload": {
    "path": "C:/models/sample.glb"
  }
}
```

- command: 実行するコマンド名
- payload: コマンド固有データ

## 3. 現在サポートしているコマンド

### 3.1 LoadModel
payload:
- path: string

動作:
- ModelEvent.LoadModel(path) を呼ぶ

### 3.2 Select / Highlight
payload:
- name: string（単体）
- names: string[]（複数）

動作:
- 一致する AnyModel を名前で探索し、選択要求を発行

### 3.3 ClearHighlight
動作:
- ModelEvent.ClearSelection() を呼ぶ

### 3.4 Hide / Show
payload:
- name: string または names: string[]

動作:
- 一致モデルの Visible を false/true に設定

### 3.5 Focus
payload:
- name: string または names: string[]（省略可）

動作:
- 指定モデルがあればそれに Fit
- 指定がなければ現在選択モデルに Fit

### 3.6 ApplyViewPreset
payload:
- projection: "perspective" | "orthogonal" | "toggle"

動作:
- 投影タイプ変更要求を発行

### 3.7 ApplyCameraPreset
payload（任意の組み合わせ）:
- distance: number
- size: number
- fov: number

動作:
- 指定された項目だけ設定要求を発行

## 4. エラー時ポリシー
- JSON不正: エラーログを出し false を返す
- command欠落: 警告ログを出し false を返す
- 未対応command: 警告ログを出し false を返す
- 対象モデル未発見: 警告ログを出し false を返す

## 5. Result応答（標準）

NamedPipe 受信メッセージごとに、Viewer は必ず次の形式で1行JSONを返す。

```json
{
  "command": "Result",
  "payload": {
    "ok": true,
    "request": "LoadModel",
    "errorCode": "NONE",
    "message": "load model requested"
  }
}
```

payload フィールド:
- ok: 成功時 true、失敗時 false
- request: 処理対象の command 名
- errorCode: 標準化されたエラーコード
- message: 人が読める補足メッセージ

### 5.1 標準 errorCode 一覧
- NONE
- EMPTY_MESSAGE
- INVALID_JSON
- MISSING_COMMAND
- UNSUPPORTED_COMMAND
- INVALID_PAYLOAD
- TARGET_NOT_FOUND
- INTERNAL_ERROR
- MAIN_THREAD_TIMEOUT

## 6. 今後の拡張方針
- 応答メッセージ（成功/失敗と理由）を明示的に返す
- model識別を name から UUID ベースへ移行
- 双方向同時接続時の同時実行制御を追加

## 7. 座標系ポリシー
- 外部境界（IPC payload、DB保存、ユーザー表示）は CATIA 座標系で統一する。
- Viewer 内部（Node3D の Transform、カメラ操作、物理/描画計算）は Godot 座標系で扱う。
- 境界を跨ぐ変換は `CoordinateSystemUtility` を使用して行う。

CATIA -> Godot の軸対応:
- Xg = Yc
- Yg = Zc
- Zg = Xc

Godot -> CATIA の軸対応:
- Xc = Zg
- Yc = Xg
- Zc = Yg
