# Viewerに実装予定の機能

## 選択関係
矩形での複数選択
Ctrlを押しながらの複数選択
レイキャストの貫通
    現状は最初にヒットしたオブジェクトのみ選択される仕様になっているが、レイキャストの貫通を実装して複数オブジェクトから選択できるようにする

## Tree関係
双方向辞書の作成

## カメラ関係
複数ノードのフィット機能
    Fit機能は単一のNode3DもしくはNodeを配列(IEnumerable)で受け取るオーバーロードを持つこととする
選択ノードの表示/非表示切替
    Godotの表示非表示切り替えで実装する

## モデル関係
選択状態のモデルの色変更
    Materialで制御？
    色以外で3Dモデルを目立たせる方法を用意
モデルの動的ロードとツリーへの反映
点群座標への点モデル配置
    VectorからQuaternionに変換どうする？とりあえず直変換で
点群座標をカメラ投影しての点配置
    とりあえず3Dモデルで始める、
点群座標へのVFX配置
モデルの

## UI
オーバーレイのような透過する仕様ツリー
Window管理
Dockable Window

ViewState
    ProductPlan
        ComponentPlan
        JointPlan
        SealantPlan
        ShimPlan
        BondPlan
        AnnotationPlan
        ResourcePlan