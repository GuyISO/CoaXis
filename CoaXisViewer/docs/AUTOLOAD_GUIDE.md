# CoaXisViewer AutoLoad ガイド

## 1. 目的
この文書は、AutoLoad 登録クラスの実装方針を揃え、責務分離と初期化順を安定化するためのルールをまとめる。

## 2. 実装方針

### 2.1 Application の役割
- AutoLoad の入口は Application とし、Application が子ノードとして各モジュールを生成・保持する。
- グローバルアクセスは Application 経由に集約する。
- Instance 管理が必要な場合は、Application クラス内で明示的に管理する。
- Application が公開する機能群は Facade としてまとめ、利用側は ModelFacade, PickFacade, ViewportFacade, SelectionFacade のような窓口を経由する。
- Facade の内部実体は既存の Event / Service 実装を再利用してよく、公開 API の集約点だけを Application に寄せる。

### 2.2 Event 系
- Event 系は src/application/event/EventBase.cs を継承する。
- EventBase<T> は Emit を共通化し、未初期化時の警告と発火ログを集約する。
- 各 Hub は Signal 定義と Action / Notify API を持ち、呼び出しは Application から行う。
- Request 系の公開メソッド名は使わず、AskRootModel, SetMultiSelectionMode, ClearSelection のように動作名だけで統一する。
- Notify 系は通知専用としてそのまま維持する。
- Signal 名の Requested / Notified は内部イベント表現として残してよい。

### 2.3 Facade 系
- Facade は Application から見た公開窓口として配置し、外部コードは原則として Facade のみを参照する。
- Facade は Node ベースで実装し、Event / Service は子ノードとして内包する。
- 既存の Node アクセサ名を残す場合でも、実体は Facade を返すように統一する。

### 2.4 FacadeBase の共通化
- Domain Facade の共通処理は src/application/base/FacadeBase.cs に集約する。
- 子ノード生成は FacadeBase の AddModule<TModule>(nodeName) を使い、各 Facade で new/AddChild を重複実装しない。

### 2.5 Domain Facade の実装テンプレート
- Domain Facade は次の形を基本とする。

```csharp
public partial class ExampleFacade : FacadeBase
{
	public ExampleEvent Event { get; }
	public ExampleService Service { get; }

	public ExampleFacade()
	{
		Event = AddModule<ExampleEvent>("ExampleEvent");
		Service = AddModule<ExampleService>("ExampleService");
	}
}
```

### 2.6 _ExitTree の扱い
- _ExitTree で独自の購読解除やリソース解放が必要な場合は、その処理を先に行ってから base._ExitTree() を呼ぶ。
- Hub や他 AutoLoad の終了順は固定ではないため、購読解除時は対象ノードの有効性を考慮する。

## 3. 対象クラス
- Facade: LogFacade, SettingFacade, ViewportFacade, ModelFacade, PickFacade, SelectionFacade, MeasurementFacade, UiFacade, AssetFacade
- Event: LogEvent, ViewportEvent, ModelEvent, PickEvent, MeasurementEvent
- Service: LogService, SettingService, ModelOperationService, ModelVisualService, SelectionService, MeasurementService, UiManager, AssetManager
- Systems/Input: DeviceInputHandler

## 4. 新規追加時のチェック
- AutoLoad に登録する理由があるか。
- Application 配下のモジュールとして配置するべきか。
- Application 経由で公開する API を最小化できているか。
- Signal 発火共通化が必要なら EventBase<T> を使うか。
- _ExitTree に cleanup がある場合、base._ExitTree() を最後に呼んでいるか。