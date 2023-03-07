using Godot;
using System;
using System.Collections.Generic;

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
	
	private List<Room> _rooms;
	
	public override void _Ready()
	{
		if(Generate) return;
		
		_grid = new Grid3D<Cell>(Size, Vector3I.Zero);
		_grid.AssignAll((Vector3I pos) => {
			return new Cell(pos);
		});
		_random = new RandomNumberGenerator();
		_random.Randomize();
		
		_rooms = new List<Room>();
		
		PlaceRooms();
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
}

public abstract class DungeonLevelSegment
{
	public Vector3I Start { get; private set; }
	public Vector3I End { get; private set; }
	List<Vector3I> OccupiedPositions;
	
	public DungeonLevelSegment()
	{
		OccupiedPositions = new List<Vector3I>();
	}
	
	public virtual void AssignCells(Grid3D<Cell> grid)
	{
		foreach (Vector3I position in OccupiedPositions)
		{
			grid[position].Segments.Add(this);
		}
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

public class Hallway : DungeonLevelSegment
{
	
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
