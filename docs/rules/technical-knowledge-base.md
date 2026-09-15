# 技術ナレッジベース

この文書は、属人化を防ぐために技術的な判断・制約・運用手順を記録する正本です。

## 記録ルール

1. 実装中に発生した重要判断は、同一作業で本ファイルへ追記する。
2. 口頭・チャット・個人メモのみで完結させない。
3. 各項目は次のテンプレートに従う。

## 運用追補ルール

### 1. ドキュメント同期

1. 仕様変更時は、関連する仕様書・運用手順・テスト観点を同時更新する。
2. 仕様と実装が矛盾する場合、矛盾を記録し解消方針を残す。

### 2. 失敗時のふるまい

1. 失敗時は隠蔽せず、原因・影響範囲・暫定対処・恒久対策を分けて記録する。
2. 同種の再発を防ぐため、学びをルールまたはチェックリストに反映する。

### 3. ルール自体の保守

1. ルールは運用で破綻したら改訂し、例外運用を常態化させない。
2. ルールは Must と Should を分け、違反時の扱いを明示する。

## 記録テンプレート

### [YYYY-MM-DD] タイトル

- 背景:
- 問題:
- 判断:
- 判断理由:
- 採用しなかった代替案:
- 影響範囲:
- 実装/運用手順:
- 検証方法:
- 関連ファイル/関連仕様:
- 備考:

---

### [2026-09-09] ModelFactory の DTO 一括生成へ統一

- 背景: モデルをDTOごとに生成すると、Registryの階層解決、SceneTree反映、シーンロードqueue投入の制御が各モデル単位で分散していた。
- 問題: 個別生成の繰り返しにより、全件登録前に階層解決が実行され、入力順によってTree通知の親子関係が不安定になる。また、バッチ内の重複IDや循環親子を副作用発生後に検出すると、Node・通知・queueが不整合になる。
- 判断: `ModelFactory.CreateFromDtos(IReadOnlyList<ModelDto>)` を唯一の生成入口とし、DTO変換、重複・循環検証、全件Registry登録、階層解決、Node生成、queue投入、親先行通知の順で処理する。1件の場合も1要素の集合を渡す。
- 判断理由: 全件登録後に `ResolveHierarchy` を一度だけ実行でき、親子関係を確定してからNodeとTreeへ反映できる。シーンロードqueueは全件投入と通知の後に起動し、生成処理中の状態通知順を安定させる。
- 採用しなかった代替案: 単件 `CreateFromDto` を互換ラッパーとして残す案は、利用経路が再び単件処理へ戻る余地を残すため不採用。UI向けの新しい一括イベントは、既存の `ModelAdded` 契約を維持できるため追加しない。
- 影響範囲: `CoaXisViewer/src/application/domain/model/ModelFactory.cs`、IPC/サンプルのモデルロード経路、ModelTreeの `ModelAdded` 通知順。
- 実装/運用手順: ローダーが返した `List<ModelDto>` をそのまま `CreateFromDtos` に渡す。1バッチ内の重複IDと循環親子は入力エラーとして扱う。未登録の親は従来どおりRoot配下へ配置する。ScenePathの実ロード完了はAPI戻り時点では保証しない。
- 検証方法: `check: mojibake`、`dotnet build .\\CoaXis.sln` を実行し、親先行・子先行・孤児parent・重複ID・循環親子・ScenePath空/重複・Clear直後を確認する。
- 関連ファイル/関連仕様: `CoaXisViewer/src/application/domain/model/ModelFactory.cs`、`CoaXisViewer/src/application/infrastructure/ipc/IpcCommandDispatcher.cs`、`CoaXisViewer/src/SampleTest.cs`
- 備考: SceneAssetLoaderのResourceLoader処理自体はGodotの制約により逐次であり、今回の一括化は同期側の重複処理とqueue起動制御を主な対象とする。

---

### [2026-09-07] SceneBuilder CLI をマルチプロセスで並列化

