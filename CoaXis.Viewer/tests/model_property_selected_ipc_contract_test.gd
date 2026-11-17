extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var test: Node = load("res://tests/ModelPropertySelectedIpcContract.cs").new()
	root.add_child(test)
	test.call("Run")
