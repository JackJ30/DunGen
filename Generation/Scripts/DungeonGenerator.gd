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
var stairs : Array[Stairway]
var selected_edges : Array[Delaunay3D.Edge]
var delaunay : Delaunay3D
var random : RandomNumberGenerator

func _ready():
	grid = Grid3D.new(size, Vector3i.ZERO, func(position : Vector3i): return Cell.new(CellType.None,position))
	visual_gen.grid = grid
	delaunay = Delaunay3D.new()
	random = RandomNumberGenerator.new()
	random.randomize()
	
	place_rooms()
	triangulate()
	create_hallways()
	display_edges(selected_edges)
	pathfind_hallways()
	display_cells()

func place_rooms():
	var rooms_spawned : int = 0
	var num_tries : int = 0
	while rooms_spawned < room_count:
		if num_tries > 500:
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
		grid.assign_mass(new_room.bounds, func(position : Vector3i): return Cell.new(CellType.Room, position))
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

func pathfind_hallways():
	var pathfinder = DungeonGenPathfinder.new(size)
	var test_pathfinder_script = load("res://Generation/Scripts/DungeonGenerationPathfinding.cs") # TODO - Refactor script location
	var test_pathfinder = test_pathfinder_script.new()
	
	for edge in selected_edges:
		var start_room : Room = edge.u.data
		var end_room : Room = edge.v.data
		
		var start_pos_f = start_room.bounds.get_center()
		var end_pos_f = end_room.bounds.get_center()
		var start_pos = Vector3i(start_pos_f)
		var end_pos = Vector3i(end_pos_f)
		
		var path = pathfinder.find_path(start_pos, end_pos, Callable(self, "cost_function"))
		
		if path != null:
			for i in range(path.size()):
				var current = path[i]
				if grid.grab(current).cell_type == CellType.None: grid.grab(current).cell_type = CellType.Hallway
				if i > 0:
					var previous = path[i - 1]
					var delta = current - previous
					
					if delta.y != 0:
						# TODO - REFACTOR FOR VERTICAL MOVEMENT REWORK (THIS CODE PIECE CHECKS ALL FOUR CELLS IN THE STAIRWAY)
						var xDir : int = clamp(delta.x, -1, 1)
						var zDir : int = clamp(delta.z, -1, 1)
						var vertical_offset = Vector3i(0, delta.y, 0)
						var horizontal_offset = Vector3i(xDir, 0, zDir)
						
						grid.grab(previous + horizontal_offset).cell_type = CellType.Stairway;
						grid.grab(previous + horizontal_offset * 2).cell_type = CellType.Stairway;
						grid.grab(previous + vertical_offset + horizontal_offset).cell_type = CellType.Stairway;
						grid.grab(previous + vertical_offset + horizontal_offset * 2).cell_type = CellType.Stairway;

func cost_function(a : DungeonGenPathfinder.DNode, b : DungeonGenPathfinder.DNode, start_pos : Vector3i, end_pos : Vector3i):
	var path_cost = DungeonGenPathfinder.PathCost.new()
	var delta = b.position - a.position
	
	if delta.y == 0:
		path_cost.cost = Vector3(b.position).distance_to(Vector3(end_pos))
		
		if grid.grab(b.position).cell_type == CellType.Stairway: return path_cost
		elif grid.grab(b.position).cell_type == CellType.Room: path_cost.cost += 5 # TODO - maybe make rooms non-traversable
		elif grid.grab(b.position).cell_type == CellType.None: path_cost.cost += 1
		
		path_cost.traversable = true
	else:
		if ((grid.grab(a.position).cell_type != CellType.None && grid.grab(a.position).cell_type != CellType.Hallway) || (grid.grab(b.position).cell_type != CellType.None && grid.grab(b.position).cell_type != CellType.Hallway)):
			return path_cost
		
		path_cost.cost = 100 + Vector3(b.position).distance_to(Vector3(end_pos))
		# TODO - REFACTOR FOR VERTICAL MOVEMENT REWORK (THIS CODE PIECE CHECKS ALL FOUR CELLS IN THE STAIRWAY)
		var xDir : int = clamp(delta.x, -1, 1)
		var zDir : int = clamp(delta.z, -1, 1)
		var vertical_offset = Vector3i(0, delta.y, 0)
		var horizontal_offset = Vector3i(xDir, 0, zDir)
		
		if (
			!grid.in_bounds(a.position + vertical_offset) ||
			!grid.in_bounds(a.position + horizontal_offset) ||
			!grid.in_bounds(a.position + vertical_offset + horizontal_offset)
		): return path_cost
		
		if(
			grid.grab(a.position + horizontal_offset).cell_type != CellType.None ||
			grid.grab(a.position + horizontal_offset * 2).cell_type != CellType.None ||
			grid.grab(a.position + vertical_offset + horizontal_offset).cell_type != CellType.None ||
			grid.grab(a.position + vertical_offset + horizontal_offset * 2).cell_type != CellType.None
		): return path_cost
		
		path_cost.traversable = true
		path_cost.is_stair = true
	
	return path_cost

func display_cells():
	for x in grid.data:
		for y in x:
			for cell in y:
				if(cell.cell_type == CellType.Room): visual_gen.display_room_cell(cell)
				if(cell.cell_type == CellType.Hallway): visual_gen.display_room_cell(cell)
				if(cell.cell_type == CellType.Stairway): visual_gen.display_room_cell(cell)

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
	var cell_type : CellType
	var collection
	var position : Vector3i
	
	func _init(cell_type : CellType, position : Vector3i):
		self.cell_type = cell_type
		self.position = position

class Room:
	var bounds : AABB
	
	func _init(position : Vector3i, size : Vector3i):
		bounds = AABB(position,size)
	
	func intersect(other : Room):
		return bounds.intersects(other.bounds)

class Hallway:
	func _init():
		pass

class Stairway:
	func _init():
		pass