- 背景: `SceneBuildCommandLineRunner` は入力JSON内の全リクエストを1件ずつ `SceneBuilder.TryBuild()` で逐次処理しており、モデル数が多いバッチではCPUに余裕があっても処理時間が線形に伸びていた。
- 問題: Godot の `GltfDocument.GenerateScene()` / `Node.AddChild()` / `PackedScene.Pack()` / `ResourceSaver.Save()` は main thread 専用APIのため、同一プロセス内で `Task.Run` によるリクエスト単位の並列化は不可能。
- 判断: 入力リクエストを `builder-settings.json` の `MaxParallelism` 個のチャンクへ均等分割し、自プロセスの実行ファイルを子プロセスとして同数起動、各子プロセスが独立した main thread で並列にビルドするマルチプロセス方式を採用した。
- 判断理由: プロセスを分ければ各プロセスが自分の main thread を持つため、Godot のスレッド制約を完全に回避しつつ、OS プロセスレベルの並列度でCPUコアを活用できる。実装難度もプロセス内の部分バックグラウンド化（ローダーAPI大幅リファクタが必要）より低い。
- 採用しなかった代替案: GLB面抽出・LineSet幾何計算のみを `Task.Run` で背景スレッド化する部分並列化案は、効果が理論値30〜50%程度に留まり、既存ローダーAPI（`VisualGlbLoader`/`ColliderGlbLoader`/`LineSetJsonParser`）の大幅リファクタが必要なため見送った。将来的な追加最適化候補として残す。
- 影響範囲: `CoaXisSceneBuilder/src/SceneBuildCommandLineRunner.cs`、新規 `SceneBuildProcessOrchestrator`（`src/service/`）、新規 `SceneBuildRequestSplitter`（`src/util/`）、新規 `SceneBuildResultDto`（`src/dto/`）、`BuilderSettings`/`builder-settings.json`。
- 実装/運用手順: リクエスト数が2件以上かつ `MaxParallelism > 1` の場合のみ分割・並列起動する。子プロセスには環境変数 `COAXIS_SCENEBUILDER_WORKER=1` を渡して再分割を防止し、各チャンクの結果はサイドカーJSON（`<chunk>.result.json`）経由で親プロセスが集計する。一時ファイルは `%TEMP%\CoaXisSceneBuilder\batch_*` に作成し、実行後に削除する。`MaxParallelism<=1` またはリクエスト1件の場合は既存の逐次処理のまま（後方互換）。
- 検証方法: `dotnet build .\CoaXis.sln` が成功すること。`samples/scene_builder_input.json` を `MaxParallelism=1` と `4` の両方で実行し、生成される `.scn` が同等であること、複数プロセス実行時に壁時計時間が短縮されること、意図的な失敗リクエストを混在させても `failedCount` がチャンク横断で正しく合算されること、実行後に一時ディレクトリが残らないことを確認する。
- 関連ファイル/関連仕様: `CoaXisSceneBuilder/README.md`, `CoaXisSceneBuilder/settings/builder-settings.json`, `CoaXisSceneBuilder/src/SceneBuildCommandLineRunner.cs`, `CoaXisSceneBuilder/src/service/SceneBuildProcessOrchestrator.cs`
- 備考: 子プロセスの標準出力は `[chunk{n}]` プレフィックス付きで親プロセスの `GD.Print`/`GD.PushError` へ転送し、デバッグ性を確保している。

---

### [2026-08-31] ドキュメント同期を必須工程として正式化

- 背景: プロジェクトでコード変更と仕様書更新のズレが発生しやすく、README・仕様・運用ルールが更新漏れになるリスクがあった。
- 問題: 実装だけが先行し、ドキュメントが後回しになると、設計意図と現実が分離してしまう。
- 判断: 実装変更のたびに、影響範囲に応じたドキュメント更新を同一作業で行う運用を定める。
- 判断理由: 実装の「正」だけでなく、第三者が再開できる状態を維持するために、文書同期は開発の必須工程である。
- 採用しなかった代替案: 「後でまとめて更新する」運用は一時的に楽だが、ズレが固定化されやすく、レビュー品質が低下するため不採用。
- 影響範囲: ルート README、仕様書、運用ルール、今後の全 C# 実装作業。
- 実装/運用手順: 実装前に影響範囲を確認し、必要なドキュメントを特定したうえで同時更新し、ビルド・検証を実施する。
- 検証方法: `dotnet build .\CoaXis.sln` が成功し、関連 README・約束事・仕様書の記述に齟齬がないことを確認する。
- 関連ファイル/関連仕様: `README.md`, `docs/rules/documentation-sync-policy.md`, `docs/specification/specification_integrated.md`
- 備考: この運用は「ドキュメント更新が未実施の変更は完了扱いにしない」という基準を定着させるために導入した。

---

### [2026-06-27] EventHub の Request/Notification region 体裁を正式ルール化

