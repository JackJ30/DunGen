extends MeshInstance3D

var cell : DungeonGenerator.Cell

func _enter_tree():
	global_position = cell.position
