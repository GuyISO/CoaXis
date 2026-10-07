// TODO: リファクタリング確認後に削除
using System;
using Godot;
using System.Collections.Generic;

/// <summary>
/// モデルの実体（Entity）を表すクラス。ModelFactory で生成され、ModelNode（描画ノード）に対応する
/// </summary>
public class ModelEntity
{
    #region Fields

    /// <summary>子モデル実体を保持するための辞書</summary>
    private readonly Dictionary<Guid, ModelEntity> _children = new();
    /// <summary>このモデル実体に直接紐づくルートプロパティを保持するための辞書</summary>
    private readonly Dictionary<Guid, ModelProperty> _properties = new();
    /// <summary>モデルの配置位置</summary>
    private Vector3 _position;
    /// <summary>モデルの回転</summary>
    private Quaternion _rotation;
    /// <summary>モデルの表示状態</summary>
    private ModelVisibility _visibility;
    /// <summary>ツリー表示時の折り畳み状態</summary>
    private bool _isCollapsed;
    /// <summary>モデルのロード状態</summary>
    private ModelStatus _status = ModelStatus.Unloaded;
    /// <summary>この実体に対応する描画用ノード</summary>
    private ModelNode _node;

    #endregion

    #region Properties

    /// <summary>モデル実体の一意識別子</summary>
    public Guid Id { get; }

    /// <summary>親モデル実体の識別子。Root 配下の場合は Guid.Empty</summary>
    public Guid ParentId { get; }

    /// <summary>モデル種別</summary>
    public string Type { get; }

    /// <summary>表示名</summary>
    public string Name { get; }

    /// <summary>座標変換後の配置位置</summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            _position = value;
            if (_node != null && GodotObject.IsInstanceValid(_node))
            {
                _node.Position = value;
            }

