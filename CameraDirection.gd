extends RichTextLabel

@onready var camera = get_parent().get_parent()

func _process(delta):
	print(camera)
	
	var direction = get_parent().get_parent().get_global_transform().basis.z
	var px_dist = direction.distance_squared_to(Vector3(1,0,0))
	var nx_dist = direction.distance_squared_to(Vector3(-1,0,0))
	var py_dist = direction.distance_squared_to(Vector3(0,1,0))
	var ny_dist = direction.distance_squared_to(Vector3(0,-1,0))
	var pz_dist = direction.distance_squared_to(Vector3(0,0,1))
	var nz_dist = direction.distance_squared_to(Vector3(0,0,-1))
	
	var minimum = min(px_dist,nx_dist,py_dist,ny_dist,pz_dist,nz_dist)
	
	if minimum == px_dist: text = "-X"
	if minimum == nx_dist: text = "+X"
	if minimum == py_dist: text = "-Y"
	if minimum == ny_dist: text = "+Y"
	if minimum == pz_dist: text = "-Z"
	if minimum == nz_dist: text = "+Z"
