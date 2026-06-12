# Viewerに実装予定の機能

## 選択関係
選択しているツリーやモデル状態を管理するSlections実装
モデルの選択
矩形での複数選択
Ctrlを押しながら複数選択
選択した座標と垂直方向の取得

## Tree関係
Node3Dのみを

## カメラ関係
選択した面に垂直機能
    Vector3をQuaternionに変換する際、現状のカメラの上方向を引き継ぐ
複数ノードのフィット機能
    Fit機能はNodeを配列で受け取ることとする、選択処理との兼ね合いがあるためSelectionManager実装後に
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
動かせるフローティングUI
オーバーレイのような透過する仕様ツリー

ViewState
    ProductPlan
        ComponentPlan
        JointPlan
        SealantPlan
        ShimPlan
        BondPlan
        AnnotationPlan
        ResourcePlan