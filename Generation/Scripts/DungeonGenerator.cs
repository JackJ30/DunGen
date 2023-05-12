using Godot;
using System;
using System.Collections.Generic;
using Graphs;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;

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
	private Delaunay3D _delaunay;
	private HashSet<Prim.Edge> _hallwayEdges;
	private DungeonRenderer _renderer;
	private DungeonRoomGenerator roomGenerator;
	
	private List<Room> _rooms;
	private List<Stairway> _stairways;
	
	public override void _Ready()
	{
		if(!Generate) return;
		
		_grid = new Grid3D<Cell>(Size, Vector3I.Zero);
		_grid.AssignAll((Vector3I pos) => {
			return new Cell(pos);
		});
		GD.Randomize();
		
		_renderer = this.GetNode("DungeonRenderer") as DungeonRenderer;
		_renderer.Initialize(_grid);
		roomGenerator = new DungeonRoomGenerator();
		
		_rooms = new List<Room>();
		_stairways = new List<Stairway>();
		
		PlaceRooms();
		RenderGrid();
	}

	private void PlaceRooms()
	{
		/*
		int roomsSpawned = 0;
		int numTries = 0;
		while (roomsSpawned < RoomCount)
		{
			if (numTries >= 1000) break;
			numTries += 1;
			
			Vector3I roomPosition = new Vector3I(GD.RandRange(0,Size.X - 1), GD.RandRange(0,Size.Y - 1), GD.RandRange(0,Size.Z - 1));
			Vector3I roomSize = new Vector3I(GD.RandRange(RoomMinSize.X,RoomMaxSize.X), GD.RandRange(RoomMinSize.Y,RoomMaxSize.Y), GD.RandRange(RoomMinSize.Z,RoomMaxSize.Z));
			
			Room newRoom = new Room(roomPosition, roomSize);

			bool addRoom = true;
			foreach (Room room in _rooms)
			{
				if (room.Intersects(newRoom))
				{
					addRoom = false;
					break;
				}
			}
			
				if (!addRoom) continue;
				if (!newRoom.InBounds(_grid)) continue;
				
				_rooms.Add(newRoom);
				newRoom.AssignCells(_grid);
				roomsSpawned += 1;
			numTries = 0;
		}*/

		RoomCluster cluster = roomGenerator.GenerateRoomCluster(20);
		cluster.AssignToGrid(_grid);
		
	}
	
	private void Triangulate()
	{
		List<Vertex> vertices = new List<Vertex>();

		foreach (var room in _rooms) {
			vertices.Add(new Vertex<Room>(room.GetAveragePosition(), room));
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
			if (GD.Randf() < ExtraHallwayChance) {
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

			var startRoomFloor = startRoom.GetFloorPositions(_grid);
			var endRoomFloor = endRoom.GetFloorPositions(_grid);
			var startPos = startRoomFloor[GD.RandRange(0, startRoomFloor.Length - 1)];
			var endPos = endRoomFloor[GD.RandRange(0, endRoomFloor.Length - 1)];
			
			List<PathfindingLevelSegment> path = pathfinder.FindPath(_grid, startPos, endPos);
			
			if (path == null) continue;
			
			for (int i = 0; i < path.Count(); i++)
			{
				path[i].InterpretPathfindingResult(_grid, path, i);
				if (path[i] is Stairway) _stairways.Add((Stairway)path[i]);
			}
		}
	}

	void RenderGrid()
	{
		foreach (DungeonLevelSegment segment in GetAllSegments())
		{
			_renderer.RenderSegment(segment);
		}
	}

	private List<DungeonLevelSegment> GetAllSegments()
	{
		List<DungeonLevelSegment> segments = new List<DungeonLevelSegment>();
		
		for (int x = 0; x < Size.X; x++) {
			for (int y = 0; y < Size.Y; y++) {
				for (int z = 0; z < Size.Z; z++) {
					foreach (DungeonLevelSegment segment in _grid[x,y,z].Segments)
					{
						if (!segments.Contains(segment)) segments.Add(segment);
					}
				}
			}
		}

		return segments;
	}
}

[DataContract]
public class SerializedDungeon
{
	[DataMember]
	public Grid3D<Cell> Grid;
	[DataMember]
	public List<DungeonLevelSegment> Segments;
	
	public SerializedDungeon(Grid3D<Cell> grid)
	{
		Grid = grid;
		Segments = new List<DungeonLevelSegment>();
		
		for (int x = 0; x < Grid.Size.X; x++)
		{
			for (int y = 0; y < Grid.Size.Y; y++)
			{
				for (int z = 0; z < Grid.Size.Z; z++)
				{
					if (!Grid[x,y,z].IsEmpty()) 
					{
						foreach(DungeonLevelSegment segment in Grid[x,y,z].Segments)
						{
							if(!Segments.Contains(segment)) Segments.Add(segment);
						}
					}
				}
			}
		}
	}
	
