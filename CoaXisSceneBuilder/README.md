# CoaXisSceneBuilder

CATIA V5 から抽出されたモデル中間データを読み込み、[CoaXisViewer](../README.md) 用の最適化済み 3D シーンファイル（`.scn`）を事前ビルド・保存するための Godot C# プロジェクトです。

---

## 1. 概要と役割

CoaXis システムでは、CATIA から出力されたモデルを高速・軽量に描画・利用するために、アセットの事前変換処理を行います。

将来的には CoaXisViewer 側から動的アセット構築機能を完全に分離し、CoaXisViewer は事前に生成された `.scn` の描画のみに専念するアーキテクチャへと移行します。本プロジェクト（`CoaXisSceneBuilder`）はそのアセット事前生成処理を専門に担当し、各種中間データを統合して Godot の単一 `PackedScene`（`.scn`）へと焼き込んで保存します。

---

## 2. 処理フローと入力・出力

```
[CATIA V5 (wrl1ファイル等)]
          │
          ▼ 前処理
  ┌───────┴────────┬──────────────────┐
  │                │                  │
  ▼                ▼                  ▼
VisualGlb       ColliderGlb      LineSetJson
(.glb)          (.glb)           (.json)
  │                │                  │
  └───────┬────────┴──────────────────┘
          │
          ▼ 読み込み・統合
  【CoaXisSceneBuilder】
   ・視覚メッシュ読み込み (VisualGlbLoader)
   ・衝突形状生成 (ColliderGlbLoader)
   ・ラインセット解析・チューブメッシュ生成 (LineSetJsonParser)
   ・PackedScene (SceneBuilder) にバインド
          │
          ▼ シーン焼き込み・出力
      Model.scn (バイナリ形式)
          │
          ▼ 参照・描画
   【CoaXisViewer】
```

### 2.1 入力ファイル（事前準備）
CATIA から抽出された `wrl1` ファイル等を元に、事前に以下の 3 つのファイルが用意されます。

1. **`VisualGlb` (`.glb`)**: 3D モデルの視覚表示用メッシュデータ。
2. **`ColliderGlb` (`.glb`)**: 3D モデルの衝突判定（レイキャスト・選択判定）用メッシュデータ。
3. **`LineSetJson` (`.json`)**: モデルに含まれる線分群（配管・配線・中心線等）の座標・属性情報を保持する JSON データ。

### 2.2 CLI/本番実行時の入力仕様 (JSON Schema)

本番運用では、ビルド指示 JSON の**絶対パスを1件**渡して実行します。JSON は配列であり、単一または複数アセットを一括ビルドできます。JSON の拡張子は `.json`、ファイルは実在している必要があります。複数の JSON を渡すことはできないため、複数アセットは1つの JSON 配列へまとめます。

### 2.2.1 Shell から実行する

開発中は Godot 本体から、以下のようにヘッドレスで実行します。`--` より後ろの1引数がアプリケーションへ渡されます。

```powershell
& 'C:\Development\Godot\Godot_v4.6.3-stable_mono_win64.exe' `
  --headless `
  --path 'C:\work\CoaXisSceneBuilder' `
  -- `
  'C:\work\jobs\input.json'
```

export 済みアプリは、コンソール出力を確認できる `CoaXisSceneBuilder.console.exe` を使用します。

```powershell
.\build\CoaXisSceneBuilder.console.exe `
  --headless `
  -- `
  'C:\work\jobs\input.json'
```

### 2.2.2 ドラッグ＆ドロップで実行する

エクスプローラーでビルド要求 JSON ファイルを `CoaXisSceneBuilder.console.exe` へドラッグ＆ドロップします。Windows が JSON の絶対パスを引数として渡し、ビルド結果とエラーをコンソールに表示します。

- ドロップできる JSON は1回につき1件です。
- `CoaXisSceneBuilder.exe` ではなく、実行結果を確認できる `CoaXisSceneBuilder.console.exe` をドラッグ先に使用します。
- JSON を指定せずにダブルクリックした場合、コンソールに shell とドラッグ＆ドロップの使用方法を表示して終了コード `1` で終了します。

成功時の終了コードは `0`、JSON の検証・読込、アセット読込、保存のいずれかに失敗した場合は `1` です。

`visualGlb`、`colliderGlb`、`lineSetJson`、`outputScn` にはすべて絶対パスを指定できます。プロジェクト内で試す場合に限り、従来どおり `res://` またはプロジェクト相対パスも使用できます。

```json
[
  {
    "visualGlb": "C:\\work\\input\\model_visual.glb",
    "colliderGlb": "C:\\work\\input\\model_collider.glb",
    "lineSetJson": "C:\\work\\input\\model_lineset.json",
    "outputScn": "C:\\work\\output\\model.scn",
    "visualUnshaded": true
  }
]
```

