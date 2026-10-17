---
name: godot-node-lifecycle-rules
description: "Use when: Godot C# Node の _Ready/_ExitTree、購読解除、base 呼び出しを実装・修正するとき"
---

# Godot Node Lifecycle Rules

Godot C# Node のライフサイクル規約。Application Hubのregion構成は `.github/instructions/design-philosophy.instructions.md` を優先する。

## Rules

1. `_Ready()` で開始した購読は `_ExitTree()` で必ず解除する。
2. `_ExitTree()` は独自の解除・解放を先に行い、最後に `base._ExitTree();` を1回呼ぶ。
3. ハンドラーと購読開始・解除は `Events` に置く。購読・Node参照確立メソッドを先頭にまとめる。
4. Nodeでは `Lifecycle` を `Events` より前に置く。Application Hubは専用の7-region規約に従う。

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

## 確認

- `_Ready()` 内で `+=` したものが `_ExitTree()` で `-=` されているか
- `_ExitTree()` の末尾が `base._ExitTree();` になっているか
- `base._ExitTree();` が重複していないか
