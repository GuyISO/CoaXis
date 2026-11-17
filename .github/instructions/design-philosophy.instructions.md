---
description: "Use when changing CoaXis architecture, state ownership, layer boundaries, or reusable implementation rules."
---

# 設計方針

## 方針の記録

設計方針は適用条件を含む判定可能なルールとして本ファイルへ追加し、既存の正本に重ねて記載しない。未反映の判断だけを`.github/design-philosophy/registry.md`へ短く記録する。

## 実装上の不変条件

- **モジュール名:** Viewer・IpcTester・SceneBuilderのモジュール名はそれぞれ `CoaXis.Viewer`・`CoaXis.IpcTester`・`CoaXis.SceneBuilder` とする。既存のIPC識別子は互換性を考慮し、名前変更に合わせて自動的に変更しない。
- **Signal payload:** 自作の参照型は `RefCounted` を継承する。状態変更通知は値を載せず、対象を示す場合もIDのみ。一過性イベントは必要なpayloadを許容する。
- **状態所有:** 状態Hubが正本を所有し、購読者は購読後にPropertyを読み、その後は通知に追従する。要求はHubの公開メソッドを呼び、AskState等で再通知させない。Node/UIに状態を複製しない。
- **設定・描画:** Settingsは起動時に読み込み、Node/UIは初期化時に表示へ反映する。実行中の設定再読込による既存Node/UIへの動的反映や再描画は行わない。
- **テーマ選択:** 将来拡張する選択肢は安定した識別子で保持し、Hubの対応表からリソースへ解決する。設定・Hub・描画処理を二値フラグで分岐させず、二択UIのみが各選択肢の識別子へ変換する。
- **描画色:** UI・Viewportの色はThemeが所有し、設定JSONには保存しない。ブランド色を使う描画はTheme内のCoaXisプライマリ色を参照する。
- **モデル:** ドメインIDは`Guid entityId` / `propertyId`とし、Signal/Event/Pick/UI間ではModelNode参照でなくIDを渡す。Godot Signalのstring IDは受信直後に `Guid.TryParse` する。`ModelHub` はEntity/Propertyを分け、各Registryが識別子と階層を所有する。Entity更新はModelEntityが値と対応Nodeを同期し、StateHubは通知のみ、VisualHubは実効Visibilityと表示効果、UIはTree投影を担う。Status遷移はModelEntityの専用メソッドに限定し、`IsVisible` はVisibilityと親階層から導出する。EntityとPropertyをまたぐ置換・部分追加・連鎖削除は`ModelSetHub`が調整する。DTOからEntity/Propertyを生成し、集合のID・階層を検証する入口は`ModelFactory`に集約し、MapperはDTO単体の変換を担う。Replace/Addでは全EntityがRootへ到達し、全PropertyがEntityまたはPropertyを親としてEntityへ到達することを一括検証する。未解決親、空のProperty親ID、Entity/Property間のID重複を含む入力は全体を拒否し、Registryに孤児を残さない。
- **モデル操作・UI:** Pick/Measurement/Load/SceneはEntity配下、Property LoadはProperty配下に置く。Hub間は責務・依存方向を保ち循環させない。PopupMenuはUIごとに複製せず、対象IDを受けるApplication UI Menuから表示する。
- **モデル選択:** 共通モードは`ModelSelectionHub`、選択集合はEntity/Property各SelectionHubが所有する。新たな選択の追加前に他種別の選択を解除し、両集合を同時に非空にしない。PropertyはTreeなどのUIから選択し、EntityのPickには含めない。
- **Viewport:** Camera状態・操作は`ViewportCameraHub`、操作状態は`ViewportInteractionHub`、Layer状態は`ViewportDisplayHub`が所有する。`ViewportHub`は構成、`CameraRig`は状態のScene反映に限る。
- **IPC:** Protocolはwire DTOとenvelopeを扱い、ViewerがDTOからドメインへ変換する。単純なpayload factoryはpayload自身に置き、専用Builderを増やさない。
- **WinForms UI:** CoaXisのWindows Formsアプリケーションは`CoaXis.Ui`を参照し、フォームは`CoaXisForm`、通常のボタンは`CoaXisButton`、ページナビゲーションは`CoaXisNavigationRail`を基本にする。共有SVGは`CoaXisUiAssets`から参照し、アプリ間で重複保持しない。共通テーマが必要なコントロールは`ICoaXisThemedControl`を実装し、自身の外観を適用する。`ThemeManager`はテーマを保持し対象ツリーへ配布するが、個々のコントロール種別を判定しない。`CoaXisForm`は表示時とテーマ切替時に配下へ適用する。
- **Application Hub:** 7 region（`Fields / Properties / Signals / Lifecycle / Events / Methods / Helpers`）をこの順で必ず置く。`[Signal]` と`NotifyXxx`はSignals、購読処理・`OnXxx`はEvents、操作はMethods、補助はHelpers。Lifecycle以外の全メンバーにXML summaryを付け、Actions/Notifications regionは使わない。
