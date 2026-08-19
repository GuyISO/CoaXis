---
name: godot-node-lifecycle-rules
description: "Use when: Godot C# の Node 継承クラスで _Ready/_ExitTree の実装・修正、イベント購読解除、base._ExitTree() 追加、Lifecycle/Events/Public API の並びをそろえるとき"
---

# Godot Node Lifecycle Rules

このスキルは、Godot C# の Node 継承クラスにおけるライフサイクル実装を統一するための規約。

## Scope

- 対象: `Node` を継承する C# クラス
- 主眼: `_Ready()` と `_ExitTree()` の対称性、イベント購読解除漏れ防止、終了処理の順序統一

## Rules

1. `_Ready()` で開始した購読は `_ExitTree()` で必ず解除する。
2. `_ExitTree()` の最後の実行文は必ず `base._ExitTree();` にする。
3. `_ExitTree()` では、独自の解放・解除処理を先に実行し、`base._ExitTree();` は末尾に1回だけ置く。
4. region の順序は `Lifecycle -> Events -> Public API` を基本とする。
5. イベントハンドラ本体は `Events` region にまとめる。
6. `SubscribeEvents` / `UnsubscribeEvents`（または同等の購読開始/解除メソッド）は `Events` region の最上部に配置する。
7. `SubscribeUiEvents` / `UnsubscribeUiEvents` など、UI入力・通知向けの類似購読/解除メソッドも `Events` region の最上部に配置する。
8. `EnsureChildNodes` など、ノード参照の確立を行う初期化メソッドも `Events` region の最上部に配置する。

## Method Template

```csharp
#region Lifecycle

public override void _Ready()
{
    // 購読開始
}

public override void _ExitTree()
{
    // 購読解除・後始末

    base._ExitTree();
}

#endregion
```

## Review Checklist

- `_Ready()` 内で `+=` したものが `_ExitTree()` で `-=` されているか
- `_ExitTree()` の末尾が `base._ExitTree();` になっているか
- `base._ExitTree();` が重複していないか
- region 構成が `Lifecycle -> Events -> Public API` の順序か

## Maintenance

- この規約を追加・変更した場合は、同日に `.github/design-philosophy/registry.md` へ履歴を追記する。
- 新しいカテゴリ名を導入した場合は `.github/design-philosophy/index.md` の対応表も更新する。
