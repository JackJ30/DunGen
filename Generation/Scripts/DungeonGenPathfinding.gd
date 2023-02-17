extends RefCounted
class_name DungeonGenPathfinder

var grid : Grid3D
var queue : PriorityQueue
var closed : Util.DictionarySet
var stack : Array[Vector3i]

var neighbors = [
	Vector3i(1, 0, 0),
	Vector3i(-1, 0, 0),
	Vector3i(0, 0, 1),
	Vector3i(0, 0, -1),

	Vector3i(3, 1, 0),
	Vector3i(-3, 1, 0),
	Vector3i(0, 1, 3),
	Vector3i(0, 1, -3),

	Vector3i(3, -1, 0),
	Vector3i(-3, -1, 0),
	Vector3i(0, -1, 3),
	Vector3i(0, -1, -3)
] # TODO - Maybe make neighbors dynamically generated based on system that allows new types of vertical movement

func _init(size : Vector3i):
	grid = Grid3D.new(size,Vector3i.ZERO,func(position : Vector3i): return DNode.new(position))
	
	queue = PriorityQueue.new()
	closed = Util.DictionarySet.new()

func reset_nodes():
	for x in grid.data:
		for y in x:
			for z in y:
				var node = z
				node.previous = null
				node.cost = INF
				node.previous_set.clear()

func find_path(start : Vector3i, end : Vector3i, costFunction : Callable):
	reset_nodes()
	queue.clear()
	closed.clear()
	
	grid.grab(start).cost = 0.0
	queue.enqueue(grid.grab(start),0.0)
	
	while !queue.empty():
		var node : DNode = queue.dequeue()
		closed.add(node)
		
		if node.position == end:
			return reconstruct_path(node)
		
		for offset in neighbors:
			if !grid.in_bounds(node.position + offset): 
				continue
			var neighbor : DNode = grid.grab(node.position + offset)
			if closed.contains(neighbor): 
				continue
			if node.previous_set.contains(neighbor.position): 
				continue
			
			var path_cost : PathCost = costFunction.call(node, neighbor, start, end)
			if !path_cost.traversable: 
				continue
			
			if path_cost.is_stair: # TODO - REFACTOR FOR VERTICAL MOVEMENT REWORK (THIS CODE PIECE CHECKS ALL FOUR CELLS IN THE STAIRWAY)
				var xDir : int = clamp(offset.x, -1, 1)
				var zDir : int = clamp(offset.z, -1, 1)
				var vertical_offset = Vector3i(0, offset.y, 0)
				var horizontal_offset = Vector3i(xDir, 0, zDir)
				
				if (
					node.previous_set.contains(node.position + horizontal_offset) ||
					node.previous_set.contains(node.position + horizontal_offset * 2) ||
					node.previous_set.contains(node.position + vertical_offset + horizontal_offset) ||
					node.previous_set.contains(node.position + vertical_offset + horizontal_offset * 2)
				): continue
				
			var new_cost = path_cost.cost + node.cost
			
			if new_cost < neighbor.cost:
				neighbor.previous = node
				neighbor.cost = new_cost
				
				if queue.contains(node):
					queue.update_priority(node, new_cost)
				else:
					queue.enqueue(neighbor, neighbor.cost)
				
				neighbor.previous_set.clear()
				neighbor.previous_set.add_array(node.previous_set.array.keys())
				neighbor.previous_set.add(node.position)
				
				if path_cost.is_stair: # TODO - REFACTOR FOR VERTICAL MOVEMENT REWORK (THIS CODE PIECE CHECKS ALL FOUR CELLS IN THE STAIRWAY)
					var xDir : int = clamp(offset.x, -1, 1)
					var zDir : int = clamp(offset.z, -1, 1)
					var vertical_offset = Vector3i(0, offset.y, 0)
					var horizontal_offset = Vector3i(xDir, 0, zDir)
					
					neighbor.previous_set.add(node.position + horizontal_offset)
					neighbor.previous_set.add(node.position + horizontal_offset * 2)
					neighbor.previous_set.add(node.position + vertical_offset + horizontal_offset)
					neighbor.previous_set.add(node.position + vertical_offset + horizontal_offset * 2)
	return null

func reconstruct_path(start_node : DNode) -> Array[Vector3i]:
	var result : Array[Vector3i]
	var node = start_node
	
	while node != null:
		stack.push_front(node.position)
		node = node.previous
	
	while stack.size() > 0:
		result.append(stack.pop_front())
	
	return result

class DNode:
	var position : Vector3i
	var previous : DNode
	var previous_set : Util.DictionarySet
	var cost : float
	
	func _init(position : Vector3i):
		self.position = position
		self.previous_set = Util.DictionarySet.new()

#TODO - Refactor pathcost to have CostType instead of traversable and is_stair
class PathCost: # TODO - THIS COULD CAUSE ISSUES BECAUSE OF INIT, I MADE EDUCATED GUESSES ABOUT THE DEFAULTS, DON"T REMEMBER HOW UNITY DOES IT
	var traversable : bool
	var is_stair : bool
	var cost : float
	
	func _init():
		is_stair = false
		traversable = false
