extends SceneTree

# Actual viewport input dispatch; keeps screenshot acceptance separate from control reliability.
func _initialize():
	call_deferred("run_checks")

func tap(code):
	var press = InputEventKey.new()
	press.keycode = code
	press.pressed = true
	Input.parse_input_event(press)
	var release = InputEventKey.new()
	release.keycode = code
	Input.parse_input_event(release)
	Input.flush_buffered_events()
	await process_frame
	await process_frame

func run_checks():
	var game = load(ProjectSettings.get_setting("application/run/main_scene")).instantiate()
	root.add_child(game)
	current_scene = game
	await process_frame
	await process_frame
	var lab = game.get_child(0)
	var shell = lab.get_node("SharedDensityHair")
	var camera = root.get_camera_3d()
	assert(game.find_child("SharedHeadCollider", true, false) == null, "Lab must stay isolated from production salon")
	assert(lab.find_child("snow_customer", true, false) != null, "Licensed hero reaches the actual Lab")
	assert(lab.find_child("polyhaven_barber_chair", true, false) != null, "Licensed barber chair reaches the actual Lab")
	assert(lab.find_child("snow_fps", true, false) != null, "Licensed first-person hand reaches the actual camera")
	assert(shell.material_override is StandardMaterial3D, "No hair shader required")
	var mesh = shell.mesh
	var positions = mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	for position in positions:
		assert(position.is_finite(), "Hair geometry finite")
	for normal in mesh.surface_get_arrays(0)[Mesh.ARRAY_NORMAL]:
		assert(normal.is_finite() and normal.length() > 0.9, "Hair normals finite and normalized")
	var gray = shell.material_override.albedo_color
	await tap(KEY_F5)
	assert(shell.material_override.albedo_color != gray and shell.mesh == mesh, "Color switch preserves geometry")
	await tap(KEY_F5)
	assert(shell.material_override.albedo_color == gray, "Gray is reversible")
	await tap(KEY_F4)
	assert(shell.mesh != mesh, "Alternate hairstyle is distinct")
	await tap(KEY_F4)
	assert(shell.mesh == mesh, "Original hairstyle restores exact mesh")
	await tap(KEY_F1)
	assert(is_equal_approx(camera.fov, 54.0), "A camera input")
	assert(camera.name == "BeautyMatchCamera", "Named review camera is active")
	await tap(KEY_F3)
	assert(is_equal_approx(camera.fov, 49.0), "C camera input")
	await tap(KEY_F2)
	assert(is_equal_approx(camera.fov, 73.0) and is_equal_approx(camera.position.y, 1.7), "B production FOV and eye height")
	await tap(KEY_TAB)
	assert(Input.mouse_mode == Input.MOUSE_MODE_CAPTURED, "Walkthrough captures mouse")
	var before = camera.position
	var press = InputEventKey.new()
	press.physical_keycode = KEY_W
	press.pressed = true
	Input.parse_input_event(press)
	Input.flush_buffered_events()
	await create_timer(0.2).timeout
	press = InputEventKey.new()
	press.physical_keycode = KEY_W
	Input.parse_input_event(press)
	Input.flush_buffered_events()
	assert(camera.position.distance_to(before) > 0.1 and is_equal_approx(camera.position.y, 1.7), "Native WASD moves at grounded eye height")
	await tap(KEY_ESCAPE)
	assert(Input.mouse_mode == Input.MOUSE_MODE_VISIBLE, "Escape releases cursor")
	await tap(KEY_F12)
	assert(shell.mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX] == positions, "Controls never mutate density surface")
	await tap(KEY_F6)
	var credits = lab.find_children("*", "AcceptDialog", true, false)
	assert(credits.size() == 1 and credits[0].dialog_text.contains("CC-BY 4.0"), "Required asset credits are accessible")
	credits[0].hide()
	print("VISUAL_TARGET_INPUT_OK F1/F2/F3/F4/F5/F6/Tab/W/Escape/F12; imported assets and attribution; finite geometry, exact gray/mesh rollback; no authority scene")
	game.call("QuitGracefully", 0)
