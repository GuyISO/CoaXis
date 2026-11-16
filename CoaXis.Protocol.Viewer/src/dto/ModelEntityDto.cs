using System;

namespace CoaXis.Protocol.Viewer;

/// <summary>
/// モデル実体のデータを表す DTO クラス
/// </summary>
public class ModelEntityDto
{
    /// <summary>
    /// モデル実体の一意なID。省略時は新しいGUIDが生成される。
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 親モデルのID。省略時は null。
    /// </summary>
    public Guid? ParentId { get; init; } = null;

    /// <summary>
    /// モデルの種類。省略時は空文字列。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// モデルの名前。省略時は空文字列。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// モデルの位置を表す3次元ベクトル。省略時は (0, 0, 0)。
    /// </summary>
    public float[] Position { get; init; } = new[] { 0f, 0f, 0f };

    /// <summary>
    /// モデルの回転を表すクォータニオン。省略時は (0, 0, 0, 1)。
    /// </summary>
    public float[] Rotation { get; init; } = new[] { 0f, 0f, 0f, 1f };

    /// <summary>
    /// モデルの表示設定。省略時は "Inherit"。
    /// </summary>
    public string Visibility { get; init; } = "Inherit";
    
    /// <summary>
    /// ツリー表示で折りたたまれているかどうか
    /// </summary>
    public bool IsCollapsed { get; init; } = false;
    /// <summary>
    /// ツリー表示に使うアイコンID。省略時またはNoneの場合は既定アイコンを使う。
    /// </summary>
    public string IconId { get; init; } = string.Empty;
    
    /// <summary>
    /// シーン内でのパス。省略時は空文字列。
    /// </summary>
    public string ScenePath { get; init; } = string.Empty;

    /// <summary>
    /// Viewport上にPropertyのOverlayTreeを表示するかどうか。省略時は false。
    /// </summary>
    public bool IsPropertyTreeVisible { get; init; } = false;
}
