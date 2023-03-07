using Godot;
using System;
using System.Collections.Generic;
using Graphs;
using System.Linq;

public partial class DungeonGenerator : Node
{
	[Export]
	bool Generate = true;
	[ExportGroup("Grid Settings")]
	[Export]
	Vector3I Size = new Vector3I(40,6,40);
	[ExportGroup("Room Settings")]
	[Export]
	int RoomCount = 12;
	[Export]
	Vector3I RoomMaxSize = new Vector3I(6,3,6);
	[Export]
	Vector3I RoomMinSize = new Vector3I(2,1,2);
	[ExportGroup("Hallway Settings")]
	[Export(PropertyHint.Range, "0,1,")]
	float ExtraHallwayChance = 0.05f;
	
	private Grid3D<Cell> _grid;
	private RandomNumberGenerator _random;
	private Delaunay3D _delaunay;
	private HashSet<Prim.Edge> _hallwayEdges;
	
	private List<Room> _rooms;
	
	public override void _Ready()
	{
		if(!Generate) return;
		
		_grid = new Grid3D<Cell>(Size, Vector3I.Zero);
		_grid.AssignAll((Vector3I pos) => {
			return new Cell(pos);
		});
		_random = new RandomNumberGenerator();
		_random.Randomize();
		
		_rooms = new List<Room>();
		
		PlaceRooms();
		Triangulate();
		CreateHallwayConnections();
		PathfindHallways();
	}

	private void PlaceRooms()
	{
		int roomsSpawned = 0;
		int numTries = 0;
		while (roomsSpawned < RoomCount)
		{
			if (numTries >= 1000) break;
			numTries += 1;
			
			Vector3I roomPosition = new Vector3I(_random.RandiRange(0,Size.X - 1), _random.RandiRange(0,Size.Y - 1), _random.RandiRange(0,Size.Z - 1));
			Vector3I roomSize = new Vector3I(_random.RandiRange(RoomMinSize.X,RoomMaxSize.X), _random.RandiRange(RoomMinSize.Y,RoomMaxSize.Y), _random.RandiRange(RoomMinSize.Z,RoomMaxSize.Z));
			
			Room newRoom = new Room(roomPosition, roomSize);
			Room bufferRoom = new Room(roomPosition + new Vector3I(-1,-1,-1), roomSize + new Vector3I(2,1,2));
			
			bool addRoom = true;
			foreach (Room room in _rooms)
			{
				if (room.Intersects(bufferRoom))
				{
					addRoom = false;
					break;
				}
			}
			
			if (!addRoom) continue;
			if (!_grid.Bounds.Encloses(bufferRoom.Bounds)) continue;
			
			_rooms.Add(newRoom);
			newRoom.AssignCells(_grid);
			roomsSpawned += 1;
			numTries = 0;
		}
	}
	
	private void Triangulate()
	{
		List<Vertex> vertices = new List<Vertex>();

		foreach (var room in _rooms) {
			vertices.Add(new Vertex<Room>((Vector3)room.Bounds.Position + ((Vector3)room.Bounds.Size) / 2, room));
		}

		_delaunay = Delaunay3D.Triangulate(vertices);
	}
	
	void CreateHallwayConnections()
	{
		List<Prim.Edge> edges = new List<Prim.Edge>();

		foreach (var edge in _delaunay.Edges) {
			edges.Add(new Prim.Edge(edge.U, edge.V));
		}

		List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

		_hallwayEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
		var remainingEdges = new HashSet<Prim.Edge>(edges);
		remainingEdges.ExceptWith(_hallwayEdges);

		foreach (var edge in remainingEdges) {
			if (_random.Randf() < ExtraHallwayChance) {
				_hallwayEdges.Add(edge);
			}
		}
	}
	
	void PathfindHallways()
	{
		foreach (Edge edge in _hallwayEdges)
		{
			DungeonGenerationPathfinding pathfinder = new DungeonGenerationPathfinding(Size);
		
			var startRoom = (edge.U as Vertex<Room>).Item;
			var endRoom = (edge.V as Vertex<Room>).Item;

			var startPosf = startRoom.Bounds.GetCenter();
			var endPosf = endRoom.Bounds.GetCenter();
			var startPos = new Vector3I((int)startPosf.X, (int)startPosf.Y, (int)startPosf.Z);
			var endPos = new Vector3I((int)endPosf.X, (int)endPosf.Y, (int)endPosf.Z);
			
			List<PathfindingLevelSegment> path = pathfinder.FindPath(_grid, startPos, endPos);
			GD.Print(path);
		}
	}
}

public abstract class DungeonLevelSegment
{	
	public DungeonLevelSegment()
	{
		
	}
	
