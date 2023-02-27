using Godot;
using System;

public partial class DungeonGenerationPathfinding : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public void TestFunction()
	{
		GD.Print("test print");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

public class DNode
{
	Vector3I position;
	DNode previous;
}
