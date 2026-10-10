# Run with APPDATA redirected to a disposable repair-test-user directory.
extends SceneTree

func _initialize():
	call_deferred("run")

func run():
	assert(OS.get_user_data_dir().contains("repair-test-user"), "Use isolated APPDATA for this test")
	assert(not FileAccess.file_exists("user://score_history.json"), "Use a fresh test directory")
	var game = load("res://Scenes3D/Main3D.tscn").instantiate()
	root.add_child(game)
	var player = game.get_node("Player")
	var start = player.position
	Input.action_press("move_right")
	await create_timer(0.2).timeout
	Input.action_release("move_right")
	assert(player.position.x > start.x + 0.5, "Movement must work")
	player.call("ResetPlayer", Vector3(0,0.5,0))
	await create_timer(4.2).timeout
	game.set_physics_process(false)
	var heavy: Node3D
	var chaser: Node3D
	var striker: Node3D
	for actor in game.get_node("Entities").get_children():
		actor.set_physics_process(false)
		match actor.get_script().resource_path.get_file():
			"HeavyEnemy3D.cs": heavy = actor
			"ChaserEnemy3D.cs": chaser = actor
			"StrikerEnemy3D.cs": striker = actor
	assert(heavy != null and chaser != null and striker != null, "All enemy roles must spawn")
	player.call("ResetPlayer", Vector3(0,0.5,0))
	chaser.position = Vector3(0,0.5,-2)
	# Exercise signal wiring, spawned bullets, physics collision and damage.
	player.emit_signal("ShotRequested",Vector3(0,0.5,-0.9),Vector3.FORWARD)
	await create_timer(0.2).timeout
	assert(chaser.get("Health") == 14.0, "Player bullet must deal 24 damage")
	player.emit_signal("ShotRequested",Vector3(0,0.5,-0.9),Vector3.FORWARD)
	await create_timer(0.2).timeout
	assert(not is_instance_valid(chaser), "Second shot must destroy a chaser")
	assert(heavy.get("Health") == 200.0)
	for i in 8: heavy.call("TakeDamage",24.0)
	assert(heavy.get("Health") == 8.0 and not heavy.is_queued_for_deletion())
	heavy.call("TakeDamage",24.0)
	assert(heavy.is_queued_for_deletion(), "Heavy must die on ninth hit")
	assert(game.get_node("Hud/Stats").text.contains("Score: 400"))
	striker.emit_signal("ShotRequested",Vector3(0,0.5,-1.2),Vector3.BACK)
	await create_timer(0.2).timeout
	assert(player.get("Health") == 89.0, "Enemy bullet must deal 11 damage")
	if DisplayServer.get_name() != "headless":
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("res://.godot/repair_verified.png")
	# A large frame must finish exactly once, without needing a 60-second wait.
	game.call("_PhysicsProcess",61.0)
	assert(not player.get("Active"))
	var rounds = JSON.parse_string(FileAccess.get_file_as_string("user://score_history.json"))
	assert(rounds.size() == 1 and rounds[0].Score == 400 and rounds[0].Result == "TIME UP")
	game.call("_PhysicsProcess",61.0)
	assert(JSON.parse_string(FileAccess.get_file_as_string("user://score_history.json")).size() == 1)
	var key = InputEventKey.new()
	key.keycode = KEY_R
	key.pressed = true
	Input.parse_input_event(key)
	await process_frame
	assert(player.get("Active") and player.get("Health") == 100.0)
	assert(game.get_node("Hud/Stats").text.contains("Time: 01:00"))
	player.call("TakeDamage",1000.0)
	assert(not player.get("Active"))
	rounds = JSON.parse_string(FileAccess.get_file_as_string("user://score_history.json"))
	assert(rounds.size() == 2 and rounds[1].Result == "GAME OVER")
	# Reload the scene to verify score persistence, not just the current HUD.
	game.free()
	game = load("res://Scenes3D/Main3D.tscn").instantiate()
	root.add_child(game)
	game.call("_PhysicsProcess",61.0)
	rounds = JSON.parse_string(FileAccess.get_file_as_string("user://score_history.json"))
	assert(rounds.size() == 3 and rounds[0].Score == 400)
	assert(game.get_node("Hud").get_child(2).text.contains("400 pts"))
	print("REPAIR_RUNTIME_CHECK_OK: movement, enemy spawning, bullets, heavy HP, scoring, timeout, death, restart, persistence")
	quit()
