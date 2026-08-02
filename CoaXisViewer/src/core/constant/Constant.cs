/// <summary>
/// ユーザー設定に公開しない固定値の集約
/// </summary>
public static class Constant
{
	public static class Ui
	{
		public static class Tree
		{
			public const int CommandNoColumnMinWidth = 64;
			public const int CommandStateColumnMinWidth = 64;
			public const int SelectionNoColumnMinWidth = 64;
			public const int HierarchyVisibleIconSize = 24;
		}
	}

	public static class Ipc
	{
		public const string PipeName = "CoaXisViewerPipe";
		public const bool StartPipeServerOnReady = true;
	}

	public static class Input
	{
		public const float ArcballRegionRatio = 0.45f;
		public const float MoveThreshold = 1.0f;
	}

	public static class Color
	{
		public const string CommandDoColor = "#FFFFFFFF";
		public const string CommandUndoColor = "#808080FF";
	}

	public static class Model
	{
		public const string RootModelId = "00000000-0000-0000-0000-000000000001";
	}
}