	private DataContractSerializer GetSerializer()
	{
		var settings = new DataContractSerializerSettings();
		settings.MaxItemsInObjectGraph = 0x7FFF0;
		settings.IgnoreExtensionDataObject = false;
		settings.PreserveObjectReferences = true;
		
		List<Type> knownTypeList = new List<Type>();
		knownTypeList.Add(typeof(DungeonLevelSegment));
		knownTypeList.Add(typeof(PathfindingLevelSegment));
		knownTypeList.Add(typeof(Room));
		knownTypeList.Add(typeof(Hallway));
		knownTypeList.Add(typeof(Stairway));
		
		settings.KnownTypes = knownTypeList;
		
		var serializer = new DataContractSerializer(GetType(), settings);
		
		return serializer;
	}
	
	public String Serialize()
	{
		var settings = new DataContractSerializerSettings();
		var serializer = GetSerializer();
		FileStream writer = new FileStream(@System.IO.Path.GetTempPath() + "dungeon.txt", FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write, 128 * 1024);
		serializer.WriteObject(writer, this);
		writer.Close();
		
		return File.ReadAllText(@System.IO.Path.GetTempPath() + "dungeon.txt");
		
	}
	
	public Grid3D<Cell> DeSerialize(String data)
	{
		MemoryStream stream = new MemoryStream();
		StreamWriter writer = new StreamWriter(stream);
		writer.Write(data);
		writer.Flush();
		stream.Position = 0;
		
		XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
		var serializer = GetSerializer();
		
		SerializedDungeon deserializedDungeon = serializer.ReadObject(reader,true) as SerializedDungeon;
		
		stream.Close();
		writer.Close();
		
		return deserializedDungeon.Grid;
	}
}

[DataContract]
public abstract class DungeonLevelSegment
{	
	[DataMember]
	public String id { get; private set; }
	
	public Vector3I Origin { get; protected set; }
	public Vector3I Direction { get; protected set; }
	
	public DungeonLevelSegment(Vector3I origin, Vector3I direction)
	{
		id = Guid.NewGuid().ToString("N");

		Origin = origin;
		Direction = direction;
	}
	
	public virtual void AssignCells(Grid3D<Cell> grid, bool local = false)
	{
		foreach (Vector3I position in GetOccupiedPositions().Select(position => RelativePosition(position,local)))
		{
			grid[position].Segments.Add(this);
		}
	}
	
	public virtual void UnAssignCells(Grid3D<Cell> grid, bool local = false)
	{
		foreach (Vector3I position in GetOccupiedPositions().Select(position => RelativePosition(position,local)))
		{
			if(grid[position].Segments.Contains(this)) grid[position].Segments.Remove(this);
		}
	}
	
	public virtual Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] {};
	}

	public virtual void Translate(Vector3I amount)
	{
		Origin += amount;
	}

	public virtual void RotateXZ(Vector3I amount) // Change direction
	{
		Direction = Util.RotationNumberToDirectionXZ(Util.DirectionToRotationNumberXZ(Direction) +
		                                              Util.DirectionToRotationNumberXZ(Direction));
	}
	
	public virtual bool NeighborEvaluator(Cell cellFrom, Cell cellTo, Vector3I delta)
	{
		return false;
	}
	
	protected Vector3I LocalToGlobal(Vector3I localPosition)
	{
		Vector3 directionConverted = (Vector3)Direction;
		return Origin + (Direction * localPosition.Z) + (Vector3I.Up * localPosition.Y) + ((Vector3I)directionConverted.Rotated(Vector3.Up, (float)-Math.PI/2) * localPosition.X);
	}

	protected Vector3I RelativePosition(Vector3I position, bool local) // This is just a function to shorten lines of in functions that work both globally and locally
	{
		return local ? position : LocalToGlobal(position);
	}
}

[DataContract]
public abstract class PathfindingLevelSegment : DungeonLevelSegment
{
	public Vector3I Start { get; protected set; }
	public Vector3I End { get; protected set; }
	
	public PathfindingLevelSegment(Vector3I start, Vector3I end, Vector3I origin) : base(origin,end - start)
	{
		Start = start;
		End = end;
	}
	
	public virtual float CalculateCost(Vector3I targetPos, Vector3I previousPos, Grid3D<Cell> grid) // -1f = non-traversable
	{
		return 0.0f;
	}
	
