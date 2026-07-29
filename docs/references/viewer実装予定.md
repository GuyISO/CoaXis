# Viewerに実装予定の機能

## すぐやる

カメラ状態を保存・復元する機能を追加、UIに一覧で保存して、選択して復元できるようにする、ついでにUUID v7で識別子を付与して、
外部ファイルかレジストリにも保存してアプリケーションを閉じても復元できるようにする

## Applicationに追加
infra
    Save&Load機能
        カメラ状態
        モデル状態
        設定ファイル
    IPC

domain
    Modelの移動Snapping機能
    VFX配置機能、Modelとの紐づけもする

## 3D表示関係
モデルのマテリアル変更以外のハイライト方法
    色やMaterial以外で3Dモデルを目立たせる方法を用意、発光点滅など？少なくともSelectionでのマテリアル変更とは別の方法でハイライトしないと破綻する
点群座標へのVFX配置
    GodotのVFX Graphを使用して、点群座標にエフェクトを配置する機能を実装する
3Dから2Dに投影してテキストを表示
    3D空間の特定の位置にテキストを表示するために、カメラ投影を使用して2Dスクリーン座標に変換し、UI上にテキストを配置する機能を実装する

## UI
設定画面
    設定変更したら、Settings.jsonに保存する
貫通レイキャスト
    一覧と選択UI
右クリックメニュー
    右クリックで表示されるコンテキストメニューを実装する
    メニュー項目の追加や削除が容易に行えるようにする

## ユーティリティ
Modelの移動機能
    選択したモデルを移動できるようにする、移動可否はModelの属性で制御する
    ワールド移動、ローカル移動、Snapping機能は欲しい

## その他
将来的にCoaXis用ビューワーソフトとして使用できるように、以下のモデル構造に対応させる

RootModel
    ViewState
        ProductPlans
            ProductPlan(Assembly)
                ComponentPlans
                    ComponentPlan
            ProductPlan(Part)
                PointPlans
                    PointPlan
                LinePlans
                    LinePlan
                SurfacePlans
                    SurfacePlan
                AnnotationPlans
                    AnnotationPlan
                ParameterPlans
                    ParameterPlan
                ResourcePlans
                    ResourcePlan
        ViewPoints
            ViewPoint

## 細かい修正
測定機能
    表示されるラベルと線を豪華にする、モデルに埋まっても見えるようにしたい
Tree
    階層を跨いで複数選択するのができない

## 先延ばしにしたやつとか
矩形での複数選択
    完全に含むもののみ選択する機能は難しくて先送りにしてある
オーバーレイのような透過する仕様ツリー
    Godotの仕様上むり？検討する
Dockable Window
    各UIをDock可能なContainerの入れ子として使えるようにする
点群座標をカメラ投影しての点配置
    とりあえず3Dモデルで始めるのでまだ先
断面表示
    需要があれば実装する、今のところは必要ない
干渉・クリアランスチェック
    需要があれば実装する、今のところは必要ない