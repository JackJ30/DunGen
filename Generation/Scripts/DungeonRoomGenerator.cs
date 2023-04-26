using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DungeonRoomGenerator
{
	public Room GetRoomFromDistribution(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		float random = GD.Randf();
		
		if (random < 0.05f) return new LargeRoom(pointFrom,direction,context);
		if (random < 0.25f) return new LongRoom(pointFrom,direction,context);
		if (random < 0.50f) return new TShapedRoom(pointFrom,direction,context);
		else return new MediumRoom(pointFrom,direction,context);
	}
	
	public RoomCluster GenerateRoomCluster(int numRooms)
	{
		RoomCluster cluster = new RoomCluster();
		cluster.AddRoom(GetRoomFromDistribution(Vector3I.Zero, Vector3I.Back)); // No context needed, as this is the first room
		
		for (int i = 0; i < numRooms - 1; i++)
		{
			SimplePriorityQueue<RoomPlacementNormal, float> roomPlacementNormalsQueue = new SimplePriorityQueue<RoomPlacementNormal, float>();
			List<RoomFace> roomFaces = RoomFace.GetFaces(cluster.GetCompositeShape()).Where(face => face.Direction != Vector3I.Up || face.Direction != Vector3I.Up).ToList();
			
			int maxHeightDifference = roomFaces.Max(face => face.MaxHeight - face.MinHeight);
			int desiredFloor = 0;
			for (int j = 0; j < maxHeightDifference; j++)
			{
				if (GD.Randf() < 0.15f) desiredFloor++; // ROOM FLOOR PRIORITY INCREASE CHANCE
				else break;
			}
			
			roomFaces.ForEach( face => { face.ExposedNormals.ForEach( normal => {
				RoomPlacementNormal roomPlacementNormal = new RoomPlacementNormal(face,normal);
				roomPlacementNormalsQueue.Enqueue(roomPlacementNormal, NormalPriorityFunction(roomPlacementNormal, desiredFloor)); 
				}); 
			});
			
			Room generatedRoom = null;
			
			while (roomPlacementNormalsQueue.Count > 0)
			{
				RoomFace.ExposedNormal normalFrom = roomPlacementNormalsQueue.Dequeue().HeldNormal;
				
				if (normalFrom.Direction == Vector3I.Up || normalFrom.Direction == Vector3I.Down) continue;
				generatedRoom = GetRoomFromDistribution(normalFrom.Position + normalFrom.Direction, normalFrom.Direction,cluster.GetCompositeShape());
				if (cluster.AddRoom(generatedRoom))
				{
					cluster.AddDoor(new LinkedVector3I(normalFrom.Position,normalFrom.Position + normalFrom.Direction));
					break;
				}
			}
		}

		cluster.AddExtraDoors(0.2f); // EXTRA DOOR PERCENTAGE
		
		cluster.Abs();
		return cluster;
	}
	
	float NormalPriorityFunction(RoomPlacementNormal roomPlacementNormal, int desiredFloor)
	{
		float priority = GD.Randf();
		if (roomPlacementNormal.HeldNormal.Position.Y == roomPlacementNormal.HeldFace.MinHeight + desiredFloor)
		{
			priority -= 0.5f; // ROOM FLOOR PRIORITY WEIGHTING
		}
		return priority;
	}
	
	public struct RoomPlacementNormal
	{
		public RoomFace HeldFace {get;private set;}
		public RoomFace.ExposedNormal HeldNormal {get;private set;}
		
		public RoomPlacementNormal(RoomFace heldFace, RoomFace.ExposedNormal heldNormal)
		{
			HeldFace = heldFace;
			HeldNormal = heldNormal;
		}
	}
}

public class RoomCluster
{
	public List<Room> Rooms { get; private set; }
	public List<LinkedVector3I> DoorPositions { get; private set; }
	
	public RoomCluster()
	{
		Rooms = new List<Room>();
		DoorPositions = new List<LinkedVector3I>();
	}
	
	public bool AddRoom(Room addend)
	{
		if (GetCompositeShape().Intersect(addend.GlobalShape).Any()) return false;
		
		Rooms.Add(addend);
		return true;
	}

	public void AddDoor(LinkedVector3I door)
	{
		DoorPositions.Add(door);
	}

	public void AssignToGrid(Grid3D<Cell> grid)
	{
		foreach (Room room in Rooms)
		{
			room.AssignCells(grid);
		}

		foreach (LinkedVector3I doorPosition in DoorPositions)
		{
			Door generatedDoor = new Door(doorPosition);
			generatedDoor.AssignCells(grid);
		}
	}
	
