using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DungeonRoomGenerator
{
	public RoomGeneration GetRoomGenerationFromDistribution(Vector3I pointFrom, Vector3I direction)
	{
		float random = GD.Randf();
		
		if (random < 0.05f) return new LargeRoomGeneration(pointFrom,direction);
		if (random < 0.4f) return new LongRoomGeneration(pointFrom,direction);
		else return new MediumRoomGeneration(pointFrom,direction);
	}
	
	public List<Room> GenerateRoomCluster(int numRooms)
	{
		RoomCluster cluster = new RoomCluster();
		cluster.AddRoom(new Room(GetRoomGenerationFromDistribution(Vector3I.Zero, Vector3I.Back)));
		
		for (int i = 0; i < numRooms - 1; i++)
		{
			SimplePriorityQueue<RoomPlacementNormal, float> roomPlacementNormalsQueue = new SimplePriorityQueue<RoomPlacementNormal, float>();
			List<RoomCluster.RoomFace> roomFaces = cluster.GetFaces(cluster.GetAllExposedNormals()).Where(face => face.Direction != Vector3I.Up || face.Direction != Vector3I.Up).ToList();
			
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
				RoomCluster.ExposedNormal normalFrom = roomPlacementNormalsQueue.Dequeue().HeldNormal;
				
				if (normalFrom.Direction == Vector3I.Up || normalFrom.Direction == Vector3I.Down) continue;
				generatedRoom = new Room(GetRoomGenerationFromDistribution(normalFrom.Position + normalFrom.Direction, normalFrom.Direction));
				if(cluster.AddRoom(generatedRoom)) break;
			}
		}
		
		cluster.Abs();
		return cluster.Rooms;
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
		public RoomCluster.RoomFace HeldFace {get;private set;}
		public RoomCluster.ExposedNormal HeldNormal {get;private set;}
		
		public RoomPlacementNormal(RoomCluster.RoomFace heldFace, RoomCluster.ExposedNormal heldNormal)
		{
			HeldFace = heldFace;
			HeldNormal = heldNormal;
		}
	}
}

public class RoomCluster
{
	public List<Room> Rooms { get; private set; }
	
	public RoomCluster()
	{
		Rooms = new List<Room>();
	}
	
	public bool AddRoom(Room addend)
	{
		if (GetCompositeShape().Intersect(addend.RoomGeneration.GlobalShape).Any()) return false;
		
		Rooms.Add(addend);
		return true;
	}
	
	public Vector3I Abs() 
	{
		Vector3I smallest = Util.GetSmallestIndividual(Rooms.Select(room => Util.GetSmallestIndividual(room.RoomGeneration.GlobalShape.ToArray())).ToArray());
		Translate(-smallest);
		return -smallest;
	}
	
	public void Translate(Vector3I amount)
	{
		foreach (Room room in Rooms)
		{
			room.RoomGeneration.Translate(amount);
		}
	}
	
	public List<ExposedNormal> GetAllExposedNormals()
	{
		IEnumerable<ExposedNormal> exposedNormals = Enumerable.Empty<ExposedNormal>();
		List<Vector3I> compositeShape = GetCompositeShape();
		
		foreach (Vector3I position in compositeShape)
		{
			exposedNormals = exposedNormals.Concat(ExposedNormal.GetExposedNormalsAtPosition(position, compositeShape));
		}
		
		return exposedNormals.ToList();
	}
	
	public List<RoomFace> GetFaces(List<ExposedNormal> exposedNormals) // May Not be working
	{
		List<RoomFace> faces = new List<RoomFace>();
		List<ExposedNormal> normalsLeft = new List<ExposedNormal>(exposedNormals);
		
		while (normalsLeft.Any())
		{
			ExposedNormal start = normalsLeft[0]; 
			List<ExposedNormal> normalsInFace = new FloodFill<ExposedNormal>(normalsLeft, start, GetNeighbors, normal => normal.Direction == start.Direction).GetOutput();
			normalsLeft = normalsLeft.Except(normalsInFace).ToList();
			
			faces.Add(new RoomFace(normalsInFace));
		}
		
		return faces;
	}
	
	List<Vector3I> GetCompositeShape()
	{
		IEnumerable<Vector3I> compositeShape = Enumerable.Empty<Vector3I>();
		
		foreach (Room room in Rooms)
		{
			compositeShape = compositeShape.Concat(room.RoomGeneration.GlobalShape);
		}
		
		return compositeShape.ToList();
	}
	
	public ExposedNormal[] GetNeighbors(ExposedNormal input, List<ExposedNormal> all)
	{
		return all.Where(normal => ((Vector3)(normal.Position - input.Position)).LengthSquared() == 1f).ToArray();
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
	}

	public class RoomFace
	{
		public List<ExposedNormal> ExposedNormals { get; private set; }
		
		public int MinHeight { get; private set; }
		public int MaxHeight { get; private set; }
		public Vector3I Direction { get; private set; }
		
		public RoomFace(List<ExposedNormal> exposedNormals)
		{
			ExposedNormals = exposedNormals;
			
			if (exposedNormals.Any()) Direction = exposedNormals.First().Direction;
			
			MinHeight = ExposedNormals.Min(x => x.Position.Y);
			MaxHeight = ExposedNormals.Max(x => x.Position.Y);
		}
	}
	
	/*
		_faces = GetFaces();
				
		_exposedNormals.Clear();
		
	*/
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
	
	public RoomGeneration(Vector3I pointFrom, Vector3I direction)
	{
		_shape = GenerateShape(pointFrom, direction);
		
		_origin = pointFrom;
		_direction = direction;
	}
	
	protected virtual List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
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
	
	public void Translate(Vector3I amount)
	{
		_origin += amount;
		GlobalShape = GetGlobalShape();
	}
	
	public List<Vector3I> GetGlobalShape()
	{
		List<Vector3I> globalPositions = new List<Vector3I>();
		Vector3 directionConverted = (Vector3)_direction;
		foreach (Vector3I position in _shape)
		{
			globalPositions.Add(_origin + (_direction * position.Z) + (Vector3I.Up * position.Y) + ((Vector3I)directionConverted.Rotated(Vector3.Up, (float)-Math.PI/2) * position.X));
		}
		return globalPositions;
	}
}

public class MediumRoomGeneration : RoomGeneration
{
	public MediumRoomGeneration(Vector3I pointFrom, Vector3I direction) : base(pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(3,6);
		int width = GD.RandRange(3,6);
		int height = 1;
		if (GD.Randf() <= 0.4f) { height += 1; }
		
		return GenerateBaseShape(length,width,height);
	}
}

public class LongRoomGeneration : RoomGeneration
{
	public LongRoomGeneration(Vector3I pointFrom, Vector3I direction) : base(pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(6,8);
		int width = GD.RandRange(2,3);
		int height = 1;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		return GenerateBaseShape(length,width,height);
	}
}

public class LargeRoomGeneration : RoomGeneration
{
	public LargeRoomGeneration(Vector3I pointFrom, Vector3I direction) : base(pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(7,9);
		int width = GD.RandRange(7,9);
		int height = 2;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		return GenerateBaseShape(length,width,height);
	}
}