- 背景: `ModelEventHub` と `ViewportEventHub` は、`Request` と `Notification` を分離した装飾付き region で運用されており、責務境界が読み取りやすい状態になっている。
- 問題: 一般規約の region 名統一だけを機械適用すると、EventHub の体裁が壊れ、意図した読みやすさが失われる。
- 判断: EventHub クラスのみ、装飾付きの `Request` / `Notification` region を例外として許可する。
- 判断理由: EventHub は「要求発行」と「状態通知」を同居させる集約点であり、2系統を視覚的に分けるメリットが大きい。
- 採用しなかった代替案: すべてを `Public Methods` に統合する案は、規約の単純さは高いが、EventHub では探索性が低下するため不採用。
- 影響範囲: `CoaXisViewer/src/autoload/event/*EventHub.cs` と region 体裁リファクタ手順。
- 実装/運用手順: EventHub 変更時は `Properties -> Lifecycle -> Request -> Notification` の順序を維持し、非 EventHub クラスには同装飾 region を持ち込まない。
- 検証方法: `#region` 検索で EventHub 以外に装飾付き `Request` / `Notification` が存在しないことを確認する。
- 関連ファイル/関連仕様: `docs/rules/implementation-conventions.md`, `CoaXisViewer/src/autoload/event/ModelEventHub.cs`, `CoaXisViewer/src/autoload/event/ViewportEventHub.cs`
- 備考: 体裁統一の自動化を行う場合は、EventHub の例外パターンを除外条件に含める。

---

### [2026-06-01] Viewer IPCメッセージをcommand/payloadエンベロープへ統一

- 背景: CoaXisViewer の IPC 実装が PoC 段階の文字列送信中心で、仕様書のコマンド契約（LoadModel, ApplyCameraPreset など）と追跡しづらかった。
- 問題: 受信側が自由文字列依存だと、Editor-Viewer間で互換性を維持しにくく、エラー時の切り分けが困難。
- 判断: Viewer 側は JSON の `command` と `payload` を持つエンベロープ形式を標準とし、GameManager でコマンド分配する。
- 判断理由: 仕様書 6.3 のコマンド一覧へ直接マッピングでき、未実装コマンドも `OnError` で明示通知できるため運用時の可観測性が上がる。
- 採用しなかった代替案: 文字列プレフィックス（例: `cmd:xxx|...`）は暫定対応が容易だが、拡張時に破綻しやすいため不採用。
- 影響範囲: CoaXisViewer の IPC 受信処理（GameManager）、送信ユーティリティ（IpcClient）、契約定義（src/global）。
- 実装/運用手順: Editor 側は `{"command":"ApplyCameraPreset","payload":{...}}` 形式で送信し、Viewer からの通知も同形式で受ける。
- 検証方法: `dotnet build .\\CoaXis.sln` 成功と、NamedPipe テストで `ApplyCameraPreset` / `Focus` の処理確認。
- 関連ファイル/関連仕様: docs/specification/specification_integrated.md（6.3 IPC連携）, CoaXisViewer/src/global/ViewerIpcProtocol.cs, CoaXisViewer/src/viewer/GameManager.cs
- 備考: 既存のプレーン文字列メッセージは互換ログとして受理せず無視する設計（移行期間のみ）。

---

### [2026-06-01] C#クラス内構成の標準順序とregion名を統一

- 背景: PoC段階でクラス内の記述順・region名が揺れており、探索性とレビュー効率が低下していた。
- 問題: 開発者ごとに `Event Handlers` / `Input Handling` / `State Management` など命名が分かれ、同種メソッドの所在を予測しづらい。
- 判断: クラス内順序を `Signals -> Fields -> Properties -> Lifecycle -> Public Methods -> Internal Helpers` に統一し、region名も同名へ限定する。
- 判断理由: Godot系/非Godot系を問わず共通運用でき、学習コストとレビューコストを下げられる。
- 採用しなかった代替案: クラスごと自由命名は柔軟性が高いが、長期保守で認知負荷が上がるため不採用。
- 影響範囲: `CoaXisViewer/src/**/*.cs` および `tests/**/*.cs` のクラス内構成。
- 実装/運用手順: 変更対象クラスに手を入れる際、優先度高でregion名と順序を本規約へ合わせる。
- 検証方法: `dotnet build .\\CoaXis.sln` 成功、および `#region` 名の検索で標準語彙のみを確認。
- 関連ファイル/関連仕様: `docs/rules/implementation-conventions.md`
- 備考: 機能変更と構造整理の同時実施はレビュー難度を上げるため、原則分離する。

---

### [2026-06-01] 文字エンコーディング事故の再発防止フローを標準化

- 背景: region 名の一括置換作業中に、PowerShell 上書きで UTF-8 コメントが破損し、改行崩壊と構文エラーが発生した。
- 問題: 文字化けがコメントだけでなくコード行にも混入し、`if` や `return` がコメントと同一行化してビルド失敗を招いた。
- 判断: 「予防ルール + 自動検知 + 事故時標準フロー」を docs とタスクへ反映し、再発時に同じ調査を繰り返さない運用へ移行する。
- 判断理由: 人手レビューのみでは見落としが発生しやすく、作業者依存を避けるには機械チェックの常設が必要。
- 採用しなかった代替案: 事故ごとに ad-hoc に修復する運用は初動が遅く、再発率が下がらないため不採用。
- 影響範囲: `CoaXisViewer/src/**/*.cs` のコメント整備、`.vscode/tasks.json`、`scripts/quality/check-mojibake.ps1`、`docs/rules`。
- 実装/運用手順: `check: mojibake` 実行後に `dotnet build .\\CoaXis.sln` を実施し、最終確認は `verify: build + encoding` を使用する。
- 検証方法: 文字化けパターン検索ゼロとビルド成功を確認する。
- 関連ファイル/関連仕様: `docs/rules/encoding-incident-prevention.md`, `scripts/quality/check-mojibake.ps1`, `.vscode/tasks.json`
- 備考: 検知漏れが見つかった場合は、同日中に検知パターンを追加する。