            Application.Model.Entity.State.NotifyPosition(Id);
        }
    }

    /// <summary>座標変換後の回転</summary>
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value)
            {
                return;
            }

            _rotation = value;
            if (_node != null && GodotObject.IsInstanceValid(_node))
            {
                _node.Quaternion = value;
            }

            Application.Model.Entity.State.NotifyRotation(Id);
        }
    }

    /// <summary>表示状態</summary>
    public virtual ModelVisibility Visibility
    {
        get => _visibility;
        set
        {
            if (_visibility == value)
            {
                return;
            }

            _visibility = value;
            NotifyVisibilityChanged();
        }
    }

    /// <summary>ツリー表示時の初期折り畳み状態</summary>
    public virtual bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (_isCollapsed == value)
            {
                return;
            }

            _isCollapsed = value;
            Application.Model.Entity.State.NotifyCollapsed(Id);
        }
    }

    /// <summary>アイコン画像のパス</summary>
    public string IconPath { get; }

    /// <summary>読み込み対象シーンのパス</summary>
    public string ScenePath { get; }

    /// <summary>読み込んだシーンのAABB中心をモデル原点に合わせる位置補正を行うかどうか</summary>
    public bool AlignToAabbCenter { get; }

    /// <summary>ModelEntityの現在状態を取得する。</summary>
    public ModelStatus Status => _status;

    /// <summary>Visibility設定と親階層から導出した、このモデルの実効表示状態を取得する。</summary>
    public bool IsVisible => ModelVisibilityResolver.ResolveIsVisible(this);

    /// <summary>この実体に対応する描画用ノード</summary>
    public ModelNode Node
    {
        get => _node;
        set
        {
            _node = value;
            if (_node == null || !GodotObject.IsInstanceValid(_node))
            {
                return;
            }

            // ノードの生成・再接続時もEntityが保持する配置状態を正本として同期する。
            _node.Position = _position;
            _node.Quaternion = _rotation;
        }
    }

    /// <summary>親実体を参照するためのプロパティ</summary>
    public ModelEntity Parent => ParentId != Guid.Empty ? Application.Model.Entity.Registry.Get(ParentId) : null;

    /// <summary>子実体の一覧を返す</summary>
    public IReadOnlyCollection<ModelEntity> Children => _children.Values;

    /// <summary>このモデル実体に直接紐づくルートプロパティの一覧を返す</summary>
    public IReadOnlyCollection<ModelProperty> Properties => _properties.Values;

    #endregion

    #region Constructors

    /// <summary>
    /// モデル実体を生成する
    /// </summary>
    public ModelEntity(
        Guid id,
        Guid parentId,
        string type,
        string name,
        Vector3 position,
        Quaternion rotation,
        ModelVisibility visibility,
        bool isCollapsed,
        string iconPath,
        string scenePath,
        bool alignToAabbCenter)
    {
        Id = id;
        ParentId = parentId;
        Type = type ?? string.Empty;
        Name = name ?? string.Empty;
        _position = position;
        _rotation = rotation;
        _visibility = visibility;
        _isCollapsed = isCollapsed;
        IconPath = iconPath ?? string.Empty;
        ScenePath = scenePath ?? string.Empty;
        AlignToAabbCenter = alignToAabbCenter;
    }

    /// <summary>
    /// 最小情報からモデル実体を生成する
    /// </summary>
    public ModelEntity(Guid id, Guid parentId, string name)
        : this(
            id,
            parentId,
            string.Empty,
            name,
            Vector3.Zero,
            Quaternion.Identity,
            ModelVisibility.Inherit,
            false,
            string.Empty,
            string.Empty,
            false)
    {
    }

    #endregion

    #region State Transitions

    /// <summary>Registry登録前の初期化状態へ遷移する。</summary>
    internal void InitializeForRegistration()
    {
        TransitionStatus(ModelStatus.Initialized, ModelStatus.Unloaded);
    }

    /// <summary>初期化済みEntityをRegistry登録状態へ遷移する。</summary>
    internal void MarkRegistered()
    {
        TransitionStatus(ModelStatus.Registered, ModelStatus.Initialized);
    }

    /// <summary>Sceneロード開始状態へ遷移する。</summary>
    internal void BeginLoading()
    {
        TransitionStatus(ModelStatus.Loading, ModelStatus.Registered);
    }

    /// <summary>Sceneロード完了状態へ遷移する。</summary>
    /// <exception cref="InvalidOperationException">ScenePathが設定されているのにLoadingを経由せず完了しようとした場合</exception>
    internal void MarkLoaded()
    {
        if (_status == ModelStatus.Registered && !string.IsNullOrWhiteSpace(ScenePath))
        {
            throw new InvalidOperationException(
                $"ModelEntity '{Id}' cannot skip Loading when ScenePath is configured.");
        }

        TransitionStatus(ModelStatus.Loaded, ModelStatus.Registered, ModelStatus.Loading);
    }

    /// <summary>Sceneロード失敗状態へ遷移する。</summary>
    internal void MarkLoadFailed()
    {
        TransitionStatus(ModelStatus.LoadFailed, ModelStatus.Loading);
    }

    /// <summary>登録済みEntityを破棄状態へ遷移する。</summary>
    internal void MarkDisposed()
    {
        TransitionStatus(
            ModelStatus.Disposed,
            ModelStatus.Registered,
            ModelStatus.Loading,
            ModelStatus.Loaded,
            ModelStatus.LoadFailed);
    }

    /// <summary>
    /// Visibility設定変更または祖先変更による実効表示再解決を通知する。
    /// </summary>
    internal void NotifyVisibilityChanged()
    {
        if (Application.Model.Entity.Registry.IsRegistered(Id))
        {
            Application.Model.Entity.State.NotifyVisibility(Id);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 子モデル実体を登録する。同一 Id の重複登録は無視する。
    /// </summary>
    /// <param name="child">追加対象の子モデル実体</param>
    public void AttachEntity(ModelEntity childEntity)
    {
        if (childEntity == null)
        {
            return;
        }

        if (_children.ContainsKey(childEntity.Id))
        {
            return;
        }

        _children.Add(childEntity.Id, childEntity);
    }

    /// <summary>
    /// <summary>子モデル実体の登録を解除する</summary>
    /// <param name="childEntity">解除対象の子モデル実体</param>
    public void DetachEntity(ModelEntity childEntity)
    {
        if (childEntity == null)
        {
            return;
        }

        _children.Remove(childEntity.Id);
    }

    /// <summary>
    /// プロパティをモデル実体に紐付ける
    /// </summary>
    /// <param name="property">追加対象のプロパティ</param>
    public void AttachProperty(ModelProperty property)
    {
        if (property == null)
        {
            return;
        }

        if (_properties.ContainsKey(property.Id))
        {
            return;
        }

        _properties.Add(property.Id, property);
    }

    /// <summary>
    /// プロパティの紐付けを解除する
    /// </summary>
    /// <param name="property">解除対象のプロパティ</param>
    public void DetachProperty(ModelProperty property)
    {
        if (property == null)
        {
            return;
        }

        _properties.Remove(property.Id);
    }

    /// <summary>
    /// 子モデル一覧およびプロパティ一覧をクリアする
    /// </summary>
    public void Clear()
    {
        // TODO: 子モデルおよびプロパティの参照などを解除する処理が必要な場合はここに追加する
        _children.Clear();
        _properties.Clear();
    }

	/// <summary>
	/// モデルの表示/非表示を切り替える。
	/// </summary>
	public void ToggleVisibility()
	{
		if (!Application.Model.Entity.Registry.IsRegistered(Id))
		{
			Application.Log.Warn($"ModelEntity: toggle target is not registered. entityId='{Id}'");
			return;
		}

		var command = new SetModelVisibilityCommand(
			[Id],
			GetNextVisibility(Visibility));
		Application.Command.Execute(command);
	}

    #endregion

	#region Helpers

    /// <summary>
    /// 許可された状態遷移だけを実行し、状態変更を通知する。
    /// </summary>
    /// <param name="nextStatus">遷移先の状態</param>
    /// <param name="allowedPreviousStatuses">遷移を許可する現在状態</param>
    /// <exception cref="InvalidOperationException">現在状態から遷移できない場合</exception>
    private void TransitionStatus(ModelStatus nextStatus, params ModelStatus[] allowedPreviousStatuses)
    {
        if (_status == nextStatus)
        {
            return;
        }

        if (Array.IndexOf(allowedPreviousStatuses, _status) < 0)
        {
            throw new InvalidOperationException(
                $"Invalid ModelEntity status transition for '{Id}': '{_status}' -> '{nextStatus}'.");
        }

        _status = nextStatus;
        if (Application.Model.Entity.Registry.IsRegistered(Id))
        {
            Application.Model.Entity.State.NotifyStatus(Id);
        }
    }

	/// <summary>
	/// 現在の表示設定から次の切替先を返す。
	/// </summary>
	private static ModelVisibility GetNextVisibility(ModelVisibility visibility)
	{
		return visibility switch
		{
			ModelVisibility.Inherit => ModelVisibility.Visible,
			ModelVisibility.Visible => ModelVisibility.Invisible,
			_ => ModelVisibility.Inherit,
		};
	}

    #endregion
}