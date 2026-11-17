extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var test: Node = load("res://tests/ModelSearchContract.cs").new()
	root.add_child(test)
	if not test.call("Run"):
		quit(1)
		return
	test.free()
	print("Model search contract passed.")
	quit(0)