	public virtual Vector3I[] GetAdditionalRequiredEmptyPositions()
	{
		return new Vector3I[] {};
	}
	
	public virtual void InterpretPathfindingResult(Grid3D<Cell> grid, List<PathfindingLevelSegment> results, int index)
	{
		AssignCells(grid);
	}
	
	public virtual bool SatisfiesNextCondition(PathfindingLevelSegment segment)
	{
		return true;
	}
}

[DataContract]
public class Door : DungeonLevelSegment
{
	public Door(LinkedVector3I position) : base(position.A, position.A - position.B)
	{
	}

	public override void AssignCells(Grid3D<Cell> grid, bool local = false)
	{
		base.AssignCells(grid);
		
		Cell.Connect(grid[RelativePosition(Vector3I.Zero, local)],grid[RelativePosition(new Vector3I(0,0,1), local)]);
	}

	public override void UnAssignCells(Grid3D<Cell> grid, bool local = false)
	{
		base.UnAssignCells(grid);
		
		Cell.Disconnect(grid[RelativePosition(Vector3I.Zero, local)],grid[RelativePosition(new Vector3I(0,0,1), local)]);
	}

	public override Vector3I[] GetOccupiedPositions()
	{
		return new Vector3I[] { Vector3I.Zero, new Vector3I(0,0,1) };
	}
}


// HALLWAY AND STAIRWAY NEED FIXING
[DataContract]
public class Hallway : PathfindingLevelSegment
{
	public Hallway(Vector3I start, Vector3I end) : base(start, end, end)
	{
	}
	
	public override float CalculateCost(Vector3I targetPos, Vector3I previousPos, Grid3D<Cell> grid)
	{
		float cost = ((Vector3)End).DistanceTo((Vector3)targetPos);    //heuristic
		if (grid[End].HasSegment<Stairway>()) {
			return -1f;
		} else if (grid[End].HasSegment<Room>()) {
			cost += 5f;
		} else if (grid[End].IsEmpty()) {
			cost += 1f;
		}
		return cost;
	}
	
	public override void InterpretPathfindingResult(Grid3D<Cell> grid, List<PathfindingLevelSegment> results, int index)
	{
		if (grid[End].HasSegment<Room>()) return;
		
		base.InterpretPathfindingResult(grid, results, index);
		
		if(index != 0)
		{
			foreach (Vector3I position in results[index - 1].GetOccupiedPositions())
			{
				if(grid[position].HasSegment<Room>())
				{
					Door createdDoor = new Door(new LinkedVector3I(End, position));
					createdDoor.AssignCells(grid);
				}
			}
		}
		
		if(index < results.Count() - 1)
		{
			foreach (Vector3I position in results[index + 1].GetOccupiedPositions())
			{
				if(grid[position].HasSegment<Room>())
				{
					Door createdDoor = new Door(new LinkedVector3I(End, position));
					createdDoor.AssignCells(grid);
				}
			}
		}
	}
	
