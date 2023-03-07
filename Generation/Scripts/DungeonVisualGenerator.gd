extends Node3D
class_name DunngeonVisualGenerator

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
@export var hallway_stair : PackedScene

var grid : Grid3D

func room_neighbor_evaluator(cell_from, cell_to, delta):
	if(cell_to.is_empty()): return false
	
	if(cell_from.nav_objects[0].check_connections(cell_from,cell_to)):
		return true;
	
	if cell_from.nav_objects[0] != cell_to.nav_objects[0]: return false # IF HAS SEGMENT !!!!!!!!!!!!!!!!!
		
	if !(cell_to.nav_objects[0] is DungeonGenerator.Room): return false
	return true

func hallway_neighbor_evaluator(cell_from, cell_to, delta):
	if(cell_to.is_empty()): return false
	if(delta.y != 0): return false
	
	if(cell_from.nav_objects[0].check_connections(cell_from,cell_to)):
		return true;
	
	return (cell_to.nav_objects[0] is DungeonGenerator.Hallway)

func stairway_neighbor_evaluator(cell_from, cell_to, delta):
	if(cell_to.is_empty()): return false
	
	if(cell_from.nav_objects[0].check_connections(cell_from,cell_to)):
		return true;
	
	if cell_from.nav_objects[0] != cell_to.nav_objects[0]: return false
	
	return true;

func display_cell(cell, evaluator : String):
	
	print("start neighbor check")
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
	
	
	print("end neighbor check")
	
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
		new_wall.global_position = grid_to_world_pos_floor(Vector3(cell.position), cell_scale)
			
		var angle = atan2(tile_direction.z, tile_direction.x) - atan2(direction.z, direction.x)# direction.angle_to(tile_direction)
		if angle < 0: angle += 2*PI
		
		new_wall.global_rotation.y = angle
		
	for node in new_assets:
		add_child(node)
		node.global_position = grid_to_world_pos_floor(Vector3(cell.position), cell_scale) 

func display_stairway(stairway : DungeonGenerator.Stairway):
	var new_stairway = hallway_stair.instantiate()
	add_child(new_stairway)
	
	var mean_position_x = 0
	var mean_position_y = 0
	var mean_position_z = 0
	
	for position in stairway.occupied_spaces:
		mean_position_x += position.x as float
		mean_position_y += position.y as float
		mean_position_z += position.z as float
	
	mean_position_x /= stairway.occupied_spaces.size()
	mean_position_y /= stairway.occupied_spaces.size()
	mean_position_z /= stairway.occupied_spaces.size()
	
	new_stairway.global_position = grid_to_world_pos_floor(Vector3(mean_position_x, mean_position_y, mean_position_z), cell_scale)
	var trueDirection = stairway.directionXZ
	if stairway.start.y > stairway.end.y: trueDirection = trueDirection * -1.0
	var angle = atan2(1,0) - atan2(trueDirection.z, trueDirection.x)# direction.angle_to(tile_direction)
	if angle < 0: angle += 2*PI
	
	new_stairway.global_rotation.y = angle

func has_neighbor(cell, offset : Vector3i, is_valid_type : Callable) -> bool:
	if !grid.in_bounds(cell.position + offset): return false
	
	if is_valid_type.call(cell, grid.grab(cell.position + offset), offset):
		return true
	
	return false

func grid_to_world_pos_floor(pos : Vector3, scale : Vector2):
	return Vector3(pos) * Vector3(scale.x,scale.y,scale.x) + (Vector3.DOWN * (scale.y / 2))

func grid_to_world_pos(pos : Vector3, scale : Vector2):
	return Vector3(pos) * Vector3(scale.x,scale.y,scale.x)
