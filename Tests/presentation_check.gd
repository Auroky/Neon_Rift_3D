# Use fresh, disposable APPDATA containing presentation-test-user. Optional capture:
# --script res://Tests/presentation_check.gd -- --capture-dir C:/absolute/output
extends SceneTree

func _initialize():
	call_deferred("run")

func capture(name: String):
	if DisplayServer.get_name() == "headless": return
	var args = OS.get_cmdline_user_args()
	var index = args.find("--capture-dir")
	if index < 0: return
	await RenderingServer.frame_post_draw
	assert(root.get_texture().get_image().save_png(args[index+1].path_join(name)) == OK)

func run():
	assert(OS.get_user_data_dir().contains("presentation-test-user"), "Isolate test scores")
	assert(not FileAccess.file_exists("user://score_history.json"), "Use a fresh test directory")
	var game = load("res://Scenes3D/Main3D.tscn").instantiate()
	root.add_child(game)
	game.set_physics_process(false)
	var player = game.get_node("Player")
	player.set_physics_process(false)
	var entities = game.get_node("Entities")
	var poses = [Vector3(-4,0.5,-2),Vector3(4,0.5,-1),Vector3(-3,0.5,2)]
	var actors = []
	for i in 3:
		game.call("SpawnEnemy")
		var actor = entities.get_child(i)
		actor.set_physics_process(false)
		actor.position = poses[i]
		actor.rotation.y = [-0.7,0.8,-0.4][i]
		actors.append(actor)
	player.call("TakeDamage",35.0)
	player.call("TakeDamage",11.0)
	assert(player.get("Health")==65.0, "Presentation must preserve immunity and HP")
	actors[0].call("TakeDamage",12.0)
	actors[1].call("TakeDamage",24.0)
	actors[2].call("TakeDamage",96.0)
	await create_timer(0.25).timeout
	assert(is_equal_approx(player.get_node("VisualFeedback").call("ReadFraction"),0.65))
	assert(is_equal_approx(actors[2].get_node("VisualFeedback").call("ReadFraction"),0.52))
	# Exercise a real fourth spawn and its score signal, then check effects are separate.
	game.call("SpawnEnemy")
	var victim = entities.get_child(3)
	victim.set_physics_process(false)
	victim.position = Vector3(3,0.5,2)
	victim.call("TakeDamage",1000.0)
	victim.call("TakeDamage",1000.0)
	assert(game.get_node("Hud/Stats").text.contains("Score: 100"))
	await create_timer(0.14).timeout
	var effects = []
	for node in entities.get_children():
		if str(node.name).begins_with("TankExplosion"): effects.append(node)
	assert(effects.size() == 1, "Death must create exactly one effect")
	assert(effects[0].find_children("*", "CollisionObject3D", true, false).is_empty(), "FX must not collide")
	await capture("NeonRift_UI_Combat.png")
	await create_timer(1.1).timeout
	assert(not is_instance_valid(effects[0]), "Effects must clean up")
	# Fill all five slots using actual completed rounds in this disposable save.
	for run_index in 5:
		for actor in actors:
			if is_instance_valid(actor): actor.call("TakeDamage",1000.0)
		game.call("_PhysicsProcess",61.0)
		if run_index == 4: break
		game.get_node("Hud/Dashboard").get_child(0).emit_signal("pressed")
		await process_frame
		assert(player.get("Active") and player.get("Health") == 100.0, "Button must use normal restart")
		actors = []
		for j in (run_index+1)*2:
			game.call("SpawnEnemy")
			var new_actor = entities.get_child(entities.get_child_count()-1)
			new_actor.set_physics_process(false)
			new_actor.call("TakeDamage",1000.0)
		await process_frame
	var rounds = JSON.parse_string(FileAccess.get_file_as_string("user://score_history.json"))
	assert(rounds.size()==5)
	await capture("NeonRift_UI_Results.png")
	game.get_node("Hud/Dashboard").get_child(0).emit_signal("pressed")
	await process_frame
	await process_frame
	assert(not game.get_node("Hud/Dashboard").get_child(0).visible)
	assert(player.get("Health")==100.0 and player.get("Active"))
	assert(entities.get_child_count()==0, "Restart clears old enemies and effects")
	print("PRESENTATION_CHECK_OK: true HP bars, exactly-once non-colliding FX, cleanup, five-slot results, button restart")
	quit()
