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
@export var room_wall2_2 : PackedScene
@export var room_wall3 : PackedScene

var grid : Grid3D

func room_neighbor_evaluator(cell, delta):
	if(cell.is_empty()): return false
	return (cell.nav_objects[0] is DungeonGenerator.Room || (
		cell.nav_objects[0] is DungeonGenerator.Hallway && (cell.nav_objects[0].start == cell.position) || (cell.nav_objects[0].end == cell.position))
		)

func hallway_neighbor_evaluator(cell, delta):
	if(cell.is_empty()): return false
	return (cell.nav_objects[0] is DungeonGenerator.Hallway) && (delta.y == 0)

func display_cell(cell, evaluator : String):
	var new_assets = []
	if !has_neighbor(cell, Vector3i.DOWN, Callable(self,evaluator)):
		new_assets.append(room_floor.instantiate())
	if !has_neighbor(cell, Vector3i.UP, Callable(self,evaluator)):
		new_assets.append(room_ceiling.instantiate())
	
	Callable(self,evaluator)
	
	var neighbors = []
	neighbors.append(int(has_neighbor(cell, Vector3i(0,0,1),Callable(self,evaluator))))
	neighbors.append(int(has_neighbor(cell, Vector3i(0,0,-1),Callable(self,evaluator))))
	neighbors.append(int(has_neighbor(cell, Vector3i(1,0,0),Callable(self,evaluator))))
	neighbors.append(int(has_neighbor(cell, Vector3i(-1,0,0),Callable(self,evaluator))))
	var direction = Vector3(neighbors[2] - neighbors[3], 0, neighbors[0] - neighbors[1])
	var num_neighbors = neighbors[0] + neighbors[1] + neighbors[2] + neighbors[3]
	
	if(num_neighbors != 4):
		var new_wall
		var tile_direction
		if num_neighbors == 0:
			new_wall = room_wall0.instantiate()
			tile_direction = Vector3.ZERO
		if num_neighbors == 1:
			new_wall = room_wall1.instantiate()
			tile_direction = Vector3(0,0,1)
		if num_neighbors == 2:
			if direction.length_squared() == 0: # Straight Line
				new_wall = room_wall2_2.instantiate()
				tile_direction = Vector3(0,0,1)
				
				if neighbors[0] == 1 || neighbors[1] == 1:
					direction.z = 1
				if neighbors[2] == 1 || neighbors[2] == 1:
					direction.x = 1
			else: # Corner
				new_wall = room_wall2.instantiate()
				tile_direction = Vector3(1,0,1)
		if num_neighbors == 3:
			new_wall = room_wall3.instantiate()
			tile_direction = Vector3(0,0,1)
	
		add_child(new_wall)
		new_wall.global_position = grid_to_world_pos_floor(cell.position, cell_scale)
			
		var angle = atan2(tile_direction.z, tile_direction.x) - atan2(direction.z, direction.x)# direction.angle_to(tile_direction)
		if angle < 0: angle += 2*PI
		
		new_wall.global_rotation.y = angle
		
	for node in new_assets:
		add_child(node)
		node.global_position = grid_to_world_pos_floor(cell.position, cell_scale)

func display_hallway_cell(cell):
	var new_cell = hallway_visual.instantiate()
	new_cell.global_position = grid_to_world_pos_floor(cell.position, cell_scale)
	add_child(new_cell)

func display_stair_cell(cell):
	var new_cell = stair_visual.instantiate()
	add_child(new_cell)
	new_cell.global_position = grid_to_world_pos_floor(cell.position, cell_scale)

func display_room_cell_simple(cell):
	var new_cell = room_visual.instantiate()
	new_cell.cell = cell
	add_child(new_cell)

func has_neighbor(cell, offset : Vector3i, is_valid_type : Callable) -> bool:
	if !grid.in_bounds(cell.position + offset): return false
	
	if is_valid_type.call(grid.grab(cell.position + offset), offset):
		return true
	
	return false

func grid_to_world_pos_floor(pos : Vector3i, scale : Vector2):
	return Vector3(pos) * Vector3(scale.x,scale.y,scale.x) + (Vector3.DOWN * (scale.y / 2))
