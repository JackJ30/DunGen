extends Node
class_name DungeonGenerator

@export_category("Dungeon Generator")
@export_group("Grid Settings")
@export var size : Vector3i
@export_group("Room Settings")
@export var room_count : int
@export var room_max_size : Vector3i
@export var room_min_size : Vector3i
@export_group("Hallway Settings")
@export_range(0.0,1.0) var extra_hallway_chance = 0.125

@onready var visual_gen : DunngeonVisualGenerator = get_node("Visual Generator")
var grid : Grid3D
var rooms : Array[Room]
var hallways : Array[Hallway]
var stairways : Array[Stairway]
var selected_edges : Array[Delaunay3D.Edge]
var delaunay : Delaunay3D
var random : RandomNumberGenerator

func _ready():
	grid = Grid3D.new(size, Vector3i.ZERO, func(position : Vector3i): return Cell.new(position, null))
	visual_gen.grid = grid
	delaunay = Delaunay3D.new()
	random = RandomNumberGenerator.new()
	random.randomize()
	
	place_rooms()
	triangulate()
	create_hallways()
	display_edges(selected_edges)
	pathfind_hallways_CSharp()
	#pathfind_hallways()
	display_cells()

func place_rooms():
	var rooms_spawned : int = 0
	var num_tries : int = 0
	while rooms_spawned < room_count:
		if num_tries > 1000:
			break
		
		num_tries += 1
		var room_position = Vector3i(random.randi_range(0,size.x - 1), random.randi_range(0,size.y - 1), random.randi_range(0,size.z - 1))
		var room_size = Vector3i(random.randi_range(room_min_size.x,room_max_size.x), random.randi_range(room_min_size.y,room_max_size.y), random.randi_range(room_min_size.z,room_max_size.z))
		
		var new_room = Room.new(room_position, room_size)
		var buffer_room = Room.new(room_position + Vector3i(-1,-1,-1), room_size + Vector3i(2,1,2))
		
		var add = true
		for room in rooms:
			if room.intersect(buffer_room):
				add = false
				break
		
		if !add: continue
		if !grid.bounds.encloses(new_room.bounds):
			continue;
		
		rooms.append(new_room)
		new_room.assign_cells(grid)
		rooms_spawned += 1
		num_tries = 0

func triangulate():
	var vertices : Array[Delaunay3D.Vertex]
	
	for room in rooms:
		var new_vertex = Delaunay3D.Vertex.new(room.bounds.position + Vector3(room.bounds.size)/2)
		new_vertex.data = room
		vertices.append(new_vertex)
	
	delaunay.triangulate(vertices)

func create_hallways():
	var edges : Array[Delaunay3D.Edge]
	
	for edge in delaunay.edges:
		edges.append(Delaunay3D.Edge.new(edge.u,edge.v))
	
	selected_edges = Delaunay3D.Edge.Minimum_Spanning_Tree(edges,edges[0].u)
	var remaining_edges = edges.filter(func(n) : return !selected_edges.has(n))
	for edge in remaining_edges:
		if random.randf_range(0.0,1.0) < extra_hallway_chance:
			selected_edges.append(edge)
 
