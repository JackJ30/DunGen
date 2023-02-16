extends StaticBody3D

var cell : DungeonGenerator.Cell
var value

func _enter_tree():
	global_position = cell.position

func _on_input_event(camera, event, position, normal, shape_idx):
	var mouse_click = event as InputEventMouseButton
	if mouse_click and mouse_click.button_index == 1 and mouse_click.pressed:
		print(value)
