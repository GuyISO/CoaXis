# CoaXisViewer AutoLoad ガイド

## 1. 目的
この文書は、AutoLoad 登録クラスの実装方針を揃え、シングルトン運用の重複実装を避けるためのルールをまとめる。

## 2. 実装方針

### 2.1 共通基底クラス
- AutoLoad 登録する Node 派生クラスは、原則として src/autoload/SingletonNodeBase.cs を継承する。
- SingletonNodeBase<TNode> が _EnterTree / _ExitTree で Instance の確立と解放を担当する。
- 個別クラスで Instance プロパティや単純な _EnterTree / _ExitTree を再実装しない。

### 2.2 EventHub 系
- EventHub 系は src/autoload/event/EventHubBase.cs を継承する。
- EventHubBase<T> は SingletonNodeBase<T> の上に TryEmitSignal を追加し、未初期化時の警告と発火ログを共通化する。
- 各 Hub は Signal 定義と static な Request / Notify API だけを持つ。

### 2.3 _ExitTree の扱い
- _ExitTree で独自の購読解除やリソース解放が必要な場合は、その処理を先に行ってから base._ExitTree() を呼ぶ。
- Hub や他 AutoLoad の終了順は固定ではないため、購読解除時は対象 Instance の null を考慮する。

## 3. 対象クラス
- EventHub: ModelEventHub, ViewportEventHub, PickEventHub
- Services: ModelOperationService, ModelVisualService, Selection, SettingsService, UiManager
- Systems/Input: LogHub, AssetManager, DeviceInputHandler

## 4. 新規追加時のチェック
- AutoLoad に登録する理由があるか。
- static API から Instance を参照する必要があるか。
- 単純な singleton 確立だけなら SingletonNodeBase<TNode> で足りるか。
- Signal 発火共通化が必要なら EventHubBase<T> を使うか。
- _ExitTree に cleanup がある場合、base._ExitTree() を最後に呼んでいるか。