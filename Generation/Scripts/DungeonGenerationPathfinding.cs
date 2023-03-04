using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum CellType {
	None,
	Room,
	Hallway,
	Stairway
}

public partial class DungeonGenerationPathfinding : Node
{
	Grid3D<DNode> grid;
	Grid3D<CellType> existingGrid;
	SimplePriorityQueue<DNode, float> queue;
	HashSet<DNode> closed;
	Stack<Vector3I> stack;
	
	public void Initialize(Vector3I size)
	{
		grid = new Grid3D<DNode>(size, Vector3I.Zero);
		queue = new SimplePriorityQueue<DNode, float>();
		closed = new HashSet<DNode>();
		stack = new Stack<Vector3I>();
		
		for (int x = 0; x < size.X; x++) {
			for (int y = 0; y < size.Y; y++) {
				for (int z = 0; z < size.Z; z++) {
					grid[x, y, z] = new DNode(new Vector3I(x, y, z));
				}
			}
		}
	}
	
	void ResetNodes() {
		var size = grid.Size;
		
		for (int x = 0; x < size.X; x++) {
			for (int y = 0; y < size.Y; y++) {
				for (int z = 0; z < size.Z; z++) {
					var node = grid[x, y, z];
					node.Previous = null;
					node.Cost = float.PositiveInfinity;
					node.Procedure = null;
					node.PreviousSet.Clear();
				}
			}
		}
	}

	public Godot.Collections.Array FindPath(Godot.Collections.Array existingGrid, Vector3I start, Vector3I end) {
		ResetNodes();
		queue.Clear();
		closed.Clear();
		
		this.existingGrid = new Grid3D<CellType>(
			new Vector3I(
				((Godot.Collections.Array)existingGrid).Count(), 
				((Godot.Collections.Array)((Godot.Collections.Array)existingGrid)[0]).Count(), 
				((Godot.Collections.Array)((Godot.Collections.Array)((Godot.Collections.Array)existingGrid)[0])[0]).Count()
			), Vector3I.Zero);
		for (int x = 0; x < existingGrid.Count(); x++)
		{
			for (int y = 0; y < ((Godot.Collections.Array)existingGrid[x]).Count(); y++)
			{
				for (int z = 0; z < ((Godot.Collections.Array)((Godot.Collections.Array)existingGrid[x])[y]).Count(); z++)
				{
					this.existingGrid[x,y,z] = (CellType)(int)(((Godot.Collections.Array)((Godot.Collections.Array)((Godot.Collections.Array)existingGrid)[x])[y])[z]);
				}
			}
		}
		
		queue = new SimplePriorityQueue<DNode, float>();
		closed = new HashSet<DNode>();

		grid[start].Cost = 0.0f;
		grid[start].Procedure = new HallwayProcedure(start, start);
		queue.Enqueue(grid[start], 0.0f);

		while (queue.Count > 0) {
			DNode node = queue.Dequeue();
			closed.Add(node);

			if (node.Position == end) {
				return ReconstructPath(node);
			}
			
			foreach (var offset in HallwayProcedure.GetPossibleOffsets()) {
				TryProcedure(node, end, new HallwayProcedure(node.Position, node.Position + offset));
			}
			foreach (var offset in StairwayProcedure.GetPossibleOffsets()) {
				TryProcedure(node, end, new StairwayProcedure(node.Position, node.Position + offset));
			}
		}

		return null;
	}

	void TryProcedure(DNode node, Vector3I end, PathfindProcedure procedure)
	{
		if (!grid.InBounds(procedure.EndPosition)) return;
		var neighbor = grid[procedure.EndPosition];
		if (closed.Contains(neighbor)) return;

		if (node.PreviousSet.Contains(neighbor.Position)) {
			return;
		}
		foreach (Vector3I position in procedure.GetOccupiedPositions())
		{
			if (node.PreviousSet.Contains(position)) return;
		}

		float cost = procedure.CalculateCost(end, existingGrid);
		if (Math.Abs(-1f - cost) < 0.001f) return;

		float newCost = node.Cost + cost;

		if (newCost < neighbor.Cost) {
			neighbor.Previous = node;
			neighbor.Cost = newCost;
			neighbor.Procedure = procedure;

			if (queue.TryGetPriority(node, out float existingPriority)) {
				queue.UpdatePriority(node, newCost);
			} else {
				queue.Enqueue(neighbor, neighbor.Cost);
			}

			neighbor.PreviousSet.Clear();
			neighbor.PreviousSet.UnionWith(node.PreviousSet);
			neighbor.PreviousSet.Add(node.Position);

			foreach (Vector3I position in procedure.GetOccupiedPositions())
			{
				neighbor.PreviousSet.Add(position);
			}
		}
	}