## IMPORTANT INFO - OCCUPIED POSITIONS ARE THE ONES THAT ARE ACTUALLY PLACED
func pathfind_hallways_CSharp():
	var pathfinder_script = load("res://Generation/Scripts/DungeonGenerationPathfinding.cs") # TODO - Refactor script location
	var pathfinder = pathfinder_script.new()
	pathfinder.Initialize(size)
	
	for edge in selected_edges:
		var grid_csharp = []
		for x in range(size.x):
			grid_csharp.append([])
			for y in range(size.y):
				grid_csharp[x].append([])
				for z in range(size.z):
					grid_csharp[x][y].resize(size.z)
					var cell = grid.grab(Vector3i(x,y,z))
					if cell.is_empty():
						grid_csharp[x][y][z] = CellType.None
					elif cell.nav_objects[0] is Room:
						grid_csharp[x][y][z] = CellType.Room
					elif cell.nav_objects[0] is Hallway:
						grid_csharp[x][y][z] = CellType.Hallway
					elif cell.nav_objects[0] is Stairway:
						grid_csharp[x][y][z] = CellType.Stairway
					
		var start_room : Room = edge.u.data
		var end_room : Room = edge.v.data
		
		var start_pos_f = start_room.bounds.get_center()
		var end_pos_f = end_room.bounds.get_center()
		var start_pos = Vector3i(start_pos_f)
		var end_pos = Vector3i(end_pos_f)
		
		var pathfind_results = pathfinder.FindPath(grid_csharp, start_pos, end_pos)
		if pathfind_results == null: continue
		
		var nav : DungeonNavigationObject = null
		var navs_to_add : Array[DungeonNavigationObject]
		for i in range(pathfind_results.size()):
			var procedure = pathfind_results[i]
			
			if procedure[0] == CellType.Hallway as int: #HALLWAY
				if !grid.grab(procedure[2]).is_empty():
					if grid.grab(procedure[2]).nav_objects[0] is Room && !(grid.grab(procedure[1]).nav_objects[0] is Room):
						# if is in room and one before is not, place connection
						if (nav != null): nav.connections.append(DungeonNavigationConnection.new([procedure[1],procedure[2]]))
					continue
				
				if !(nav is Hallway):
					if(nav != null): navs_to_add.append(nav)
					
					nav = Hallway.new()
					nav.start = procedure[2]
				
				nav.occupied_spaces.append(procedure[2])
				nav.end = procedure[2]
				
				# if one before is in room, place connection
				if grid.grab(procedure[1]).nav_objects[0] is Room:
					nav.connections.append(DungeonNavigationConnection.new([procedure[1],procedure[2]]))
				
			elif procedure[0] == CellType.Stairway as int: # STAIRWAY
				if(nav != null): navs_to_add.append(nav)
				
				nav = Stairway.new()
				nav.start = procedure[1]
				nav.end = procedure[2]
				nav.occupied_spaces.append_array(procedure[3])
				navs_to_add.append(nav)
				
				nav.connections.append(DungeonNavigationConnection.new([nav.start,nav.occupied_spaces[0]]))
				nav.connections.append(DungeonNavigationConnection.new([nav.end,nav.occupied_spaces[3]]))
				
				nav = Hallway.new()
				nav.start = procedure[2]
				nav.end = procedure[2]
				nav.occupied_spaces.append(procedure[2])
		if(nav != null): navs_to_add.append(nav)
		
		for add_nav in navs_to_add:
			if add_nav is Hallway:
				hallways.append(add_nav)
			elif add_nav is Stairway:
				stairways.append(add_nav)
			add_nav.assign_cells(grid)

func display_cells():
	for x in grid.data:
		for y in x:
			for cell in y:
				if(cell.nav_objects[0] is Room): visual_gen.display_cell(cell, "room_neighbor_evaluator")
				if(cell.nav_objects[0] is Hallway): visual_gen.display_cell(cell, "hallway_neighbor_evaluator")
				if(cell.nav_objects[0] is Stairway): visual_gen.display_cell(cell, "stairway_neighbor_evaluator")

func display_edges(edges : Array[Delaunay3D.Edge]):
	for edge in edges:
		Draw3D.line(edge.u.position,edge.v.position)

enum CellType {
	None,
	Room,
	Hallway,
	Stairway
}

class Cell:
	var nav_objects : Array[DungeonNavigationObject]
	var collection
	var position : Vector3i
	
	func _init(position : Vector3i, nav_object : DungeonNavigationObject):
		self.position = position
		self.nav_objects = [nav_object]
	
	func add_nav_object(nav_object : DungeonNavigationObject):
		if nav_objects[0] == null:
			nav_objects.clear()
		
		nav_objects.append(nav_object)
	
	func is_empty():
		return nav_objects.is_empty() || nav_objects[0] == null

class DungeonNavigationObject:
	var start : Vector3i
	var end : Vector3i
	var occupied_spaces : Array[Vector3i]
	var connections : Array[DungeonNavigationConnection]
	
	func assign_cells(grid : Grid3D):
		for position in occupied_spaces:
			grid.grab(position).add_nav_object(self)
	
	func check_connections(cell_from,cell_to) -> bool:
		var nav_objects = cell_from.nav_objects
		nav_objects.append_array(cell_to.nav_objects)
		for nav_object in nav_objects:
			for connection in nav_object.connections:
				if connection.connected_positions.has(cell_from.position) && connection.connected_positions.has(cell_to.position):
					return true;
		
		return false;

class Room extends DungeonNavigationObject:
	var bounds : AABB
	
	func _init(position : Vector3i, size : Vector3i):
		bounds = AABB(position,size)
	
	func intersect(other : Room):
		return bounds.intersects(other.bounds)
	
	func assign_cells(grid : Grid3D):
		grid.assign_mass(bounds, func(position : Vector3i): return Cell.new(position, self))

class Hallway extends DungeonNavigationObject:
	func _init():
		pass
	
	func assign_cells(grid : Grid3D):
		for position in occupied_spaces:
			grid.grab(position).add_nav_object(self)

class Stairway extends DungeonNavigationObject:
	func _init():
		pass

class DungeonNavigationConnection:
	var connected_positions : Array[Vector3i]
	
	func _init(connected_positions : Array[Vector3i]):
		self.connected_positions = connected_positions
