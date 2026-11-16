# CoaXis

CoaXis は、3D モデルの可視化と事前アセット生成を分離して運用するプロジェクトです。このリポジトリでは主に Viewer・Protocol・IPC Tester を開発します。要件の変更時は、実装・仕様・運用ルールを同時に揃えます。

## プロジェクト

- CoaXis.Viewer
  - 3D 表示・操作・IPC 連携を担当する Godot プロジェクト
- CoaXis.Protocol.Viewer
  - 共通契約と IPC で使う DTO / 定義を管理するプロジェクト
- CoaXis.IpcTester
  - JSON/CSVや操作テストデータを使い、Viewerへモデル集合の読込・追加・削除・変更・読み出し要求を送り、要求と応答を確認するWindowsデスクトップツール
- CoaXis.Ui
  - WinFormsアプリケーション向けの共通UIライブラリ（詳細は [README](CoaXis.Ui/README.md)）
- CoaXis.SceneBuilder
  - 事前アセット生成プロジェクト。glbをscn形式に変換し、Viewer で利用可能な形式に変換する役割を持つ