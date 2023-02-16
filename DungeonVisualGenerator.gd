extends Node3D
class_name DunngeonVisualGenerator

@export var room_visual : PackedScene
@export var hallway_visual : PackedScene
@export var stair_visual : PackedScene

func display_room_cell(cell):
	var new_cell = room_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_hallway_cell(cell):
	var new_cell = hallway_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_stair_cell(cell):
	var new_cell = stair_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)
