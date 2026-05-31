# CoaXis 統合仕様書（再設計案）

最終更新: 2026-05-31
対象版: Draft v0.9

---

## 第1章 システム定義

### 1.1 システム名
CoaXis

### 1.2 システム概要
CoaXisは、複数アプリケーションと単一データベースで構成される、組立生産技術向けの3D中心情報基盤である。
CATIA V5のMBD情報を起点に、工程計画・作業情報・リソース情報を統合管理し、分析およびMES連携までを一気通貫で扱う。

本システムの主運用は以下の分担で成立する。
- CATIA: 設計・形状情報の起点
- CoaXis: 工程計画・作業情報・変更追従の業務基盤
- MES: 実行系システム（CoaXisPublisher出力を受けて登録）

### 1.3 背景
- 既存業務は図面・表計算・属人運用に分散し、変更追従と再利用が難しい
- CATIA V5内情報を業務システムで再活用する仕組みが不足している
- 現場利用まで含めた3D可視化とデータ連携が分断されている

### 1.4 目的
- 3Dモデルを中心にした工程情報管理を実現する
- 製造業におけるPLM代替となる社内技術基盤を構築する
- 工程計画・差分確認・作業指示連携を標準化する
- MES登録の前処理を自動化し、登録工数を削減する

### 1.5 開発方針
- 主技術は .NET 8.0 / C#
- 当面はVBAを併用（既存資産流用、Excel/CATIA連携、移行リスク低減）
- 技術課題解消後、Extractor/Modelerを段階的にC#化する

### 1.6 スコープ
本仕様書は以下を対象とする。
- CoaXis全体アーキテクチャ
- 各サブシステム仕様
- ドメインモデル
- API/IPC連携
- データベース方針
- 運用・非機能要件

本仕様書は詳細DDL、画面ピクセル単位設計、個別RPAシナリオ実装手順を対象外とする。

---

## 第2章 利用者・権限

### 2.1 利用者区分
- 管理者
- ユーザー（生産技術者）
- 閲覧者（将来の現場参照者含む）

### 2.2 管理者
- データベース直接操作権限を持つ唯一のロール
- マスタ管理、ユーザー管理、環境保守、バックアップ運用を担当
- 本番稼働後は運用移管を前提に権限委譲計画を持つ

### 2.3 ユーザー
- 主対象は生産技術者約30名
- Editorを中心に計画作成・編集・差分確認を実施
- 編集機能はユーザー登録済みアカウントのみ許可

### 2.4 閲覧者
- 将来30名程度を想定
- 閲覧は接続権限ベースで許可
- 編集は不可

### 2.5 権限制御原則
- 書き込み操作は認証必須
- 読み取りは用途別に匿名許可可能（公開範囲を限定）
- 最終的なデータ整合性責任はAPIが負う

---

## 第3章 全体アーキテクチャ

### 3.1 構成要素
- CoaXisExtractor（CATIA VBA）
- CoaXisDatabase（単一DB）
- CoaXisApi（業務API）
- CoaXisCore（共通ドメイン）
- CoaXisEditor（WinForms）
- CoaXisViewer（Godot C#）
- CoaXisAnalyzer（DWH/EUC）
- CoaXisModeler（CATIA VBA）
- CoaXisPublisher（MES出力）

### 3.2 基本原則
- DBアクセスは原則API経由
- Editorが業務操作中心、Viewerが3D描画中心
- GLB実体はファイルサーバー管理、DBはメタ情報管理
- サブシステム間の契約はAPI/IPCで明文化

### 3.3 論理データフロー
1. ExtractorがCATIAからDocumentMasterを抽出し登録
2. Editorが計画を作成し、Viewerへ可視化指示
3. AnalyzerがEUC向けデータを定期生成
4. ModelerがCATIAへ工程情報を書き戻し
5. PublisherがMES登録用データを出力

### 3.4 Editor-Viewer責務分離
- Editor: データ操作、マスタ管理、業務UI、API通信
- Viewer: GLB描画、選択/ハイライト、カメラ・表示同期
- IPC: NamedPipe双方向メッセージ

---

## 第4章 サブシステム仕様

### 4.1 CoaXisExtractor
目的:
- CATIA V5 MBDから業務情報を抽出し、CoaXisの入力データを生成する

責務:
- フォルダ単位抽出（Document単位）
- Product/Instance/Component抽出
- Point/Surface/Annotation/Property抽出
- wrl出力とglb変換
- Resource登録