---

### [2026-06-01] C#のregion運用をEvents中心へ統合

- 背景: 既存規約ではイベント処理やユーザー操作起点の処理を `Internal Helpers` に集約しており、呼び出し起点の探索に時間がかかっていた。その後 `User Actions` / `Event Handlers` 分離、`Event Handlers` 一本化を経て、最終的に `Events` へ名称統一する段階的見直しを実施した。
- 問題: region 名と責務の分け方が短期間で変遷し、履歴が分散したことで「現在の正」と「運用手順」が読み取りづらくなっていた。
- 判断: 外部起点の処理（シグナル/イベント購読コールバック、ユーザー操作起点の直接呼び出し処理）は `Events` region へ統一し、標準順序は `Signals -> Fields -> Properties -> Lifecycle -> Events -> Public Methods -> Internal Helpers` とする。
- 判断理由: 名称と責務を一意にしつつ、呼び出し起点の処理を `Public Methods` より先に配置することで、イベント起点の流れを先に追える構成にできる。
- 採用しなかった代替案: `User Actions` と `Event Handlers` の分離運用、および `Event Handlers` 名継続は、表現力はあるが配置判断と命名の揺れを生みやすいため不採用。
- 影響範囲: `CoaXisViewer/src/**/*.cs` および `tests/**/*.cs` のクラス内region構成・命名。
- 実装/運用手順: クラス更新時は `#region Events` を使用し、`#region User Actions` / `#region Event Handlers` は新規作成しない。順序は `Signals -> Fields -> Properties -> Lifecycle -> Events -> Public Methods -> Internal Helpers` を適用する。
- 検証方法: `#region User Actions` と `#region Event Handlers` の残存がないこと、`#region Events` が使用されていること、必要に応じて `dotnet build .\\CoaXis.sln` で整合確認する。
- 関連ファイル/関連仕様: `docs/rules/implementation-conventions.md`
- 備考: 同日付の「C#のregion運用へUser ActionsとEvent Handlersを正式追加」「C#のユーザー起点処理をEvent Handlersへ一本化」「C#の外部起点処理region名をEventsへ統一」は本ログへ統合した。

---

### [2026-09-06] PickByShape の形状クエリをバッチ取得へ変更

- 背景: モデル数が増えると範囲選択が遅くなっていた。
- 問題: `PickByShape` が `IntersectShape` を1件取得するたびに再実行し、ヒット数に比例して物理クエリと除外配列の再構築が増えていた。
- 判断: 1回の `IntersectShape` で最大32件を取得し、取得したRIDを次のクエリの除外対象へ追加する方式へ変更した。同一 `ModelId` の複数Colliderは、範囲選択の契約に合わせて最初の1件へ集約する。
- 判断理由: 物理クエリの呼び出し回数を減らしつつ、バッチ上限に達した場合は次のクエリを継続することで、結果欠落を避けられるため。
- 採用しなかった代替案: 全ヒット数を推定して巨大な `maxResults` を一度だけ指定する方法は、Godot/Jolt側の上限や負荷が不明で、結果欠落と一括計算コストのリスクがあるため採用しなかった。
- 影響範囲: `PickUtility.PickByShape` の返却結果は同一モデルにつき1件となる。形状クエリで位置・法線・距離が取得できない制約は変更しない。
- 実装/運用手順: バッチサイズは現在32。大量モデルの実シーンで処理時間、クエリ回数、取得したModelId集合を計測し、必要に応じてサイズを調整する。無効RIDを含む結果は無限ループ防止のため、そのバッチで処理を終了する。
- 検証方法: 複数モデル、同一モデルの複数Collider、バッチ上限超過、初期除外RID、ヒットなし、至近距離の矩形選択を確認し、`check: mojibake` と `dotnet build .\\CoaXis.sln` を実行する。
- 関連ファイル/関連仕様: `CoaXisViewer/src/core/util/PickUtility.cs`, `CoaXisViewer/scenes/viewport/ViewportInteractionHandler.cs`, `CoaXisViewer/src/application/domain/selection/SelectionService.cs`, `TODO.md`
- 備考: `SelectionService` の既存 `Distinct` は防御的処理として残す。