	public Vector3I Abs() 
	{
		Vector3I smallest = Util.GetSmallestIndividual(Rooms.Select(room => Util.GetSmallestIndividual(room.GlobalShape.ToArray())).ToArray());
		Translate(-smallest);
		return -smallest;
	}
	
	public void Translate(Vector3I amount)
	{
		foreach (Room room in Rooms)
		{
			room.Translate(amount);
		}

		foreach (LinkedVector3I door in DoorPositions)
		{
			door.A += amount;
			door.B += amount;
		}
	}

	public void AddExtraDoors(float extraDoorPercentage)
	{
		if (Rooms.Count() <= 2) return; // Small optimization, no need to add extra connections if there are two or less rooms
		
		int numExtraDoors = (int)Math.Round(Rooms.Count() * extraDoorPercentage);
		List<Vector3I> compositeShape = GetCompositeShape();
		List<Vector3I> doorPositions = new List<Vector3I>();
		DoorPositions.ForEach(door =>
		{
			doorPositions.Add(door.A);
			doorPositions.Add(door.B);
		});

		int doorIndex = 0;
		foreach (Room room in Rooms.OrderBy(x => GD.Randf()))
		{
			List<Vector3I> roomGlobalShape = room.GlobalShape;
			
			int roomMinHeight = roomGlobalShape.Min(position => position.Y);
			int roomMaxHeight = roomGlobalShape.Max(position => position.Y);
			int roomHeight =  roomMaxHeight - roomMinHeight;
			int targetHeight = 0;
		
			for (int i = 0; i < roomHeight; i++)
			{
				if (GD.Randf() < 0.1f) // EXTRA DOOR FLOOR INCREASE CHANCE
				{
					targetHeight++;
				}
				else
				{
					break;
				}
			}

			List<RoomFace.ExposedNormal> normals =
				RoomFace.ExposedNormal.GetAllExposedNormals(roomGlobalShape);
			// Selectes normals that are on the selected floor, not facing up, facing into another room and not inside another door
			normals = normals.Where(normal => normal.Position.Y == roomMinHeight + targetHeight && normal.Direction.Y == 0 && compositeShape.Contains(normal.Position + normal.Direction) && !(doorPositions.Contains(normal.Position) || doorPositions.Contains(normal.Position + normal.Direction))).ToList();
			
			if (!normals.Any()) continue; // This really shouldn't happen often

			foreach (RoomFace.ExposedNormal normal in normals)
			{
				// check if rooms are already connected, as to not add redundant connections
				Room[] rooms = new Room[2] { room, Rooms.First(room => room.GlobalShape.Contains(normal.Position + normal.Direction)) };
				List<Vector3I> otherRoomGlobalShape = rooms[1].GlobalShape;
				bool isRedundant = false;
				if (DoorPositions.Any(door => (roomGlobalShape.Contains(door.A) || roomGlobalShape.Contains(door.B)) &&
				                              (otherRoomGlobalShape.Contains(door.A) ||
				                               otherRoomGlobalShape.Contains(door.B)))) continue;

				LinkedVector3I door = new LinkedVector3I(normal.Position,
					normal.Position + normal.Direction);
				AddDoor(door);
				doorPositions.Add(door.A);
				doorPositions.Add(door.B);

				doorIndex++;

				if (doorIndex >= numExtraDoors) return;
			}
		}
	}
	
	public List<Vector3I> GetCompositeShape()
	{
		IEnumerable<Vector3I> compositeShape = Enumerable.Empty<Vector3I>();
		
		foreach (Room room in Rooms)
		{
			compositeShape = compositeShape.Concat(room.GlobalShape);
		}
		
		return compositeShape.ToList();
	}
}

public class RoomFace
{
	public List<ExposedNormal> ExposedNormals { get; private set; }
	
	public int MinHeight { get; private set; }
	public int MaxHeight { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }
	public int MinWidth { get; private set; }
	public int MaxWidth { get; private set; }
	public Vector3I Direction { get; private set; }
	
