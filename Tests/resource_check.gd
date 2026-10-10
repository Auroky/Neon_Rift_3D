# Run after Godot import; include dynamically spawned scenes as resource roots.
extends SceneTree
var visited: Dictionary = {}
var failures: Array[String] = []

func visit(path: String):
	if visited.has(path): return
	visited[path] = true
	if not ResourceLoader.exists(path):
		failures.append("Missing resource: " + path)
		return
	for dependency in ResourceLoader.get_dependencies(path):
		var parts = dependency.split("::")
		var target = parts[parts.size()-1]
		if target.begins_with("res://"): visit(target)

func inspect_assets(folder: String):
	for file in DirAccess.get_files_at(folder):
		if file.ends_with(".import") or file.ends_with(".uid"): continue
		var path = folder.path_join(file)
		if not visited.has(path): failures.append("Unused asset: " + path)
	for child in DirAccess.get_directories_at(folder):
		inspect_assets(folder.path_join(child))

func _initialize():
	for scene in ["Main3D", "Player3D", "ChaserEnemy3D", "StrikerEnemy3D", "HeavyEnemy3D", "Bullet3D"]:
		visit("res://Scenes3D/%s.tscn" % scene)
	inspect_assets("res://Assets")
	for failure in failures: push_error(failure)
	if failures.is_empty(): print("RESOURCE_CHECK_OK: ", visited.size(), " resources; no missing dependencies or unused assets")
	quit(0 if failures.is_empty() else 1)
