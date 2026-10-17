# CoaXis

CoaXis は、3D モデルの可視化と事前アセット生成を分離して運用するプロジェクトです。このリポジトリでは主に Viewer・Protocol・IPC Tester を開発します。要件の変更時は、実装・仕様・運用ルールを同時に揃えます。

## プロジェクト

- CoaXisViewer
  - 3D 表示・操作・IPC 連携を担当する Godot プロジェクト
- CoaXisProtocol
  - 共通契約と IPC で使う DTO / 定義を管理するプロジェクト
- CoaXisIpcTester
  - JSON/CSVを読み込み、ViewerへNamed PipeでLoadModelを送り、要求と応答を確認するWindowsデスクトップツール

## 開発

```powershell
dotnet build .\CoaXis.sln
# Viewerを起動してから実行
dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj
```

## ドキュメント

- [Viewer・Protocol仕様](docs/specification/specification_integrated.md)
- [C#実装規約](docs/rules/implementation-conventions.md)
- [IPC Tester手順](docs/guides/viewer/IPC_TESTER_GUIDE.md)
