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
	var entity_count := search.get_node("MarginContainer/VBoxContainer/EntityResultCount") as Label
	var results := search.get_node("MarginContainer/VBoxContainer/Results") as Tree
	var property_count := search.get_node("MarginContainer/VBoxContainer/PropertyResultCount") as Label
	var property_results := search.get_node("MarginContainer/VBoxContainer/PropertyResults") as Tree
	if entity_count.get_index() >= results.get_index() or not entity_count.text.begins_with("Entity | "):
		failures.append("Entity検索結果の件数ラベルがツリーの上にありません")
	if property_count.get_index() >= property_results.get_index() or not property_count.text.begins_with("Property | "):
		failures.append("Property検索結果の件数ラベルがツリーの上にありません")
	if property_results.get_index() <= results.get_index():
		failures.append("Property表示用ツリーがEntity表示用ツリーの下にありません")
	if not property_results.hide_root:
		failures.append("Property検索結果のグルーピング用ルートが非表示になっていません")
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
		print("Search UI focus and result tree contracts passed.")
		quit(0)
		return

	for failure in failures:
		push_error(failure)
	quit(1)