	public override bool NeighborEvaluator(Cell cellFrom, Cell cellTo, Vector3I delta)
	{
		if (cellFrom.HasConnection(cellTo)) return true;
		if (cellTo.IsEmpty()) return false;
		if (delta.Y != 0) return false;
		
		return cellTo.HasSegment<Hallway>();
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

[DataContract]
public class Stairway : PathfindingLevelSegment
{
	public Stairway(Vector3I start, Vector3I end) : base(start, end, start)
	{
		Vector3I direction = (end - start) / 3;
		direction.Y = 0;
		
		Start = start + direction;
		End = end - direction;
	}
	
	public override float CalculateCost(Vector3I targetPos, Vector3I previousPos, Grid3D<Cell> grid)
	{
		if ((!grid[Start].IsEmpty() && !grid[Start].HasSegment<Hallway>())
				|| (!grid[End].IsEmpty() && !grid[End].HasSegment<Hallway>())) return -1f;

		List<Vector3I> positions = GetOccupiedPositions().Concat(GetAdditionalRequiredEmptyPositions()).ToList();
		positions.Add(previousPos);
		foreach (Vector3I position in positions)
		{
			if (!grid.InBounds(position)) return -1f;
		}
		
		foreach (Vector3I position in positions)
		{
			if (!grid[position].IsEmpty()) 
			{
				return -1f;
			}
		}

		float cost = 100f + ((Vector3)End).DistanceTo((Vector3)targetPos);    //base cost + heuristic
		return cost;
	}
	
	public override void InterpretPathfindingResult(Grid3D<Cell> grid, List<PathfindingLevelSegment> results, int index)
	{
		base.InterpretPathfindingResult(grid, results, index);
		
		Cell.Connect(grid[End],grid[End + (Direction * new Vector3I(1, 0, 1))]);
		Cell.Connect(grid[Start],grid[Start + (-Direction * new Vector3I(1, 0, 1))]);
	}

	public override void UnAssignCells(Grid3D<Cell> grid, bool local = false)
	{
		base.UnAssignCells(grid);
		
		Cell.Disconnect(grid[End],grid[End + (Direction * new Vector3I(1, 0, 1))]);
		Cell.Disconnect(grid[Start],grid[Start + (-Direction * new Vector3I(1, 0, 1))]);
	}

	public override bool NeighborEvaluator(Cell cellFrom, Cell cellTo, Vector3I delta)
	{
		if (cellFrom.HasConnection(cellTo)) return true;
		if (cellTo.IsEmpty()) return false;
		
		foreach (PathfindingLevelSegment segmentTo in cellTo.GetSegments<Stairway>())
		{
			foreach (PathfindingLevelSegment segmentFrom in cellFrom.GetSegments<Stairway>())
			{
				if ((segmentTo.Start == segmentFrom.Start + delta && segmentTo.End == segmentFrom.End + delta) || 
					(segmentTo.Start == segmentFrom.End + delta && segmentTo.End == segmentFrom.Start + delta))
				{
					if (segmentTo.Direction == segmentFrom.Direction) return true;
					if (segmentTo.Direction == -segmentFrom.Direction) return true;
				}
			}
		}
		
		if (!cellFrom.Segments.Select(x => x.id).Intersect(cellTo.Segments.Select(x => x.id)).Any()) return false;
		
		return true;
	}
	
	public override bool SatisfiesNextCondition(PathfindingLevelSegment segment)
	{
		return (segment is Hallway) && segment.Direction == Direction * new Vector3I(1, 0, 1);
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
		Vector3I[] positions = new Vector3I[2];
		positions[0] = End + (Direction * new Vector3I(1, 0, 1));
		//positions[1] = Start - (Direction * new Vector3I(1, 0, 1));
		
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

[DataContract]
public class Cell
{
	[DataMember]
	public List<DungeonLevelSegment> Segments { get; private set; }
	[DataMember]
	public List<LinkedVector3I> Connections { get; private set; }
	[DataMember]
	public Vector3I Position { get; private set; }
	
	public Cell(Vector3I position)
	{
		Position = position;
		
		Segments = new List<DungeonLevelSegment>();
		Connections = new List<LinkedVector3I>();
	}
	
	public Cell(Vector3I position, DungeonLevelSegment segment)
	{
		Position = position;
		
		Segments = new List<DungeonLevelSegment>() {segment};
		Connections = new List<LinkedVector3I>();
	}

	#region Segment Helpers

	// Type
	public IEnumerable<DungeonLevelSegment> GetSegments<T>() where T : DungeonLevelSegment
	{
		return Segments.OfType<T>();
	}
	public bool HasSegment<T>() where T : DungeonLevelSegment
	{
		return GetSegments<T>().Any();
	}

	// Condition
	public IEnumerable<DungeonLevelSegment> GetSegments(Func<DungeonLevelSegment, Boolean> condition)
	{
		return Segments.Where(condition);
	}
	public bool HasSegment(Func<DungeonLevelSegment, Boolean> condition)
	{
		return GetSegments(condition).Any();
	}

	// Type and Condition
	public IEnumerable<DungeonLevelSegment> GetSegments<T>(Func<DungeonLevelSegment, Boolean> condition) where T : DungeonLevelSegment
	{
		return Segments.Where(condition).OfType<T>();
	}
	public bool HasSegment<T>(Func<DungeonLevelSegment, Boolean> condition) where T : DungeonLevelSegment
	{
		return GetSegments<T>(condition).Any();
	}

	#endregion

	public bool IsEmpty()
	{
		return Segments.Count == 0;
	}
	
	public bool HasConnection(Cell other)
	{
		foreach (LinkedVector3I connection in Connections)
		{
			if (connection.Contains(other.Position)) return true;
		}
		return false;
	}

	public static void Connect(Cell a, Cell b)
	{
		LinkedVector3I connection = new LinkedVector3I(a.Position, b.Position);
		b.Connections.Add(connection);
		a.Connections.Add(connection);
	}

	public static void Disconnect(Cell a, Cell b)
	{
		IEnumerable<LinkedVector3I> connectionsToRemove = a.Connections.Concat(b.Connections)
			.Where(connection => connection.Contains(a.Position) && connection.Contains(b.Position));

		foreach (LinkedVector3I connection in connectionsToRemove)
		{
			a.Connections.Remove(connection);
			b.Connections.Remove(connection);
		}
	}
}