	public RoomFace(List<ExposedNormal> exposedNormals)
	{
		ExposedNormals = exposedNormals;
		
		if (exposedNormals.Any()) Direction = exposedNormals.First().Direction;
		
		MinHeight = ExposedNormals.Min(x => x.Position.Y);
		MaxHeight = ExposedNormals.Max(x => x.Position.Y);
		Height = Math.Abs(MaxHeight - MinHeight) + 1;
		if (Math.Abs(Direction.X) != 0)
		{
			MinWidth = ExposedNormals.Min(x => x.Position.Z);
			MaxWidth = ExposedNormals.Max(x => x.Position.Z);
			Width = Math.Abs(MaxWidth - MinWidth) + 1;
		}
		else if (Math.Abs(Direction.Z) != 0)
		{
			MinWidth = ExposedNormals.Min(x => x.Position.X);
			MaxWidth = ExposedNormals.Max(x => x.Position.X);
			Width = Math.Abs(MaxWidth - MinWidth) + 1;
		}
		else
		{
			Width = 1;
		}
	}
	
	public static List<RoomFace> GetFaces(List<Vector3I> shape)
	{
		return GetFaces(ExposedNormal.GetAllExposedNormals(shape));
	}
	
	public static List<RoomFace> GetFaces(List<ExposedNormal> exposedNormals)
	{
		List<RoomFace> faces = new List<RoomFace>();
		List<ExposedNormal> normalsLeft = new List<ExposedNormal>(exposedNormals);
		
		while (normalsLeft.Any())
		{
			ExposedNormal start = normalsLeft[0]; 
			List<ExposedNormal> normalsInFace = new FloodFill<ExposedNormal>(normalsLeft, start, ExposedNormal.GetNeighbors, normal => normal.Direction == start.Direction).GetOutput();
			normalsLeft = normalsLeft.Except(normalsInFace).ToList();
			
			faces.Add(new RoomFace(normalsInFace));
		}
		
		return faces;
	}
	
	public class ExposedNormal
	{
		public ExposedNormal(Vector3I position, Vector3I direction)
		{
			Position = position;
			Direction = direction;
		}
		
		public Vector3I Position { get; private set; }
		public Vector3I Direction { get; private set; }
		
		public static List<ExposedNormal> GetExposedNormalsAtPosition(Vector3I position, List<Vector3I> shape)
		{
			List<ExposedNormal> normals = new List<ExposedNormal>();
			
			// This could be cleaner, but fuck it
			if (!shape.Contains(position + Vector3I.Up)) normals.Add(new ExposedNormal(position, Vector3I.Up));
			if (!shape.Contains(position + Vector3I.Down)) normals.Add(new ExposedNormal(position, Vector3I.Down));
			if (!shape.Contains(position + Vector3I.Left)) normals.Add(new ExposedNormal(position, Vector3I.Left));
			if (!shape.Contains(position + Vector3I.Right)) normals.Add(new ExposedNormal(position, Vector3I.Right));
			if (!shape.Contains(position + Vector3I.Forward)) normals.Add(new ExposedNormal(position, Vector3I.Forward));
			if (!shape.Contains(position + Vector3I.Back)) normals.Add(new ExposedNormal(position, Vector3I.Back));
			
			return normals;
		}
		
		public static ExposedNormal[] GetNeighbors(ExposedNormal input, List<ExposedNormal> all)
		{
			return all.Where(normal => ((Vector3)(normal.Position - input.Position)).LengthSquared() == 1f).ToArray();
		}
		
		public static List<ExposedNormal> GetAllExposedNormals(List<Vector3I> shape)
		{
			IEnumerable<ExposedNormal> exposedNormals = Enumerable.Empty<ExposedNormal>();
			
			foreach (Vector3I position in shape)
			{
				exposedNormals = exposedNormals.Concat(ExposedNormal.GetExposedNormalsAtPosition(position, shape));
			}
			
			return exposedNormals.ToList();
		}
	}
}

public abstract class RoomGeneration
{
	public List<Vector3I> GlobalShape { get; private set; }
	protected List<Vector3I> Shape
	{
		get { return _shape; }
		set { _shape = value; GlobalShape = GetGlobalShape(); }
	}
	private List<Vector3I> _shape = new List<Vector3I>();
	
	protected Vector3I _origin;
	protected Vector3I _direction;
	
	public RoomGeneration(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		_origin = pointFrom;
		_direction = direction;
		
		_shape = GenerateShape(pointFrom, direction);
	}
	
	protected virtual List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		List<Vector3I> shape = new List<Vector3I>();
		
