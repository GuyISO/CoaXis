# Viewerに実装予定の機能

## これからやる

VFXをModelNodeに紐づける
    ModelNodeにVFXを紐づけて、ModelNodeの座標にVFXを表示する機能を実装する

ModelNode固有アクションの実行
    ダブルクリックから？

選択セット・表示セットの保存・復元
    選択セット・表示セットを保存して、後で復元する機能を実装する、プランコピーしても引き継がれるように、ProductPlanの属性として保存する？

## Applicationに追加
infra
    Applicationのバージョン管理・IPC通信時のチェック
    IPC

## 3D表示関係
モデルのハイライト方法
    SelectionはMaterialOverrideを使用しているが、3Dモデルを目立たせて作業指示にもなるような方法を考える
点群座標へのVFX配置
    GodotのVFX Graphを使用して、点群座標にエフェクトを配置する機能を実装する
3Dから2Dに投影してテキストを表示
    3D空間の特定の位置にテキストを表示するために、カメラ投影を使用して2Dスクリーン座標に変換し、UI上にテキストを配置する機能を実装する
モデルの透明度設定

## UI
設定画面
    設定変更したら、Settings.jsonに保存する
右クリックメニュー
    右クリックで表示されるコンテキストメニューを実装する
    メニュー項目の追加や削除が汎用処理で容易に行えるようにする
モデルツリー
    左側はルートモデルツリー専用領域にする、文字やペインの大きさを変更できるようにする
各種パネル
    フローティングorタブにドッキングして使用、画面右側で縦タブにしたいが標準機能ではない？

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
AlignNormalなどのトグルボタン
    自分で選択解除しても内部的には解除されていない、Selectionもだめ、Measurementはなぜかいい感じ


## Host側の機能
号機/OperationでのVFXの切り替え


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
Modelの移動機能
    選択したモデルを移動できるようにする、移動可否はModelの属性で制御する
    ワールド移動、ローカル移動、Snapping機能は欲しい
モデルロード進行中
    クリア・再読み込みするとうまくいかない
wrlの読み込み
    内容全然わかっていないので理解する