extends RefCounted
class_name Grid3D

var data = []
var size : Vector3i
var offset : Vector3i
var bounds : AABB

func _init(size : Vector3i, offset : Vector3i, initial_value : Callable):
	self.size = size
	self.offset = offset
	bounds = AABB(offset, size)
	
	data.resize(size.x)
	for x in range(size.x):
		data[x]=[]
		data[x].resize(size.y)
		for y in range(size.y):
			data[x][y] = []
			data[x][y].resize(size.z)
			for z in range(size.z):
				data[x][y][z] = initial_value.call(Vector3i(x,y,z))

func get_index(position : Vector3i) -> int:
	return position.x + (size.x * position.y) + (size.x * size.y * position.z);

func in_bounds(position : Vector3i) -> bool:
	var pos_offset = position + offset
	
	#return bounds.has_point(pos_offset + Vector3i.ONE)
	return (pos_offset.x < size.x && pos_offset.x >= 0) && (pos_offset.y < size.y && pos_offset.y >= 0) && (pos_offset.z < size.z && pos_offset.z >= 0)

func grab(position : Vector3i) -> Object:
	var pos_offset = position + offset
	return data[pos_offset.x][pos_offset.y][pos_offset.z]

func assign(position : Vector3i, to : Object):
	data[position.x][position.y][position.z] = to

func assign_mass(bounds : AABB, to : Callable):
	if !self.bounds.encloses(bounds): return
	
	for x in range(bounds.position.x, bounds.position.x + bounds.size.x):
		for y in range(bounds.position.y, bounds.position.y + bounds.size.y):
			for z in range(bounds.position.z, bounds.position.z + bounds.size.z):
				data[x][y][z] = to.call(Vector3i(x,y,z))

func assign_all(to : Callable):
	for x in data:
		for y in data[x]:
			for z in data[x][y]:
				data[x][y][z] = to.call(Vector3i(x,y,z))