		return shape;
	}
	
	protected List<Vector3I> GenerateBaseShape(int length, int width, int height)
	{
		List<Vector3I> shape = new List<Vector3I>();
		int widthOffset = GD.RandRange(-width+1,0);
		
		for (int l = 0; l < length; l++)
		{
			for (int w = 0; w < width; w++)
			{
				for (int h = 0; h < height; h++)
				{
					shape.Add(new Vector3I(w+widthOffset,h,l));
				}
			}
		}
		
		return shape;
	}
	
	protected List<Vector3I> AddShapeRandomness(List<Vector3I> shape, int extrusionLength, List<Vector3I> context = null)
	{
		List<RoomFace.ExposedNormal> exposedNormals = RoomFace.ExposedNormal.GetAllExposedNormals(shape);
		// Remove normals pointing to other rooms, if context is provided (increases chances that the algorithm will not fuck up and have to try again)
		if(context != null) exposedNormals = exposedNormals.Where(normal => !context.Contains(LocalToGlobal(normal.Position + normal.Direction))).ToList();
		List<RoomFace> faces = RoomFace.GetFaces(exposedNormals);
		// Remove 1 width forces and sort by width (descending)
		faces = faces.Where(face => face.Width != 1).OrderBy(face => -face.Width).ToList();
		// Prioritizes higher width
		double rand = Math.Min(Math.Abs(GD.Randfn(0.0,1.0))/6.0,.999999); // Approx normal dist from 0-1
		int faceIndex = (int)Math.Floor(rand * faces.Count());
		RoomFace selectedFace = faces[faceIndex];

		bool sliceDirection = GD.Randf() < 0.5f; // true - horizontal, false - vertical
		if (selectedFace.Height == 1) sliceDirection = true; // Don't slice one tall face vertically
		
		List<Vector3I> selectedPositions;
		Func<Vector3I,Boolean> condition = position => true;
		if (sliceDirection) // horizontal
		{
			int sliceWidth = GD.RandRange(0,selectedFace.Width-1);
			if (selectedFace.Direction.X != 0)
			{
				if (GD.Randf() < 0.5f)
				{
					condition = position => position.Z >= selectedFace.MinWidth + sliceWidth;
				}
				else
				{
					condition = position => position.Z <= selectedFace.MaxWidth - sliceWidth;
				}
			}
			else if (selectedFace.Direction.Z != 0)
			{
				if (GD.Randf() < 0.5f)
				{
					condition = position => position.X >= selectedFace.MinWidth + sliceWidth;
				}
				else
				{
					condition = position => position.X <= selectedFace.MaxWidth - sliceWidth;
				}
			}
		}
		else // vertical
		{
			int sliceWidth = GD.RandRange(0,selectedFace.Height-1);
			if (GD.Randf() < 0.5f) // Slice from top or bottom
			{ // bottom
				condition = position => position.Y >= selectedFace.MinHeight + sliceWidth;
			}
			else
			{ // top
				condition = position => position.Y <= selectedFace.MaxHeight - sliceWidth;
			}
		}
		
		selectedPositions = selectedFace.ExposedNormals.Select(normal => normal.Position).Where(condition).ToList();
		
		List<Vector3I> extrusionPositions = new List<Vector3I>();
		for (int i = 1; i < extrusionLength + 1; i++)
		{
			foreach (Vector3I position in selectedPositions)
			{
				extrusionPositions.Add(position + (selectedFace.Direction * i));
			}
		}

		// Don't extrude into shape that already exists
		extrusionPositions = extrusionPositions.Except(shape).ToList();
		
		return extrusionPositions;
	}
	
	protected List<Vector3I> GetPositionsInBounds(Vector3I pos1, Vector3I pos2) // pos1 < pos2 ALL ELEMENTS, TODO: Maybe not the best place for this function
	{
		List<Vector3I> positions = new List<Vector3I>();
		
		for (int x = pos1.X; x < pos2.X; x++)
		{
			for (int y = pos1.Y; y < pos2.Y; y++)
			{
				for (int z = pos1.Z; z < pos2.Z; z++)
				{
					positions.Add(new Vector3I(x,y,z));
				}
			}
		}
		
		return positions;
	}

	public void PathfindInterior()
	{
		
	}
	
	public void Translate(Vector3I amount)
	{
		_origin += amount;
		GlobalShape = GetGlobalShape();
	}
	
	public Vector3I LocalToGlobal(Vector3I localPosition)
	{
		Vector3 directionConverted = (Vector3)_direction;
		return _origin + (_direction * localPosition.Z) + (Vector3I.Up * localPosition.Y) + ((Vector3I)directionConverted.Rotated(Vector3.Up, (float)-Math.PI/2) * localPosition.X);
	}
	
	public List<Vector3I> GetGlobalShape()
	{
		List<Vector3I> globalPositions = new List<Vector3I>();
		foreach (Vector3I position in _shape)
		{
			globalPositions.Add(LocalToGlobal(position));
		}
		return globalPositions;
	}
}