入力:
- CATPart/CATProduct
- 抽出対象フォルダ
- 必要時のユーザー選択（最上位Assembly、組図CATPart）

出力:
- DocumentMaster登録データ
- glbファイル
- 抽出ログ

制約:
- DB直接接続禁止（API経由のみ）
- 失敗時は部分成功を許容し、ログで再処理可能性を担保

将来:
- C#版への移行
- 差分抽出・並列化

### 4.2 CoaXisDatabase
目的:
- CoaXisの唯一の業務データ集約基盤

責務:
- トランザクションデータおよびマスタデータの保持
- バックアップ・最適化・監査対応

方針:
- SQL Server前提だがDB置換可能な設計
- 本番/検証環境を分離
- GLBはDB外管理（パス/ハッシュ/更新時刻のみDB保持）

### 4.3 CoaXisApi
目的:
- 全クライアントのデータアクセス・整合性・認可制御の単一入口

責務:
- CRUD API提供
- 原子性保証（複数テーブル更新）
- バージョン互換性チェック
- 監査ログ記録

互換性判定:
- 互換あり: 許可
- 条件付き互換: 警告付き許可
- 非互換: 拒否し更新を要求

### 4.4 CoaXisCore
目的:
- 全アプリ共通の業務オブジェクトモデルを提供

方針:
- ドメインの語彙統一
- 参照関係の明確化
- DTOとDomainを分離

### 4.5 CoaXisEditor
目的:
- 生産技術者向け主業務アプリケーション

主要機能:
- Document管理
- Plan管理
- Process/Task管理
- Plan編集
- 差分確認
- Viewer連携

通信:
- API通信はEditorが担当
- ViewerとのIPCはEditorがServer

### 4.6 CoaXisViewer
目的:
- 3D可視化と操作反映を担う描画アプリケーション

主要機能:
- GLB動的ロード
- TreeView連動
- ハイライト/表示切替/カメラプリセット
- 選択イベント返却

制約:
- DBへ直接接続しない
- 業務操作の主系はEditor側

### 4.7 CoaXisAnalyzer
目的:
- 本番DB依存を避け、分析しやすい形でEUCデータを提供

提供形態:
- DWHスキーマ
- CSV定期出力

用途:
- 作業負荷分析
- 差分統計
- 改善活動支援

### 4.8 CoaXisModeler
目的:
- Plan情報をCATIAへ反映し、中間製造モデルを生成

主要機能:
- 工程情報取得
- 色分け、注記付与、表示制御
- XVL変換前提モデル出力

### 4.9 CoaXisPublisher
目的:
- MES登録向けM-BOM/BOPデータを生成して出力

主要機能:
- Plan/Process/TaskをMES向けに整形
- JSON出力
- RPA連携前提の出力規約維持

---

## 第5章 ドメインモデル仕様

### 5.1 モデル群
- DocumentMaster
- DocumentTransaction
- PlanTransaction
- ProcessMaster
- TaskMaster
- ViewTransaction

### 5.2 DocumentMaster
- Document: 抽出単位（図面フォルダ）
- Product: CATPart/CATProduct実体
- Instance: 使用位置・階層表現
- Component: 構成部品リンク
- Point/Surface/Annotation/Property: 要素情報
- Drawing/Resource: 参照資料・治工具

設計要点:
- ProductはDocument内PartNumber一意
- Instanceは親子階層を保持

### 5.3 DocumentTransaction
- Airplane: 号機単位の管理軸
- EffectiveDocument: 号機とDocumentの適用関係

設計要点:
- 有効期間で現行Documentを判定
- 差分発生時に適用更新可能

### 5.4 PlanTransaction
- ProductPlanを親に、対象要素別Planを持つ
- Operation系はProcessMasterを参照
- UsingResource/UsingDrawingで周辺情報を接続

設計要点:
- Sequenceで実行順を管理
- バージョンで履歴を保持

### 5.5 ProcessMaster
- Process: 工程の束
- Operation: 工程内作業

設計要点:
- Processの入れ子を許容
- Operationの標準化で再利用を促進

### 5.6 TaskMaster
- ComponentTask
- PointTask
- SurfaceTask
- AnnotationTask

設計要点:
- 対象別に標準作業を定義
- Operation自動割り当ての起点として利用可能

