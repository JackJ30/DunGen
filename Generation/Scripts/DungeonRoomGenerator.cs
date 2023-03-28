using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DungeonRoomGenerator
{
	public List<Room> GenerateRoomCluster(int numRooms)
	{
		RoomCluster cluster = new RoomCluster();
		cluster.AddRoom(new Room(new MediumRoomGeneration(Vector3I.Zero, Vector3I.Back)));
		
		for (int i = 0; i < numRooms - 1; i++)
		{
			List<RoomCluster.ExposedNormal> exposedNormals = cluster.GetAllExposedNormals();
			
			Room generatedRoom = null;
			
			while (generatedRoom == null)
			{
				int random = GD.RandRange(0,exposedNormals.Count - 1);
				GD.Print(random);
				RoomCluster.ExposedNormal normalFrom = exposedNormals[random];
				generatedRoom = new Room(new MediumRoomGeneration(normalFrom.Position, normalFrom.Direction));
				if(!cluster.AddRoom(generatedRoom)) generatedRoom = null;
			}
		}
		
		cluster.Abs();
		return cluster.Rooms;
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
		//if (GetCompositeShape().Intersect(addend.RoomGeneration.Shape).Any()) return false;
		
		Rooms.Add(addend);
		return true;
	}
	
	public void Abs() 
	{
		Vector3I smallest = Util.GetSmallestIndividual(Rooms.Select(room => Util.GetSmallestIndividual(room.RoomGeneration.Shape.ToArray())).ToArray());
		Translate(-smallest);
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
	
	List<Vector3I> GetCompositeShape()
	{
		IEnumerable<Vector3I> compositeShape = Enumerable.Empty<Vector3I>();
		
		foreach (Room room in Rooms)
		{
			compositeShape = compositeShape.Concat(room.RoomGeneration.Shape);
		}
		
		return compositeShape.ToList();
	}
	
	List<RoomFace> GetFaces(List<ExposedNormal> exposedNormals)
	{
		List<RoomFace> faces = new List<RoomFace>();
		List<ExposedNormal> normalsLeft = new List<ExposedNormal>(exposedNormals);
		
		while (normalsLeft.Any())
		{
			ExposedNormal start = exposedNormals[0]; 
			List<ExposedNormal> normalsInFace = new FloodFill<ExposedNormal>(exposedNormals, start, GetNeighbors, normal => normal.Direction == start.Direction).GetOutput();
			normalsLeft = normalsLeft.Except(normalsInFace).ToList();
			
			faces.Add(new RoomFace(normalsInFace.Select(normal => normal.Position).ToList(), start.Direction));
		}
		
		return new List<RoomFace>();
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
		
		public Vector3I Position;
		public Vector3I Direction;
		
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
		public RoomFace(List<Vector3I> interiorPositions, Vector3I direction)
		{
			_interiorPositions = interiorPositions;
			_direction = direction;
		}
		
		private List<Vector3I> _interiorPositions;
		private Vector3I _direction;
	}
	
	/*
		_faces = GetFaces();
				
		_exposedNormals.Clear();
		
	*/
}

public abstract class RoomGeneration
{
	public List<Vector3I> Shape = new List<Vector3I>();
	
	public RoomGeneration(Vector3I pointFrom, Vector3I direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected virtual List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		List<Vector3I> shape = new List<Vector3I>();
		
		return shape;
	}
	
	public void Translate(Vector3I amount)
	{
		for (int i = 0; i < Shape.Count; i++)
		{
			Shape[i] = Shape[i] + amount;
		}
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
		List<Vector3I> shape = new List<Vector3I>();
		
		int length = GD.RandRange(3,6);
		int width = GD.RandRange(3,6);
		int height = 1;
		if (GD.Randf() <= 0.4f) { height += 1; }
		int widthOffset = GD.RandRange(-width,0); // This might be a plus/minus one issue
		Vector3 directionConverted = (Vector3)direction;
		Vector3I offsetPosition = pointFrom + (widthOffset * (Vector3I)directionConverted.Rotated(Vector3.Up, (float)-Math.PI/2));
		
		for (int l = 0; l < length; l++)
		{
			for (int w = 0; w < width; w++)
			{
				for (int h = 0; h < height; h++)
				{
					shape.Add(offsetPosition + (direction * l) + (Vector3I.Up * h) + ((Vector3I)directionConverted.Rotated(Vector3.Up, (float)-Math.PI/2) * w));
				}
			}
		}
		
		return shape;
	} 
}
