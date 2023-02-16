extends Node3D
class_name DungeonVisualGenerator

@export var room_visual : PackedScene
@export var hallway_visual : PackedScene

@export var tips : PackedScene
@export var current : PackedScene
@export var eligble : PackedScene
@export var ineligble : PackedScene

func display_room_cell(cell):
	var new_cell = room_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_hallway_cell(cell):
	var new_cell = hallway_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)
	

func clear():
	for i in range(0, get_child_count()):
		get_child(i).queue_free()

func display_tips_cell(cell):
	var new_cell = tips.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_current_cell(cell):
	var new_cell = current.instantiate()
	new_cell.cell = cell
	add_child(new_cell)
	
func display_eligble_cell(cell, value):
	var new_cell = eligble.instantiate()
	new_cell.cell = cell
	new_cell.value = value
	add_child(new_cell)
	
func display_ineligble_cell(cell):
	var new_cell = ineligble.instantiate()
	new_cell.cell = cell
	add_child(new_cell)
	