	Godot.Collections.Array ReconstructPath(DNode node) {
		Godot.Collections.Array result = new Godot.Collections.Array();

		while (node != null) {
			stack.Push(node.Position);
			node = node.Previous;
		}

		while (stack.Count > 0) {
			node = grid[stack.Pop()];
			
			Godot.Collections.Array procedureArray = new Godot.Collections.Array();
			procedureArray.Add(node.Procedure.EnumValue);
			procedureArray.Add(node.Procedure.StartPosition);
			procedureArray.Add(node.Procedure.EndPosition);
			
			Godot.Collections.Array occupiedArray = new Godot.Collections.Array();
			foreach(Vector3I position in node.Procedure.GetOccupiedPositions())
			{
				occupiedArray.Add(position);
			}
			
			procedureArray.Add(occupiedArray);
			procedureArray.Add(node.Procedure.directionXZ);
			
			result.Add(procedureArray);
		}

		return result;
	}
}

public class DNode
{
	public Vector3I Position;
	public DNode Previous;
	public HashSet<Vector3I> PreviousSet { get; private set; }
	public PathfindProcedure Procedure;
	public float Cost;
	
	public DNode(Vector3I position) 
	{
		this.Position = position;
		this.PreviousSet = new HashSet<Vector3I>();
	}
}

public abstract class PathfindProcedure
{
	public float Cost { get; private set; }
	public int EnumValue { get; protected set; }
	public Vector3I StartPosition;
	public Vector3I EndPosition;
	
	public Vector3I directionXZ;
	public int directionY;
	
	public PathfindProcedure(Vector3I startPosition, Vector3I endPosition)
	{
		StartPosition = startPosition;
		EndPosition = endPosition;
		
		directionXZ = endPosition - startPosition;
		directionY = directionXZ.Y;
		directionXZ.Y = 0;
		directionXZ = directionXZ / 3;
	}
	
	public abstract Vector3I[] GetOccupiedPositions();
	
	public virtual float CalculateCost(Vector3I targetPos, Grid3D<CellType> grid) // -1f = non-traversable
	{
		return 0.0f;
	}
}

public class StartProcedure : PathfindProcedure
{
	public StartProcedure(Vector3I startPosition, Vector3I endPosition) : base(startPosition, endPosition)
	{
		EnumValue = -1;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] { };
	}
}

public class HallwayProcedure : PathfindProcedure
{
	public HallwayProcedure(Vector3I startPosition, Vector3I endPosition) : base(startPosition, endPosition)
	{
		EnumValue = 2;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] { };
	}
	
	public override float CalculateCost(Vector3I targetPos, Grid3D<CellType> grid)
	{
		float cost = ((Vector3)EndPosition).DistanceTo((Vector3)targetPos);    //heuristic
		if (grid[EndPosition] == CellType.Stairway) {
			return -1f;
		} else if (grid[EndPosition] == CellType.Room) {
			cost += 5f;
		} else if (grid[EndPosition] == CellType.None) {
			cost += 1f;
		}
		return cost;
	}
	
	public static Vector3I[] GetPossibleOffsets()
	{ 
		return new Vector3I[] { 
			new Vector3I(1, 0, 0), new Vector3I(-1, 0, 0), new Vector3I(0, 0, 1), new Vector3I(0, 0, -1) 
		}; 
	}
}

public class StairwayProcedure : PathfindProcedure
{
	public StairwayProcedure(Vector3I startPosition, Vector3I endPosition) : base(startPosition, endPosition)
	{
		// Assuming VALID
		EnumValue = 3;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		Vector3I[] positions = new Vector3I[4];
		positions[0] = StartPosition + directionXZ;
		positions[1] = StartPosition + (directionXZ * 2);
		positions[2] = StartPosition + directionXZ + (Vector3I.Up * directionY);
		positions[3] = StartPosition + (directionXZ * 2) + (Vector3I.Up * directionY);
		
		return positions;
	}
	
	public override float CalculateCost(Vector3I targetPos, Grid3D<CellType> grid)
	{
		if ((grid[StartPosition] != CellType.None && grid[StartPosition] != CellType.Hallway)
				|| (grid[EndPosition] != CellType.None && grid[EndPosition] != CellType.Hallway)) return -1f;

		foreach (Vector3I position in GetOccupiedPositions())
		{
			if (!grid.InBounds(position)) return -1f;
		}
		
		foreach (Vector3I position in GetOccupiedPositions())
		{
			if (grid[position] != CellType.None) return -1f;
		}

		float cost = 100f + ((Vector3)EndPosition).DistanceTo((Vector3)targetPos);    //base cost + heuristic
		return cost;
	}
	
	public static Vector3I[] GetPossibleOffsets() 
	{ 
		return new Vector3I[] { 
			new Vector3I(3, 1, 0),
			new Vector3I(-3, 1, 0),
			new Vector3I(0, 1, 3),
			new Vector3I(0, 1, -3),
			
			new Vector3I(3, -1, 0),
			new Vector3I(-3, -1, 0),
			new Vector3I(0, -1, 3),
			new Vector3I(0, -1, -3)
		}; 
	}
}