### 5.7 ViewTransaction
- PlanView
- ViewProductPlan
- ViewPreset
- CameraPreset

設計要点:
- Plan情報の見せ方をデータとして保持
- Editor-Viewer間で再利用可能

---

## 第6章 連携仕様

### 6.1 API連携（CoaXisApi）
通信:
- REST
- JSON
- HTTPS

認証:
- JWTベース
- 読み取り限定匿名アクセスを一部許可

責務:
- 入力検証
- 認可判定
- トランザクション制御
- 監査ログ

### 6.2 APIカテゴリ
- DocumentMaster系
- DocumentTransaction系
- PlanTransaction系
- ProcessMaster系
- TaskMaster系
- ViewTransaction系
- Publisher系
- System系（互換性確認）

### 6.3 IPC連携（Editor-Viewer）
方式:
- NamedPipe双方向

EditorからViewer:
- LoadModel
- Highlight
- Select
- ApplyViewPreset
- ApplyCameraPreset
- ClearHighlight
- Focus
- Hide
- Show

ViewerからEditor:
- OnSelect
- OnHover
- OnLoaded
- OnError

### 6.4 バージョン互換ルール
- クライアント起動時にバージョン照会
- 最低要求未満は接続拒否
- 推奨未満は警告

---

## 第7章 データベース仕様（方針レベル）

### 7.1 設計原則
- 3NFを基本とする
- 外部キーで整合性担保
- 破壊的カスケード削除は原則禁止

### 7.2 ID方針
- GUIDを全体で統一利用

### 7.3 性能設計
- 外部キー列へ索引
- 高頻度検索列へ追加索引
- 大容量運用を見据えた拡張余地を確保

### 7.4 GLB管理
- 実体はファイルサーバー
- DBは参照メタ情報のみ
- 改ざん検知用ハッシュを保持

### 7.5 環境分離
- 本番
- 検証/デバッグ

---

## 第8章 非機能要件

### 8.1 可用性
- 業務時間帯での安定稼働
- 障害時の復旧手順を運用標準化

### 8.2 性能
- 大規模Document処理に耐える
- Viewer操作は体感遅延を最小化
- バッチ系は夜間実行を基本

### 8.3 セキュリティ
- 通信はTLS前提
- 認証/認可をAPI集中管理
- 操作ログ・変更履歴を保持

### 8.4 保守性
- ドメインをCoaXisCoreで統一
- コンポーネント責務分離を徹底
- 移行容易性（VBAからC#）を担保

### 8.5 監査性
- 誰が、いつ、何を変更したかを追跡可能にする

---

## 第9章 運用・保守

### 9.1 日次運用
- DBバックアップ
- ログ監視
- 出力ファイル監視

### 9.2 定期運用
- 索引最適化
- 容量監視
- 不整合データ点検

### 9.3 障害対応
- API停止時の切り分け手順
- Viewer接続断の復旧手順
- 抽出/出力失敗時の再実行手順

### 9.4 権限運用
- 管理者権限付与は申請制
- 退職/異動時の即時剥奪

---

## 第10章 移行・ロードマップ

### 10.1 フェーズ計画
フェーズ1:
- Extractor/ModelerのVBA運用で全体フロー確立
- Editor-Viewer-API-DBの基盤安定化

フェーズ2:
- Analyzer/Publisherの運用定着
- 差分運用・標準化強化

フェーズ3:
- Extractor/ModelerのC#移行
- モデル変換と連携の内製比率を上げる

### 10.2 技術的負債の管理
- VBA依存箇所を棚卸し
- 置換優先度を業務影響で決定
- API契約を先行固定し移行コストを低減

---

## 第11章 未決事項

- CATIA抽出ルールの命名規約（自動判別条件）
- GLB変換ツールの標準採用候補
- MES登録項目の最終マッピング
- RPA例外処理の標準テンプレート
- Document差分がPlanに与える影響判定ルール

---

## 第12章 受け入れ基準（初版）

### 12.1 業務要件
- ExtractorでDocumentMasterを登録できる
- EditorでPlanを作成し保存できる
- Viewerで対象要素の可視化連動ができる
- PublisherでMES向けJSONを出力できる

### 12.2 品質要件
- 重大障害時に復旧手順で再開できる
- 主要操作に監査ログが残る
- 非互換クライアントをAPIが拒否できる

### 12.3 運用要件
- バックアップ/復元を検証済み
- 権限管理手順が文書化済み

---

以上。