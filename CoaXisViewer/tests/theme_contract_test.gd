extends SceneTree

const THEMES: Dictionary = {
	"dark": "res://assets/theme/coaxis_dark.tres",
	"light": "res://assets/theme/coaxis_light.tres",
}

const REQUIRED_COLORS: Dictionary = {
	"CoaXisBrand": ["primary"],
	"CoaXisEnvironment": ["background"],
	"CoaXisTreeIcon": ["normal", "muted", "visibility_hidden", "badge_background"],
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
}


func _initialize() -> void:
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

		for theme_type in REQUIRED_STYLEBOXES:
			for style_name in REQUIRED_STYLEBOXES[theme_type]:
				if not theme.has_stylebox(style_name, theme_type):
					failures.append("%s: 必須StyleBox %s/styles/%s がありません" % [
						theme_id,
						theme_type,
						style_name,
					])

	if failures.is_empty():
		print("Viewer theme contract passed.")
		quit(0)
		return

	for failure in failures:
		push_error(failure)
	quit(1)
