using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DungeonRoomGenerator
{
	/*public Room GenerateOneRoom()
	{
		
	}
	
	public List<Room> GenerateRoomCluster()
	{
		
	}*/
	
	public Room TestRoomGeneration()
	{
		RoomCluster cluster = new RoomCluster();
		cluster.AddRoom(new Room(new MediumRoomGeneration(Vector3I.Zero, Vector3I.Back)));
		cluster.Abs();
		
		return cluster.Rooms[0];
	}
}

public class RoomCluster
{
	public List<Room> Rooms { get; private set; }
	
	public RoomCluster()
	{
		Rooms = new List<Room>();
	}
	
	public void AddRoom(Room addend)
	{
		Rooms.Add(addend);
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
}

public abstract class RoomGeneration
{
	public List<Vector3I> Shape {
		get {
			return _shape;
		}
		set {
			_shape = value;
			_faces = GetFaces();
			
			_exposedNormals.Clear();
			foreach (Vector3I position in _shape)
			{
				_exposedNormals.Concat(GetExposedNormals(position)).ToList();
			}
		}
	}
	
	private List<Vector3I> _shape = new List<Vector3I>();
	private List<RoomFace> _faces = new List<RoomFace>();
	private List<ExposedNormal> _exposedNormals = new List<ExposedNormal>();
	
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
			_shape[i] = _shape[i] + amount;
		}
		
		Shape = Shape; // Recalculate faces.. etc
	}
	
	List<RoomFace> GetFaces()
	{
		List<RoomFace> faces = new List<RoomFace>();
		List<ExposedNormal> normalsLeft = new List<ExposedNormal>(_exposedNormals);
		
		while (normalsLeft.Any())
		{
			ExposedNormal start = _exposedNormals[0];
			List<ExposedNormal> normalsInFace = new FloodFill<ExposedNormal>(_exposedNormals, start, GetNeighbors, normal => normal.Direction == start.Direction).GetOutput();
			normalsLeft = normalsLeft.Except(normalsInFace).ToList();
			
			faces.Add(new RoomFace(normalsInFace.Select(normal => normal.Position).ToList(), start.Direction));
		}
		
		return new List<RoomFace>();
	}
	
	List<ExposedNormal> GetExposedNormals(Vector3I position)
	{
		List<ExposedNormal> normals = new List<ExposedNormal>();
		
		// This could be cleaner, but fuck it
		if (!_shape.Contains(position + Vector3I.Up)) normals.Add(new ExposedNormal(position, Vector3I.Up));
		if (!_shape.Contains(position + Vector3I.Down)) normals.Add(new ExposedNormal(position, Vector3I.Down));
		if (!_shape.Contains(position + Vector3I.Left)) normals.Add(new ExposedNormal(position, Vector3I.Left));
		if (!_shape.Contains(position + Vector3I.Right)) normals.Add(new ExposedNormal(position, Vector3I.Right));
		if (!_shape.Contains(position + Vector3I.Forward)) normals.Add(new ExposedNormal(position, Vector3I.Forward));
		if (!_shape.Contains(position + Vector3I.Back)) normals.Add(new ExposedNormal(position, Vector3I.Back));
		
		return normals;
	}
	
	public ExposedNormal[] GetNeighbors(ExposedNormal input, List<ExposedNormal> all)
	{
		return all.Where(normal => ((Vector3)(normal.Position - input.Position)).LengthSquared() == 1f).ToArray();
	}
	
	public struct ExposedNormal
	{
		public ExposedNormal(Vector3I position, Vector3I direction)
		{
			Position = position;
			Direction = direction;
		}
		
		public Vector3I Position;
		public Vector3I Direction;
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
		GD.Print(pointFrom);
		GD.Print(offsetPosition);
		
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
