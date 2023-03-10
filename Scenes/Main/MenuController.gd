extends Node3D

var _server_scene = preload("res://Entities/Multiplayer/Client.tscn")
var _client_scene = preload("res://Entities/Multiplayer/Server.tscn")

func _on_button_pressed(host:bool):
		if host:
			$Control/Label.text = "Server Side"
			self.add_child(_server_scene.instantiate())
		else:
			$Control/Label.text = "Client Side"
			self.add_child(_client_scene.instantiate())

		$Buttons.queue_free()
