extends SceneTree

const THEMES: Dictionary = {
	"dark": "res://assets/theme/coaxis_dark.tres",
	"light": "res://assets/theme/coaxis_light.tres",
}

const EXPECTED_CURSOR_OUTLINE_COLORS: Dictionary = {
	"dark": Color(0.137255, 0.141176, 0.152941, 1),
	"light": Color(0.898039, 0.901961, 0.909804, 1),
}

const REQUIRED_COLORS: Dictionary = {
	"CoaXisBrand": ["primary"],
	"CoaXisCursor": ["outline"],
	"CoaXisCommand": ["do", "undo"],
	"CoaXisEnvironment": ["background"],
	"Control": ["font_color"],
	"CoaXisTreeIcon": ["normal", "muted", "visibility_hidden", "badge_background"],
	"RichTextLabel": ["default_color", "font_selected_color", "selection_color"],
	"CoaXisModelStatus": [
		"Unloaded",
		"Initialized",
		"Registered",
		"Loading",
		"Loaded",
		"LoadFailed",
		"Disposed",
	],
}

const REQUIRED_STYLEBOXES: Dictionary = {
	"CoaXisSearchResultTree": ["selected", "selected_focus"],
	"CoaXisToggleButton": ["pressed"],
	"OptionButton": ["normal", "hover", "pressed", "hover_pressed", "disabled", "focus"],
}


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var failures: Array[String] = []

	for theme_id in THEMES:
		var resource_path: String = THEMES[theme_id]
		var theme := ResourceLoader.load(resource_path) as Theme
		if theme == null:
			failures.append("%s: Themeリソースを読み込めません: %s" % [theme_id, resource_path])
			continue

		for theme_type in REQUIRED_COLORS:
			for color_name in REQUIRED_COLORS[theme_type]:
				if not theme.has_color(color_name, theme_type):
					failures.append("%s: 必須色 %s/colors/%s がありません" % [
						theme_id,
						theme_type,
						color_name,
					])

		if theme.has_color("outline", "CoaXisCursor"):
			var cursor_outline: Color = theme.get_color("outline", "CoaXisCursor")
			var expected_cursor_outline: Color = EXPECTED_CURSOR_OUTLINE_COLORS[theme_id]
			if not cursor_outline.is_equal_approx(expected_cursor_outline):
				failures.append("%s: 独自カーソルの外枠色がテーマHeader色と一致しません" % theme_id)

		for theme_type in REQUIRED_STYLEBOXES:
			for style_name in REQUIRED_STYLEBOXES[theme_type]:
				if not theme.has_stylebox(style_name, theme_type):
					failures.append("%s: 必須StyleBox %s/styles/%s がありません" % [
						theme_id,
						theme_type,
						style_name,
					])

		var flat_input_styles: Dictionary = {
			"OptionButton": ["normal", "hover", "pressed", "hover_pressed", "disabled", "focus"],
			"LineEdit": ["normal", "focus", "read_only"],
			"TextEdit": ["normal", "focus", "read_only"],
		}
		for theme_type in flat_input_styles:
			for style_name in flat_input_styles[theme_type]:
				var stylebox := theme.get_stylebox(style_name, theme_type) as StyleBoxFlat
				if (
					stylebox == null
					or stylebox.border_width_left != 0
					or stylebox.border_width_top != 0
					or stylebox.border_width_right != 0
					or stylebox.border_width_bottom != 0
				):
					failures.append("%s: %sの%sに枠線が設定されています" % [
						theme_id,
						theme_type,
						style_name,
					])

		var option_button := OptionButton.new()
		option_button.theme = theme
		root.add_child(option_button)
		var input := theme.get_stylebox("normal", "LineEdit") as StyleBoxFlat
		var read_only := theme.get_stylebox("read_only", "LineEdit") as StyleBoxFlat
		for style_name in ["normal", "hover", "pressed", "hover_pressed", "disabled"]:
			var style := option_button.get_theme_stylebox(style_name) as StyleBoxFlat
			var expected := read_only if style_name == "disabled" else input
			if style == null or expected == null or not style.bg_color.is_equal_approx(expected.bg_color):
				failures.append("%s: OptionButtonの%s背景が入力欄と一致しません" % [theme_id, style_name])
		for color_name in ["font_color", "font_focus_color", "font_hover_color", "font_hover_pressed_color", "font_pressed_color"]:
			if not option_button.get_theme_color(color_name).is_equal_approx(theme.get_color("font_color", "LineEdit")):
				failures.append("%s: OptionButtonの%sが入力欄の文字色と一致しません" % [theme_id, color_name])
		option_button.free()

		if theme.has_color("default_color", "RichTextLabel") and theme.has_color("font_color", "Control"):
			if not theme.get_color("default_color", "RichTextLabel").is_equal_approx(theme.get_color("font_color", "Control")):
				failures.append("%s: RichTextLabelの文字色が標準文字色と一致しません" % theme_id)
			var label := RichTextLabel.new()
			label.theme = theme
			root.add_child(label)
			if not label.get_theme_color("default_color").is_equal_approx(theme.get_color("font_color", "Control")):
				failures.append("%s: RichTextLabelにテーマの文字色が適用されません" % theme_id)
			label.free()

		if theme.has_color("selection_color", "RichTextLabel") and theme.has_color("primary", "CoaXisBrand"):
			if not theme.get_color("selection_color", "RichTextLabel").is_equal_approx(theme.get_color("primary", "CoaXisBrand")):
				failures.append("%s: RichTextLabelの選択色がCoaXisBlueと一致しません" % theme_id)
		if theme.has_color("selection_color", "RichTextLabel") and theme.has_color("font_selected_color", "RichTextLabel"):
			var selection_luminance := theme.get_color("selection_color", "RichTextLabel").srgb_to_linear().get_luminance()
			var text_luminance := theme.get_color("font_selected_color", "RichTextLabel").srgb_to_linear().get_luminance()
			var contrast: float = (maxf(selection_luminance, text_luminance) + 0.05) / (minf(selection_luminance, text_luminance) + 0.05)
			if contrast < 4.5:
				failures.append("%s: RichTextLabelの選択文字と背景のコントラストが不足しています" % theme_id)

		if theme.has_color("do", "CoaXisCommand") and theme.has_color("undo", "CoaXisCommand"):
			var do_color := theme.get_color("do", "CoaXisCommand")
			var undo_color := theme.get_color("undo", "CoaXisCommand")
			var tree := Tree.new()
			tree.theme = theme
			root.add_child(tree)
			for color_name in ["do", "undo"]:
				if not tree.get_theme_color(color_name, "CoaXisCommand").is_equal_approx(theme.get_color(color_name, "CoaXisCommand")):
					failures.append("%s: Treeからコマンド履歴の色 %s を取得できません" % [theme_id, color_name])
			tree.free()
			if do_color.is_equal_approx(undo_color):
				failures.append("%s: コマンド履歴のDoとUndoの文字色が同じです" % theme_id)
			if theme_id == "light":
				var background := theme.get_stylebox("panel", "PanelContainer") as StyleBoxFlat
				var background_luminance := background.bg_color.srgb_to_linear().get_luminance()
				for color: Color in [do_color, undo_color]:
					var contrast := (background_luminance + 0.05) / (color.srgb_to_linear().get_luminance() + 0.05)
					if contrast < 4.5:
						failures.append("%s: コマンド履歴の文字色と背景のコントラストが不足しています" % theme_id)

	var message_scene := load("res://scenes/ui/message_ui.tscn") as PackedScene
	if message_scene == null:
		failures.append("Messageシーンを読み込めません")
	else:
		var scene_state := message_scene.get_state()
		var selection_enabled := false
		var context_menu_enabled := false
		for node_index in scene_state.get_node_count():
			if scene_state.get_node_type(node_index) != "RichTextLabel":
				continue
			for property_index in scene_state.get_node_property_count(node_index):
				var property_name := scene_state.get_node_property_name(node_index, property_index)
				var property_value: Variant = scene_state.get_node_property_value(node_index, property_index)
				if property_name == "selection_enabled":
					selection_enabled = property_value == true
				elif property_name == "context_menu_enabled":
					context_menu_enabled = property_value == true
		if not selection_enabled or not context_menu_enabled:
			failures.append("Messageの選択またはコンテキストメニューが無効です")
		elif DisplayServer.get_name() == "headless":
			var label := RichTextLabel.new()
			label.selection_enabled = selection_enabled
			label.context_menu_enabled = context_menu_enabled
			label.text = "Copy test"
			root.add_child(label)
			label.select_all()
			if label.get_menu().get_item_index(RichTextLabel.MENU_COPY) == -1:
				failures.append("RichTextLabelのコンテキストメニューにコピー項目がありません")
			if label.get_selected_text() != label.text:
				failures.append("RichTextLabelの文字列を選択できません")
			label.free()

	if failures.is_empty():
		print("Viewer theme contract passed.")
		quit(0)
		return

	for failure in failures:
		push_error(failure)
	quit(1)
