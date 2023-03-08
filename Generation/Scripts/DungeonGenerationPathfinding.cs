using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerationPathfinding
{
	Grid3D<DNode> grid;
	Grid3D<Cell> existingGrid;
	SimplePriorityQueue<DNode, float> queue;
	HashSet<DNode> closed;
	Stack<Vector3I> stack;
	
	public DungeonGenerationPathfinding(Vector3I size)
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
					node.Segment = null;
					node.PreviousSet.Clear();
				}
			}
		}
	}

	public List<PathfindingLevelSegment> FindPath(Grid3D<Cell> existingGrid, Vector3I start, Vector3I end) {
		ResetNodes();
		queue.Clear();
		closed.Clear();
		
		this.existingGrid = existingGrid;
		
		queue = new SimplePriorityQueue<DNode, float>();
		closed = new HashSet<DNode>();

		grid[start].Cost = 0.0f;
		grid[start].Segment = new Hallway(start, start);
		queue.Enqueue(grid[start], 0.0f);

		while (queue.Count > 0) {
			DNode node = queue.Dequeue();
			closed.Add(node);

			if (node.Position == end) {
				return ReconstructPath(node);
			}
			
			foreach (var offset in Hallway.GetPossibleOffsets()) {
				TrySegment(node, end, new Hallway(node.Position, node.Position + offset));
			}
			foreach (var offset in Stairway.GetPossibleOffsets()) {
				TrySegment(node, end, new Stairway(node.Position, node.Position + offset));
			}
		}

		return null;
	}

	void TrySegment(DNode node, Vector3I end, PathfindingLevelSegment segment)
	{
		if (!grid.InBounds(segment.End)) return;
		var neighbor = grid[segment.End];
		if (closed.Contains(neighbor)) return;
		if (node.PreviousSet.Contains(neighbor.Position)) {
			return;
		}
		if (!node.Segment.SatisfiesNextCondition(segment)) return;
		foreach (Vector3I position in segment.GetOccupiedPositions().Concat(segment.GetAdditionalRequiredEmptyPositions()))
		{
			if (node.PreviousSet.Contains(position)) return;
		}

		float cost = segment.CalculateCost(end, existingGrid);
		if (Math.Abs(-1f - cost) < 0.001f) return;

		float newCost = node.Cost + cost;

		if (newCost < neighbor.Cost) {
			neighbor.Previous = node;
			neighbor.Cost = newCost;
			neighbor.Segment = segment;

			if (queue.TryGetPriority(node, out float existingPriority)) {
				queue.UpdatePriority(node, newCost);
			} else {
				queue.Enqueue(neighbor, neighbor.Cost);
			}

			neighbor.PreviousSet.Clear();
			neighbor.PreviousSet.UnionWith(node.PreviousSet);
			neighbor.PreviousSet.Add(node.Position);

			foreach (Vector3I position in segment.GetOccupiedPositions())
			{
				neighbor.PreviousSet.Add(position);
			}
		}
	}

	List<PathfindingLevelSegment> ReconstructPath(DNode node) {
		List<PathfindingLevelSegment> result = new List<PathfindingLevelSegment>();

		
		while (node != null) {
			stack.Push(node.Position);
			node = node.Previous;
		}

		while (stack.Count > 0) {
			node = grid[stack.Pop()];
			
			result.Add(node.Segment);
		}

		return result;
	}
}

public class DNode
{
	public Vector3I Position;
	public DNode Previous;
	public HashSet<Vector3I> PreviousSet { get; private set; }
	public PathfindingLevelSegment Segment;
	public float Cost;
	
	public DNode(Vector3I position) 
	{
		this.Position = position;
		this.PreviousSet = new HashSet<Vector3I>();
	}
}
