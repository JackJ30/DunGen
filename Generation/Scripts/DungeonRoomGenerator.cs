using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class DungeonRoomGenerator
{
	public RoomGeneration GetRoomGenerationFromDistribution(RoomCluster cluster, Vector3I pointFrom, Vector3I direction)
	{
		float random = GD.Randf();
		
		if (random < 0.05f) return new LargeRoomGeneration(cluster,pointFrom,direction);
		if (random < 0.25f) return new LongRoomGeneration(cluster,pointFrom,direction);
		if (random < 0.50f) return new TShapedRoomGeneration(cluster,pointFrom,direction);
		else return new MediumRoomGeneration(cluster,pointFrom,direction);
	}
	
	public List<Room> GenerateRoomCluster(int numRooms)
	{
		RoomCluster cluster = new RoomCluster();
		cluster.AddRoom(new Room(GetRoomGenerationFromDistribution(cluster,Vector3I.Zero, Vector3I.Back)));
		
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
				generatedRoom = new Room(GetRoomGenerationFromDistribution(cluster,normalFrom.Position + normalFrom.Direction, normalFrom.Direction));
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
	
	public List<Vector3I> GetCompositeShape()
	{
		IEnumerable<Vector3I> compositeShape = Enumerable.Empty<Vector3I>();
		
		foreach (Room room in Rooms)
		{
			compositeShape = compositeShape.Concat(room.RoomGeneration.GlobalShape);
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
	protected RoomCluster _cluster;
	
	public RoomGeneration(RoomCluster cluster, Vector3I pointFrom, Vector3I direction)
	{
		_cluster = cluster;
		_origin = pointFrom;
		_direction = direction;
		
		_shape = GenerateShape(pointFrom, direction);
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
	
	protected List<Vector3I> AddShapeRandomness(List<Vector3I> shape, int extrusionLength)
	{
		List<RoomFace.ExposedNormal> exposedNormals = RoomFace.ExposedNormal.GetAllExposedNormals(shape);
		List<Vector3I> clusterShape = _cluster.GetCompositeShape();
		// Remove normals pointing to other rooms
		exposedNormals = exposedNormals.Where(normal => !clusterShape.Contains(LocalToGlobal(normal.Position + normal.Direction))).ToList();
		List<RoomFace> faces = RoomFace.GetFaces(exposedNormals);
		// Remove 1 width forces and sort by width (descending)
		faces = faces.Where(face => face.Width != 1).OrderBy(face => -face.Width).ToList();
		// Prioritizes higher width
		RoomFace selectedFace = faces[(int)Math.Floor(GD.Randfn(0.0,1.0) * faces.Count())];
		
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
		
		return extrusionPositions;
	}
	
	protected List<Vector3I> GetPositionsInBounds(Vector3I pos1, Vector3I pos2) // pos1 < pos2 ALL ELEMENTS
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

public class MediumRoomGeneration : RoomGeneration
{
	public MediumRoomGeneration(RoomCluster cluster, Vector3I pointFrom, Vector3I direction) : base(cluster, pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(3,6);
		int width = GD.RandRange(3,6);
		int height = 1;
		if (GD.Randf() <= 0.4f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		return GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
	}
}

public class LongRoomGeneration : RoomGeneration
{
	public LongRoomGeneration(RoomCluster cluster, Vector3I pointFrom, Vector3I direction) : base(cluster, pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(6,8);
		int width = GD.RandRange(2,3);
		int height = 1;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		return GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
	}
}

public class LargeRoomGeneration : RoomGeneration
{
	public LargeRoomGeneration(RoomCluster cluster, Vector3I pointFrom, Vector3I direction) : base(cluster, pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(7,9);
		int width = GD.RandRange(7,9);
		int height = 2;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		return GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
	}
}

public class TShapedRoomGeneration : RoomGeneration
{
	public TShapedRoomGeneration(RoomCluster cluster, Vector3I pointFrom, Vector3I direction) : base(cluster, pointFrom, direction)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction)
	{
		int length = GD.RandRange(6,8);
		int width = GD.RandRange(2,3);
		int height = 1;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		List<Vector3I> workingShape = GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
		
		int centerX = widthOffset + (width/2);
		int topWidth = GD.RandRange(6,8);
		int topLength = GD.RandRange(2,3);
		int topHeight = height;
		int topLengthOffset = GD.RandRange(length-3,length)-topLength;
		
		Vector3I topPos1 = new Vector3I(centerX-(topWidth/2),0,topLengthOffset);
		Vector3I topPos2 = new Vector3I(centerX+(topWidth/2),topHeight,topLengthOffset+topLength);
		
		workingShape = workingShape.Concat(GetPositionsInBounds(topPos1,topPos2)).Distinct().ToList();
		
		return workingShape;
	}
}
