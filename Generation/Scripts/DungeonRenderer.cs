using Godot;
using System;
using System.Collections.Generic;

public partial class DungeonRenderer : Node
{
	[Export]
	Vector2 CellScale = new Vector2(1.52f,2.35f);
	
	[ExportCategory("Visual Assets")]
	[ExportGroup("Room Assets")]
	[Export] PackedScene RoomFloor;
	[Export] PackedScene RoomCeiling;
	[Export] PackedScene RoomWall0;
	[Export] PackedScene RoomWall1;
	[Export] PackedScene RoomWall2;
	[Export] PackedScene RoomWall2_2;
	[Export] PackedScene RoomWall3;
	[Export] PackedScene Stair;
	
	private Grid3D<Cell> _grid;
	
	public void Initialize(Grid3D<Cell> grid)
	{
		_grid = grid;
	}
	
	public void DisplayCell(Cell cell)
	{
		
		List<Node3D> newObjects = new List<Node3D>();
		if (!HasNeighbor(cell, Vector3I.Down)) newObjects.Add(RoomFloor.Instantiate<Node3D>());
		if (!HasNeighbor(cell, Vector3I.Up)) newObjects.Add(RoomCeiling.Instantiate<Node3D>());
		
		List<int> neighbors = new List<int>();
		neighbors.Add(HasNeighbor(cell, new Vector3I(0,0,1)) ? 1 : 0);
		neighbors.Add(HasNeighbor(cell, new Vector3I(0,0,-1)) ? 1 : 0);
		neighbors.Add(HasNeighbor(cell, new Vector3I(1,0,0)) ? 1 : 0);
		neighbors.Add(HasNeighbor(cell, new Vector3I(-1,0,0)) ? 1 : 0);
		Vector3 direction = new Vector3(neighbors[2] - neighbors[3], 0, neighbors[0] - neighbors[1]);
		int numNeighbors = neighbors[0] + neighbors[1] + neighbors[2] + neighbors[3];
		
		if (numNeighbors != 4)
		{
			Node3D newWall = null;
			Vector3 tileDirection = Vector3.Zero;
			
			if (numNeighbors == 0)
			{
				newWall = RoomWall0.Instantiate<Node3D>();
				tileDirection = Vector3.Zero;
			}
			if (numNeighbors == 1)
			{
				newWall = RoomWall1.Instantiate<Node3D>();
				tileDirection = new Vector3(0,0,1);
			}
			if (numNeighbors == 2)
			{
				if (direction.LengthSquared() == 0) // Straight Line
				{
					newWall = RoomWall2_2.Instantiate<Node3D>();
					tileDirection = new Vector3(0,0,1);
					
					if (neighbors[0] == 1 || neighbors[1] == 1) direction.Z = 1;
					if (neighbors[2] == 1 || neighbors[3] == 1) direction.X = 1;
				}
				else // Corner
				{
					newWall = RoomWall2.Instantiate<Node3D>();
					tileDirection = new Vector3(1,0,1);
				}
			}
			if (numNeighbors == 3)
			{
				newWall = RoomWall3.Instantiate<Node3D>();
				tileDirection = new Vector3(0,0,1);
			}
			
			if (newWall == null) return; // Should never happen
			
			AddChild(newWall);
			newWall.GlobalPosition = GridToWorldPos((Vector3)cell.Position, CellScale, true);
			
			float angle = (float)Math.Atan2(tileDirection.Z, tileDirection.X) - (float)Math.Atan2(direction.Z, direction.X);
			if (angle < 0) angle += 2*(float)Math.PI;
			
			Vector3 newRotation = newWall.GlobalRotation;
			newRotation.Y = angle;
			newWall.GlobalRotation = newRotation;
		}
		
		foreach (Node3D node in newObjects)
		{
			AddChild(node);
			node.GlobalPosition = GridToWorldPos((Vector3)cell.Position, CellScale, true);
		}
	}
	
	public void DisplayStair(Stairway stairway)
	{
		Node3D newStairway = Stair.Instantiate<Node3D>();
		AddChild(newStairway);
		float meanPositionX = 0f;
		float meanPositionY = 0f;
		float meanPositionZ = 0f;
		
		foreach (Vector3I position in stairway.GetOccupiedPositions())
		{
			meanPositionX += (float)position.X;
			meanPositionY += (float)position.Y;
			meanPositionZ += (float)position.Z;
		}
		
		meanPositionX /= stairway.GetOccupiedPositions().Length;
		meanPositionY /= stairway.GetOccupiedPositions().Length;
		meanPositionZ /= stairway.GetOccupiedPositions().Length;
		
		newStairway.GlobalPosition = GridToWorldPos(new Vector3(meanPositionX, meanPositionY, meanPositionZ), CellScale, true);
		Vector3 trueDirection = (Vector3)(stairway.Direction * new Vector3I(1,0,1));
		if (stairway.Start.Y > stairway.End.Y) trueDirection = trueDirection * -1.0f;
		float angle = (float)Math.Atan2(1f,0f) - (float)Math.Atan2(trueDirection.Z, trueDirection.X);
		if (angle < 0.0f) angle += 2f*(float)Math.PI;
		
		Vector3 newRotation = newStairway.GlobalRotation;
		newRotation.Y = angle;
		newStairway.GlobalRotation = newRotation;
	}
	
	bool HasNeighbor(Cell cell, Vector3I offset)
	{
		if (!_grid.InBounds(cell.Position + offset)) return false;
		
		foreach (DungeonLevelSegment segment in cell.Segments)
		{
			if (segment.NeighborEvaluator(cell, _grid[cell.Position + offset], offset))
			{
				return true;
			}
		}
		
		return false;
	}
	
	Vector3 GridToWorldPos(Vector3 pos, Vector2 scale, bool floor)
	{
		Vector3 result = (pos * new Vector3(scale.X,scale.Y,scale.X));
		if (floor) result += (Vector3.Down * (scale.Y / 2));
		return result;
	}
		
	Vector3 GridToWorldPos(Vector3 pos, Vector2 scale)
	{
		return GridToWorldPos(pos, scale, false);
	}
}
