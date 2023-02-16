extends Node


# Called when the node enters the scene tree for the first time.
func _ready():
	var t = Delaunay3D.Triangle.new(Delaunay3D.Vertex.new(),Delaunay3D.Vertex.new(),Delaunay3D.Vertex.new())
	var g = Grid3D.new(Vector3i.ONE * 3, Vector3i.ZERO, null)
	print(g.in_bounds(Vector3i(2,3,2)))

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
