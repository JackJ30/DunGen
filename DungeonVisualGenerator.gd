extends Node3D

@export var room_visual : PackedScene
@export var hallway_visual : PackedScene

func display_room_cell(cell):
	var new_cell = room_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_hallway_cell(cell):
	var new_cell = hallway_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)
