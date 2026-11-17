extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var failures: Array[String] = []
	if DisplayServer.get_name() == "headless":
		push_error("このテストにはウィンドウのフォーカスを切り替えられる表示環境が必要です。")
		quit(1)
		return

	var camera := root.get_node("Application/Viewport/Camera")
	var selection := root.get_node("Application/Model/Selection")
	root.grab_focus()
	await create_timer(0.2).timeout
	if not root.has_focus():
		push_error("メインウィンドウにフォーカスを設定できません。")
		quit(1)
		return

	await process_frame
	var initial_position: Vector3 = camera.get("Position")
	Input.action_press("translate_camera_right")
	Input.action_press("switch_selection_mode_add")
	await process_frame
	await process_frame
	if camera.get("Position") == initial_position:
		failures.append("アクティブ時にカメラが移動しませんでした。")
	if selection.get("CurrentMode") != 1:
		failures.append("アクティブ時に選択モードが切り替わりませんでした。")
	Input.action_release("translate_camera_right")

	var window := Window.new()
	window.title = "Input focus test"
	window.size = Vector2i(320, 200)
	root.add_child(window)
	window.grab_focus()
	await create_timer(0.2).timeout
	if root.has_focus() or not window.has_focus():
		failures.append("別ウィンドウにフォーカスを切り替えられませんでした。")
	if selection.get("CurrentMode") != 0:
		failures.append("フォーカス喪失後に一時的な選択モードが解除されませんでした。")
	Input.action_release("switch_selection_mode_add")

	selection.call("SetMode", 2)
	initial_position = camera.get("Position")
	var initial_rotation: Quaternion = camera.get("Rotation")
	await process_frame
	Input.action_press("translate_camera_right")
	Input.action_press("rotate_camera_left")
	Input.action_press("escape")
	await process_frame
	await process_frame

	if camera.get("Position") != initial_position:
		failures.append("非アクティブ時にカメラが移動しました。")
	if camera.get("Rotation") != initial_rotation:
		failures.append("非アクティブ時にカメラが回転しました。")
	if selection.get("CurrentMode") != 2:
		failures.append("非アクティブ時にEscapeが処理されました。")

	Input.action_release("translate_camera_right")
	Input.action_release("rotate_camera_left")
	Input.action_release("escape")
	root.grab_focus()
	await create_timer(0.2).timeout
	if not root.has_focus():
		failures.append("メインウィンドウにフォーカスが戻りませんでした。")
	if selection.get("CurrentMode") != 2:
		failures.append("UIから設定した選択モードがフォーカス復帰時に変更されました。")
	initial_position = camera.get("Position")
	initial_rotation = camera.get("Rotation")
	Input.action_press("translate_camera_right")
	Input.action_press("rotate_camera_left")
	await process_frame
	await process_frame
	if camera.get("Position") == initial_position:
		failures.append("フォーカス復帰後にカメラ操作が再開しませんでした。")
	if camera.get("Rotation") == initial_rotation:
		failures.append("フォーカス復帰後にカメラ回転が再開しませんでした。")
	Input.action_release("translate_camera_right")
	Input.action_release("rotate_camera_left")
	selection.call("SetMode", 0)
	window.queue_free()

	if failures.is_empty():
		print("Input hub focus contract passed.")
		quit(0)
		return

	for failure in failures:
		push_error(failure)
	quit(1)
