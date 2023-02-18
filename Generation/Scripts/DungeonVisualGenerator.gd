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
	
	var neighbors = [int(has_neighbor(cell, Vector3i(0,0,1),Callable(self,"room_neighbor_evaluator"))),int(has_neighbor(cell, Vector3i(0,0,-1),Callable(self,"room_neighbor_evaluator"))),int(has_neighbor(cell, Vector3i(1,0,0),Callable(self,"room_neighbor_evaluator"))),int(has_neighbor(cell, Vector3i(-1,0,0),Callable(self,"room_neighbor_evaluator")))]
	var num_neighbors = neighbors[0] + neighbors[1] + neighbors[2] + neighbors[3]
	
	if(num_neighbors != 4):
		var new_wall = null
		var tile_direction
		
		if num_neighbors == 0:
			new_wall = room_wall0.instantiate()
			tile_direction = Vector3i.ZERO
		if num_neighbors == 1:
			new_wall = room_wall1.instantiate()
			tile_direction = Vector3i(0,0,1)
		if num_neighbors == 2:
			new_wall = room_wall2.instantiate()
			tile_direction = Vector3i(1,0,1)
		if num_neighbors == 3:
			new_wall = room_wall3.instantiate()
			tile_direction = Vector3i(0,0,1)
	
		add_child(new_wall)
		new_wall.global_position = grid_to_world_pos_floor(cell.position, cell_scale)
		
		var direction = Vector3(neighbors[0] - neighbors[1],0,neighbors[2]-neighbors[3])
		tile_direction = Vector3(tile_direction.x, 0, -tile_direction.z) # fix rotation issue
		var angle = atan2(tile_direction.x, tile_direction.z) - atan2(direction.x, direction.z) # calculate angle

		new_wall.global_rotate(Vector3.UP, angle) # apply rotation
		
		#var angle = atan2(tile_direction.x,tile_direction.y) - atan2(direction.x,direction.y)
		
		#new_wall.global_rotate(Vector3.UP, Vector3(tile_direction).angle_to(direction))
	#var neighbors = [has_neighbor(cell, Vector3i(0,0,-1), Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i(0,0,1), Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i(1,0,0), Callable(self,"room_neighbor_evaluator")), has_neighbor(cell, Vector3i(-1,0,0), Callable(self,"room_neighbor_evaluator"))]
	#var num_neighbors = (int(neighbors[0]) + int(neighbors[1]) + int(neighbors[2]) + int(neighbors[3]))
	
	#if num_neighbors != 4:
		#var new_wall = null
		#var angle
		#if num_neighbors == 0: 
		#	new_wall = room_wall0.instantiate()
		#	angle = Vector3.ZERO
		#if num_neighbors == 1:
		#	new_wall = room_wall1.instantiate()
		#	angle = Vector3(0,0,1)
		#if num_neighbors == 2:
		#	new_wall = room_wall2.instantiate()
		#	angle = Vector3(1,0,1)
		#if num_neighbors == 3:
		#	new_wall = room_wall3.instantiate()
		#	angle = Vector3(0,0,1)
		
		#add_child(new_wall)
		#new_wall.global_position = grid_to_world_pos_floor(cell.position, cell_scale)
		
		#var direction = Vector3(int(neighbors[2]) - int(neighbors[3]), 0, int(neighbors[1]) - int(neighbors[0])) # This is issue
		#new_wall.global_rotate(Vector3.UP, direction.angle_to(angle))
	
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
