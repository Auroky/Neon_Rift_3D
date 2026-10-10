# Deterministic visual inspection using actual actor scenes; gameplay is paused.
extends SceneTree
func _initialize():
	call_deferred("run")
func run():
	var game=load("res://Scenes3D/Main3D.tscn").instantiate()
	game.set_script(null)
	game.process_mode=Node.PROCESS_MODE_DISABLED
	root.add_child(game)
	game.get_node("Player").hide()
	var roles=["Player3D","ChaserEnemy3D","StrikerEnemy3D","HeavyEnemy3D"]
	var poses=[Vector3(0,0.5,0),Vector3(-5,0.5,-3),Vector3(5,0.5,-1),Vector3(-4,0.5,3)]
	for i in 4:
		var actor=load("res://Scenes3D/%s.tscn" % roles[i]).instantiate()
		actor.process_mode=Node.PROCESS_MODE_DISABLED
		game.add_child(actor)
		actor.position=poses[i]
		actor.rotation.y=[0.0,-0.7,0.9,-0.4][i]
	await process_frame
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://.godot/visual_preview.png")
	print("VISUAL_PREVIEW_OK")
	quit()
