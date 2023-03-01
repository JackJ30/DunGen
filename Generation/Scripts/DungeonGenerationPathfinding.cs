using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerationPathfinding : Node
{
	Grid3D<DNode> grid;
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
					node.PreviousSet.Clear();
				}
			}
		}
	}

	public List<Vector3I> FindPath(Vector3I start, Vector3I end) {
		ResetNodes();
		queue.Clear();
		closed.Clear();

		queue = new SimplePriorityQueue<DNode, float>();
		closed = new HashSet<DNode>();

		grid[start].Cost = 0.0f;
		grid[start].Procedure = new StartProcedure(start, start);
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

		float cost = procedure.CalculateCost(end);
		if (Math.Abs(-1f - cost) < 0.001f) return;

		float newCost = node.Cost + cost;

		if (newCost < neighbor.Cost) {
			neighbor.Previous = node;
			neighbor.Cost = newCost;

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

	List<Vector3I> ReconstructPath(DNode node) { // TODO - UPDATE THIS
		List<Vector3I> result = new List<Vector3I>();

		while (node != null) {
			stack.Push(node.Position);
			node = node.Previous;
		}

		while (stack.Count > 0) {
			result.Add(stack.Pop());
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
	public Vector3I StartPosition;
	public Vector3I EndPosition;
	
	public PathfindProcedure(Vector3I startPosition, Vector3I endPosition)
	{
		StartPosition = startPosition;
		EndPosition = endPosition;
	}
	
	public abstract Vector3I[] GetOccupiedPositions();
	
	public virtual float CalculateCost(Vector3I targetPos) // -1f = non-traversable
	{
		return 0.0f;
	}
}

public class StartProcedure : PathfindProcedure
{
	public StartProcedure(Vector3I startPosition, Vector3I endPosition) : base(startPosition, endPosition)
	{
		
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
		
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] { };
	}
	
	public override float CalculateCost(Vector3I targetPos)
	{
		float cost = ((Vector3)EndPosition).DistanceTo((Vector3)targetPos);    //heuristic

		if (grid[EndPosition] == CellType.Stairs) {
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
	Vector3I direction;
	
	public StairwayProcedure(Vector3I startPosition, Vector3I endPosition) : base(startPosition, endPosition)
	{
		// Assuming VALID
		
		direction = endPosition - startPosition;
		direction.Y = 0;
		direction = direction / 3;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		Vector3I[] positions = new Vector3I[4];
		positions[0] = EndPosition + direction;
		positions[1] = EndPosition + (direction * 2);
		positions[2] = EndPosition + direction + Vector3I.Up;
		positions[3] = EndPosition + (direction * 2) + Vector3I.Up;
		
		return positions;
	}
	
	public override float CalculateCost(Vector3I targetPos)
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
