# CoaXisViewer AutoLoad ガイド

## 1. 目的
この文書は、AutoLoad 登録クラスの実装方針を揃え、責務分離と初期化順を安定化するためのルールをまとめる。

## 2. 実装方針

### 2.1 Application の役割
- AutoLoad の入口は Application とし、Application が子ノードとして各モジュールを生成・保持する。
- グローバルアクセスは Application 経由に集約する。
- Instance 管理が必要な場合は、Application クラス内で明示的に管理する。

### 2.2 Event 系
- Event 系は src/application/event/EventBase.cs を継承する。
- EventBase<T> は Emit を共通化し、未初期化時の警告と発火ログを集約する。
- 各 Hub は Signal 定義と Action / Notify API を持ち、呼び出しは Application から行う。
- Request 系の公開メソッド名は使わず、AskRootModel, SetMultiSelectionMode, ClearSelection のように動作名だけで統一する。
- Notify 系は通知専用としてそのまま維持する。
- Signal 名の Requested / Notified は内部イベント表現として残してよい。

### 2.3 _ExitTree の扱い
- _ExitTree で独自の購読解除やリソース解放が必要な場合は、その処理を先に行ってから base._ExitTree() を呼ぶ。
- Hub や他 AutoLoad の終了順は固定ではないため、購読解除時は対象ノードの有効性を考慮する。

## 3. 対象クラス
- Event: ModelEvent, ViewportEvent, PickEvent
- Services: ModelOperationService, ModelVisualService, Selection, SettingsService, UiManager
- Systems/Input: LogHub, AssetManager, DeviceInputHandler

## 4. 新規追加時のチェック
- AutoLoad に登録する理由があるか。
- Application 配下のモジュールとして配置するべきか。
- Application 経由で公開する API を最小化できているか。
- Signal 発火共通化が必要なら EventBase<T> を使うか。
- _ExitTree に cleanup がある場合、base._ExitTree() を最後に呼んでいるか。