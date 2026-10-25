using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelPropertyの実効表示を導出し、パーティクルの描画Layerへ反映するハブ。
/// </summary>
public partial class ModelPropertyVisualHub : BaseHub
{
	#region Fields

	#endregion

	#region Properties

	#endregion

	#region Signals

	/// <summary>Propertyの実効表示を解決して描画へ反映した通知。値は ModelProperty.IsVisible を参照する。</summary>
	/// <param name="propertyId">解決された ModelProperty の識別子</param>
	[Signal] public delegate void VisibilityResolvedEventHandler(string propertyId);

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		SubscribeApplicationEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeApplicationEvents();

		base._ExitTree();
	}

	#endregion

	#region Events

	/// <summary>
	/// Applicationイベントの購読を開始する。
	/// </summary>
	private void SubscribeApplicationEvents()
	{
		Application.Model.Property.State.VisibilityNotified += OnPropertyVisibilityNotified;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;

		// Viewport系ハブはModel系より後に初期化されるため、全モジュール生成後に購読する。
		CallDeferred(MethodName.SubscribeViewportEvents);
	}

	/// <summary>
	/// Viewportのイベントの購読を開始する。
	/// </summary>
	private void SubscribeViewportEvents()
	{
		Application.Viewport.Display.ActiveLayersNotified += OnActiveLayersNotified;
		OnActiveLayersNotified();
	}

	/// <summary>
	/// Applicationイベントの購読を解除する。
	/// </summary>
	private void UnsubscribeApplicationEvents()
	{
		Application.Model.Property.State.VisibilityNotified -= OnPropertyVisibilityNotified;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		ViewportHub viewport = Application.Viewport;
		if (viewport != null)
		{
			viewport.Display.ActiveLayersNotified -= OnActiveLayersNotified;
		}
	}

	/// <summary>
	/// 表示対象レイヤーが変わったとき、全Propertyの放出有無を再評価する。
	/// </summary>
	private void OnActiveLayersNotified()
	{
		foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
		{
			ResolveAndApply(property);
		}
	}

	/// <summary>
	/// Propertyの表示設定の変更通知を受け、そのPropertyと子孫の実効表示を再解決する。
	/// </summary>
	/// <param name="propertyId">表示設定が変更された ModelProperty の識別子</param>
	private void OnPropertyVisibilityNotified(string propertyId)
	{
		if (!Guid.TryParse(propertyId, out Guid parsedPropertyId))
		{
			Application.Log.Warn($"ModelPropertyVisualHub: invalid propertyId for visibility notification. propertyId='{propertyId}'");
			return;
		}

		ModelProperty property = Application.Model.Property.Registry.Get(parsedPropertyId);
		if (property != null)
		{
			ResolveAndApplyRecursive(property);
		}
	}

	/// <summary>
	/// Entityの実効表示が変わったとき、継承している所属Propertyの実効表示を再解決する。
	/// </summary>
	/// <param name="entityId">実効表示が解決された ModelEntity の識別子</param>
	private void OnEntityVisibilityResolved(string entityId)
	{
		if (!Guid.TryParse(entityId, out Guid parsedEntityId))
		{
			return;
		}

		ModelEntity entity = Application.Model.Entity.Registry.Get(parsedEntityId);
		if (entity == null)
		{
			return;
		}

		foreach (ModelProperty property in entity.Properties)
		{
			ResolveAndApplyRecursive(property);
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Propertyの実効表示を導出し、パーティクルの描画Layerへ反映してUIへ通知する。
	/// </summary>
	/// <param name="property">反映対象の ModelProperty</param>
	public void ResolveAndApply(ModelProperty property)
	{
		if (property == null)
		{
			return;
		}

		ModelParticle particle = property.ParticleNode;
		if (particle != null)
		{
			// 描画Layerは実効状態の投影であり、別の可視状態として読み戻さない。
			bool isVisible = property.IsVisible;
			ViewportLayer visibilityLayer = isVisible
				? ViewportLayer.VisibleProperty
				: ViewportLayer.InvisibleProperty;
			particle.Layers = (uint)(ViewportLayer.Default | visibilityLayer);

			// どのカメラにも映らない間は、シミュレーションと描画負荷を止める。
			particle.Emitting = (Application.Viewport.Display.ActiveLayers & (uint)visibilityLayer) != 0;
		}

		EmitSignal(SignalName.VisibilityResolved, property.Id.ToString());
	}

	#endregion

	#region Helpers

	/// <summary>
	/// 指定Propertyとその子孫の実効表示を再解決する。
	/// </summary>
	private void ResolveAndApplyRecursive(ModelProperty root)
	{
		var stack = new Stack<ModelProperty>();
		var visited = new HashSet<Guid>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			ModelProperty property = stack.Pop();
			if (!visited.Add(property.Id))
			{
				continue;
			}

			ResolveAndApply(property);
			foreach (ModelProperty child in property.Children)
			{
				stack.Push(child);
			}
		}
	}

	#endregion
}
