extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var failures: Array[String] = []
	var window := load("res://src/ui/window/UiWindow.cs").new() as Window
	window.unfocusable = true
	root.add_child(window)
	var search := load("res://scenes/ui/search_ui.tscn").instantiate() as Control
	window.call("SetContainer", search)
	var entity_name := search.get_node("MarginContainer/VBoxContainer/Filters/EntityName") as LineEdit
	var property_value := search.get_node("MarginContainer/VBoxContainer/Filters/PropertyValue") as LineEdit

	for field in [entity_name, property_value]:
		var release := InputEventMouseButton.new()
		release.button_index = MOUSE_BUTTON_LEFT
		release.pressed = false
		field.gui_input.emit(release)
		if not window.unfocusable:
			failures.append("%s: マウスボタンの解放でフォーカスが有効になりました" % field.name)

		for button in [MOUSE_BUTTON_LEFT, MOUSE_BUTTON_RIGHT]:
			var click := InputEventMouseButton.new()
			click.button_index = button
			click.pressed = true
			field.gui_input.emit(click)
			if window.unfocusable:
				failures.append("%s: 入力欄のクリックでフォーカスが有効になりません" % field.name)

			window.focus_exited.emit()
			if not window.unfocusable:
				failures.append("%s: フォーカス喪失後に元の設定に戻りません" % field.name)

	window.unfocusable = false
	window.focus_exited.emit()
	if window.unfocusable:
		failures.append("Search UIが変更していないフォーカス設定が変更されました")

	window.free()
	if failures.is_empty():
		print("Search UI focus contract passed.")
		quit(0)
		return

	for failure in failures:
		push_error(failure)
	quit(1)
