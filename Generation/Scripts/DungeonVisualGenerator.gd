extends Node3D
class_name DunngeonVisualGenerator

@export var room_visual : PackedScene
@export var hallway_visual : PackedScene
@export var stair_visual : PackedScene
@export var cell_scale = Vector2(1.52,2.35)

@export_category("Visual Assets")
@export_group("Room Assets")
@export var room_floor : PackedScene
@export var room_ceiling : PackedScene
@export var room_wall0 : PackedScene
@export var room_wall1 : PackedScene
@export var room_wall2 : PackedScene
@export var room_wall3 : PackedScene

var grid : Grid3D

func room_neighbor_evaluator(type : DungeonGenerator.CellType):
	return type == DungeonGenerator.CellType.Room

func display_room_cell(cell):
	var new_assets = []
	if !has_neighbor(cell, Vector3i.DOWN, Callable(self,"room_neighbor_evaluator")):
		new_assets.append(room_floor.instantiate())
	if !has_neighbor(cell, Vector3i.UP, Callable(self,"room_neighbor_evaluator")):
		new_assets.append(room_ceiling.instantiate())
	
	var neighbors = [has_neighbor(cell, Vector3i.BACK, Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i.FORWARD, Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i.RIGHT, Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i.LEFT, Callable(self,"room_neighbor_evaluator"))]
	var num_open = (int(neighbors[0]) + int(neighbors[1]) + int(neighbors[2]) + int(neighbors[3]))
	
	if num_open != 4:
		var new_wall = null
		if num_open == 0: new_wall = room_wall0.instantiate()
		if num_open == 1: new_wall = room_wall1.instantiate()
		if num_open == 2: new_wall = room_wall2.instantiate()
		if num_open == 3: new_wall = room_wall3.instantiate()
		
		add_child(new_wall)
		new_wall.global_position = grid_to_world_pos_floor(cell.position, cell_scale)
		
		var direction = Vector3(int(neighbors[2]) - int(neighbors[3]), 0, int(neighbors[1]) - int(neighbors[2]))
		new_wall.global_rotate(Vector3.UP,Vector3.BACK.angle_to(direction))
	
	for node in new_assets:
		add_child(node)
		node.global_position = grid_to_world_pos_floor(cell.position, cell_scale)

func display_hallway_cell(cell):
	var new_cell = hallway_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_stair_cell(cell):
	var new_cell = stair_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func display_room_cell_simple(cell):
	var new_cell = room_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func has_neighbor(cell, offset : Vector3i, is_valid_type : Callable) -> bool:
	if !grid.in_bounds(cell.position + offset): return false
	
	if is_valid_type.call(grid.grab(cell.position + offset).cell_type):
		return true
	
	return false

func grid_to_world_pos_floor(pos : Vector3i, scale : Vector2):
	return Vector3(pos) * Vector3(scale.x,scale.y,scale.x) + (Vector3.DOWN * (scale.y / 2))
