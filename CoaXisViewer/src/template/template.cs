using Godot;
using System;

/// <summary>
/// クラスの役割をここで簡単に説明します。
/// </summary>
/// <remarks>
/// クラスに関する備考や注意点をここに記述します。
/// </remarks>

public partial class ClassName : Node3D
{
	#region Fields
	#endregion

	#region Properties
	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		
	}

    public override void _ExitTree()
	{
		
	}


	public override void _Process(double delta)
	{
		
	}

	#endregion

	#region Events

	/// <summary>
	/// OnEventRequested の説明をここに記述します。
	/// </summary>
	/// <param name="argument">引数の説明をここに記述します。</param>
	/// <returns>戻り値の説明をここに記述します。</returns>
	private void OnEventRequested(int argument)
	{
		// 内部メソッドのロジックをここに実装します。
	}

	#endregion

	#region Public API

	/// <summary>
	/// PublicMethod の説明をここに記述します。
	/// </summary>
	/// <param name="argument">引数の説明をここに記述します。</param>
	/// <returns>戻り値の説明をここに記述します。</returns>
	public int PublicMethod(int argument)
	{
		return argument;
	}

	#endregion

	#region Internal Helpers
	
	/// <summary>
	/// InternalMethod の説明をここに記述します。
	/// </summary>
	/// <param name="argument">引数の説明をここに記述します。</param>
	/// <returns>戻り値の説明をここに記述します。</returns>
	private int InternalMethod(int argument)
	{
		return argument;
	}

	#endregion

}
