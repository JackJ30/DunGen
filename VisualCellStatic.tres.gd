extends MeshInstance3D

var cell : DungeonGenerator.Cell

func _enter_tree():
	global_position = cell.position

func on_input_event(camera, event, click_position, click_normal, shape_idx):
	var mouse_click = event as InputEventMouseButton
	if mouse_click and mouse_click.button_index == 1 and mouse_click.pressed:
		print("clicked")
