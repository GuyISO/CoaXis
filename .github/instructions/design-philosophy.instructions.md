---
description: "Use when changing CoaXis architecture, state ownership, layer boundaries, or reusable implementation rules."
---

# 設計方針

## 方針の記録

設計方針は適用条件を含む判定可能なルールとして本ファイルへ追加し、既存の正本に重ねて記載しない。未反映の判断だけを`.github/design-philosophy/registry.md`へ短く記録する。

## 実装上の不変条件

- **Signal payload:** 自作の参照型は `RefCounted` を継承する。状態変更通知は値を載せず、対象を示す場合もIDのみ。一過性イベントは必要なpayloadを許容する。
- **状態所有:** 状態Hubが正本を所有し、購読者は購読後にPropertyを読み、その後は通知に追従する。要求はHubの公開メソッドを呼び、AskState等で再通知させない。Node/UIに状態を複製しない。
- **設定・描画:** `Constant` 値はキャッシュしない。`SettingService` 値は高頻度参照時だけキャッシュする。`SettingsNotified` で表示が変わる場合、同じハンドラで再描画・再構築する。
- **モデル:** ドメインIDは`Guid entityId` / `propertyId`とし、Signal/Event/Pick/UI間ではModelNode参照でなくIDを渡す。Godot Signalのstring IDは受信直後に `Guid.TryParse` する。`ModelHub` はEntity/Propertyを分け、各Registryが識別子と階層を所有する。Entity更新はModelEntityが値と対応Nodeを同期し、StateHubは通知のみ、VisualHubは実効Visibilityと表示効果、UIはTree投影を担う。Status遷移はModelEntityの専用メソッドに限定し、`IsVisible` はVisibilityと親階層から導出する。EntityとPropertyをまたぐ置換・部分追加・連鎖削除は`ModelSetHub`が調整する。DTOからEntity/Propertyを生成し、集合のID・階層を検証する入口は`ModelFactory`に集約し、MapperはDTO単体の変換を担う。Entityの描画NodeとSceneロードは親IDをたどってRootへ到達するEntityに限り、未解決EntityはRegistryに保持してもNodeを作らず、Rootへ接続された時点で生成する。
- **モデル操作・UI:** Pick/Measurement/Load/SceneはEntity配下、Property LoadはProperty配下に置く。Hub間は責務・依存方向を保ち循環させない。PopupMenuはUIごとに複製せず、対象IDを受けるApplication UI Menuから表示する。
- **Viewport:** Camera状態・操作は`ViewportCameraHub`、操作状態は`ViewportInteractionHub`、Layer状態は`ViewportDisplayHub`が所有する。`ViewportHub`は構成、`CameraRig`は状態のScene反映に限る。
- **IPC:** Protocolはwire DTOとenvelopeを扱い、ViewerがDTOからドメインへ変換する。単純なpayload factoryはpayload自身に置き、専用Builderを増やさない。
- **Application Hub:** 7 region（`Fields / Properties / Signals / Lifecycle / Events / Methods / Helpers`）をこの順で必ず置く。`[Signal]` と`NotifyXxx`はSignals、購読処理・`OnXxx`はEvents、操作はMethods、補助はHelpers。Lifecycle以外の全メンバーにXML summaryを付け、Actions/Notifications regionは使わない。