	public virtual void AssignCells(Grid3D<Cell> grid)
	{
		foreach (Vector3I position in GetOccupiedPositions())
		{
			grid[position].Segments.Add(this);
		}
	}
	
	public virtual Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] {};
	}
}

public abstract class PathfindingLevelSegment : DungeonLevelSegment
{
	public Vector3I Start { get; protected set; }
	public Vector3I End { get; protected set; }
	public Vector3I Direction { get; protected set; }
	
	public PathfindingLevelSegment(Vector3I start, Vector3I end)
	{
		Start = start;
		End = end;
		
		Direction = End - Start;
	}
	
	public virtual float CalculateCost(Vector3I targetPos, Grid3D<Cell> grid) // -1f = non-traversable
	{
		return 0.0f;
	}
	
	public virtual Vector3I[] GetAdditionalRequiredEmptyPositions()
	{
		return new Vector3I[] {};
	}
}

public class Room : DungeonLevelSegment
{
	public Aabb Bounds { get; private set; }
	
	public Room(Vector3I position, Vector3I size) : base()
	{
		Bounds = new Aabb(position,size);
	}
	
	public override void AssignCells(Grid3D<Cell> grid)
	{
		grid.AssignBounds(Bounds, (Vector3I pos) => {
			return new Cell(pos);
		});
	}
	
	public bool Intersects(Room other)
	{
		return Bounds.Intersects(other.Bounds);
	}
}

public class Hallway : PathfindingLevelSegment
{
	public Hallway(Vector3I start, Vector3I end) : base(start, end)
	{
	}
	
	public override float CalculateCost(Vector3I targetPos, Grid3D<Cell> grid)
	{
		float cost = ((Vector3)End).DistanceTo((Vector3)targetPos);    //heuristic
		if (grid[End].Segments.Any(i => i.GetType() == typeof(Stairway))) {
			return -1f;
		} else if (grid[End].Segments.Any(i => i.GetType() == typeof(Room))) {
			cost += 5f;
		} else if (grid[End].IsEmpty()) {
			cost += 1f;
		}
		return cost;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] { End };
	}
	
	public static Vector3I[] GetPossibleOffsets()
	{ 
		return new Vector3I[] { 
			new Vector3I(1, 0, 0), new Vector3I(-1, 0, 0), new Vector3I(0, 0, 1), new Vector3I(0, 0, -1) 
		}; 
	}
}

public class Stairway : PathfindingLevelSegment
{
	public Stairway(Vector3I start, Vector3I end) : base(start, end)
	{
		Vector3I direction = (end - start) / 3;
		direction.Y = 0;
		
		Start = start + direction;
		End = end - direction;
		
		Direction = End - Start;
	}
	
	public override float CalculateCost(Vector3I targetPos, Grid3D<Cell> grid)
	{
		if ((!grid[Start].IsEmpty() && !grid[Start].Segments.Any(i => i.GetType() == typeof(Hallway)))
				|| (!grid[End].IsEmpty() && !grid[End].Segments.Any(i => i.GetType() == typeof(Hallway)))) return -1f;

		Vector3I[] positions = GetOccupiedPositions().Concat(GetAdditionalRequiredEmptyPositions()).ToArray();
		foreach (Vector3I position in positions)
		{
			if (!grid.InBounds(position)) return -1f;
		}
		
		foreach (Vector3I position in positions)
		{
			if (grid[position].Segments.Any(i => i.GetType() == typeof(Hallway))) return -1f;
		}

		float cost = 100f + ((Vector3)End).DistanceTo((Vector3)targetPos);    //base cost + heuristic
		return cost;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		Vector3I[] positions = new Vector3I[4];
		positions[0] = Start;
		positions[1] = Start + (Direction * new Vector3I(1, 0, 1));
		positions[2] = Start + (Direction * new Vector3I(0, 1, 0));
		positions[3] = End;
		
		return positions;
	}
	
	public override Vector3I[] GetAdditionalRequiredEmptyPositions()
	{
		Vector3I[] positions = new Vector3I[1];
		positions[0] = End + (Direction * new Vector3I(1, 0, 1));
		
		return positions;
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

public class Cell
{
	public List<DungeonLevelSegment> Segments { get; private set; }
	public List<Vector3I> Connections { get; private set; }
	public Vector3I Position { get; private set; }
	
	public Cell(Vector3I position)
	{
		Position = position;
		
		Segments = new List<DungeonLevelSegment>();
		Connections = new List<Vector3I>();
	}
	
	public Cell(Vector3I position, DungeonLevelSegment segment)
	{
		Position = position;
		
		Segments = new List<DungeonLevelSegment>() {segment};
		Connections = new List<Vector3I>();
	}
	
	public bool IsEmpty()
	{
		return Segments.Count == 0;
	}
}