#### フィールド定義
| フィールド名 | 型 | 必須 | 概要 |
| :--- | :--- | :---: | :--- |
| `visualGlb` | string | 任意 | 視覚用 GLB ファイルのパス |
| `colliderGlb` | string | 任意 | コライダー用 GLB ファイルのパス |
| `lineSetJson` | string | 任意 | ラインセット JSON ファイルのパス |
| `outputScn` | string | **必須** | 出力先 `.scn` シーンファイルのパス |
| `visualUnshaded` | boolean | 任意 | `true` の場合、視覚メッシュのマテリアルを Unshaded に設定 |

### 2.2.3 並列ビルド（`MaxParallelism`）

`settings/builder-settings.json` の `MaxParallelism` に 2 以上を指定すると、入力 JSON 内の複数リクエストを `MaxParallelism` 個のチャンクへ均等分割し、自分自身の実行ファイルを子プロセスとして同時起動して並列ビルドします。

- Godot のノード生成・`PackedScene.Pack()`・`ResourceSaver.Save()` は main thread 専用 API のため、同一プロセス内の並列化はできません。そのため子プロセスをそれぞれ独立した Godot プロセスとして起動する方式を採っています。
- `MaxParallelism` の既定値は `1`（従来通りの逐次実行）です。
- 子プロセスは環境変数 `COAXIS_SCENEBUILDER_WORKER=1` を持って起動されるため、さらに再分割されることはありません。
- 分割済みチャンクの入出力用一時ファイルは `%TEMP%\CoaXisSceneBuilder\batch_*` に作成され、全チャンク完了後に自動削除されます。
- 子プロセスの標準出力・標準エラーは `[chunk0]` のようなプレフィックス付きで親プロセスのコンソールへ転送されます。
- リクエストが1件のみ、または `MaxParallelism` が `1` 以下の場合は並列化されず、従来通り逐次実行されます。

### 2.3 出力ファイル
- **`Model.scn`**: Godot のバイナリ形式シーンファイル。ビルド成功時に終了コード `0`（完了）を返します。

---

## 3. `.scn` (バイナリ形式) を採用する理由

Godot のシーン保存形式には、テキスト形式の `.tscn` とバイナリ形式の `.scn` が存在します。

- **ファイル容量の削減**: 複雑な 3D メッシュ情報や大量の頂点データを含むシーンを `.tscn` (テキスト形式) で保存すると、ファイルサイズが極めて大きくなります。`.scn` (バイナリ形式) を採用することでファイルサイズを極力小さく抑えられます。
- **高速なロード速度**: バイナリ形式のため、閲覧・描画時の読み込み（パース処理）が大幅に高速化され、メモリ消費と初期化時間を削減できます。

---

## 4. 主要クラス構成

| クラス名 | 役割 |
| :--- | :--- |
| `SceneBuilder` | 全体を統括するビルダー。視覚メッシュ・コライダー・ラインセットを順次読み込み、単一の `PackedScene` に焼き込んで出力する |
| `VisualGlbLoader` | 視覚用 `.glb` ファイルをロードし、メッシュノードおよびマテリアル（Unshaded設定等）を構築してシーンに追加する |
| `ColliderGlbLoader` | コライダー用 `.glb` ファイルから面（頂点データ）を収集し、`StaticBody3D` 配下に `ConcavePolygonShape3D` を再構築する |
| `LineSetJsonParser` | `LineSetJson` (`.json`) を解析し、チューブ形状の 3D メッシュ（`ArrayMesh`）を生成してシーンに追加する |
| `AssetPathResolver` | アセット読み込み用のファイルパス解決ユーティリティ |
| `SceneBuildProcessOrchestrator` | `MaxParallelism` に応じてリクエストを分割し、自プロセスを子プロセスとして並列起動・結果集計を行う |
| `SceneBuildRequestSplitter` | リクエストリストをチャンクへ均等分割し、一時JSONファイルへ書き出すユーティリティ |

---

## 5. 関連ドキュメント

- 統合仕様書: [docs/specification/specification_integrated.md](../docs/specification/specification_integrated.md)
- アセットビルドガイド: [docs/guides/asset-builder-guide.md](../docs/guides/asset-builder-guide.md)
- ルート README: [README.md](../README.md)

---

## 6. Windows 実行ファイルの export

Windows 向けには、Godot **4.6.3 .NET 版**と同バージョンの Windows export templates を使用します。export 後は `.exe` 単体ではなく、生成された `build` 配下の DLL・PCK を含むファイル一式を配布してください。

`settings/builder-settings.json` は export 成果物へ同梱され、実行時には PCK 内の `res://settings/builder-settings.json` として読み込まれます。export 後に設定値を変更する場合は、JSON を編集した後に再 export してください。

プロジェクト直下で次を実行します。既存の export 成果物を上書きする場合だけ `-Force` を追加します。

```powershell
.\scripts\export-windows.ps1 `
  -GodotExecutable 'C:\Development\Godot\Godot_v4.6.3-stable_mono_win64.exe'
```

出力先は `build\CoaXisSceneBuilder.exe` と、コンソール出力用の `build\CoaXisSceneBuilder.console.exe` です。実行方法は「2.2 CLI/本番実行時の入力仕様」を参照してください。

スクリプトは export 前に C# solution をビルドします。Godot 実行ファイルまたは export templates が不足する場合は export を中断し、原因を表示します。
