# CoaXisViewer ドキュメント整備ロードマップ

## 1. 優先度A（先に作る）

### 1.1 ARCHITECTURE.md
- Autoload / Core / Features / Scenes の責務分離
- 依存方向の原則
- シーン階層の概要

### 1.1.1 AUTOLOAD_GUIDE.md
- AutoLoad 共通基底クラスの使い分け
- Instance 確立と解放の共通化ルール
- EventHubBase と Application Facade の責務境界

### 1.2 EVENTS_GUIDE.md
- ModelEventHub と ViewportEventHub のAction/Notify一覧
- 初期状態通知のタイミング
- 購読解除パターン
- 二重購読・未解除の典型バグ

### 1.3 MODEL_LOADING_PIPELINE.md
- ロード要求から表示までの時系列
- コライダー生成フロー
- ロード失敗時の復旧方針

## 2. 優先度B（次に作る）

### 2.1 CAMERA_INPUT_SYSTEM.md
- ViewportInteractionMode の状態遷移
- Pan/Orbit/Roll/Zoom/PickRect 操作仕様
- Arcball計算の要点

### 2.2 IPC_PROTOCOL_GUIDE.md
- IPCコマンド形式
- 受信時バリデーション
- エラー応答設計
- 現在のサポートコマンド一覧（LoadModel, Select, Focus など）

### 2.3 API_REFERENCE_SETUP.md
- XMLコメントの生成ルール
- DocFX等の生成手順

## 3. 各ドキュメント共通テンプレート
- 目的
- 対象範囲
- 主要クラス
- シーケンス図または状態遷移図
- 失敗時挙動
- テスト観点

## 4. 運用ルール
- 新規機能PRでは、該当ドキュメント差分を同時提出する。
- 仕様変更時はコードより先に文書の見出しを更新する。
- 四半期ごとに未更新ページを棚卸しする。
