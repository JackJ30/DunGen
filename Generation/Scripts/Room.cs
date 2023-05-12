using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

[DataContract]
public class Room : DungeonLevelSegment
{
	public List<Vector3I> GlobalShape { get; private set; }
	protected List<Vector3I> Shape
	{
		get { return _shape; }
		set { _shape = value; GlobalShape = GetGlobalShape(); }
	}
	private List<Vector3I> _shape = new List<Vector3I>();
	
	public Room(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null) : base(pointFrom,direction)
	{
		_shape = GenerateShape(pointFrom, direction);
	}
	
	#region Segment

	public bool Intersects(Room other) // Maybe revamp with one cell padding
	{
		return GlobalShape.Intersect(other.GetOccupiedPositions()).Any();
	}
	
	public bool InBounds(Grid3D<Cell> grid)
	{
		foreach (Vector3I position in GlobalShape)
		{
			if(!grid.InBounds(position))
			{
				return false;
			}
		}
		
		return true;
	}
	
	public override Vector3I[] GetOccupiedPositions()
	{
		return _shape.ToArray();
	}
	
	public override bool NeighborEvaluator(Cell cellFrom, Cell cellTo, Vector3I delta)
	{
		if (cellFrom.HasConnection(cellTo)) return true;
		if (!cellTo.HasSegment<Room>()) return false;
		if (!cellFrom.GetSegments<Room>().Select(x => x.id).Intersect(cellTo.GetSegments<Room>().Select(x => x.id)).Any()) return false;

		return true;
	}
	
	#endregion

	#region Shape
	
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

	protected Vector3I Abs()
	{
		//return Vector3I.Zero;
		
		Vector3I translation = Util.GetSmallestIndividual(Shape.ToArray());

		TranslateLocally(-translation);
		Translate(LocalToGlobal(translation) - Origin); // Quick way to convert local translation direction to global

		return -translation;
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
	
	public override void Translate(Vector3I amount) // TODO: Figure out a way to make this also move positions that are assigned to a grid (or decide we don't do that)
	{
		base.Translate(amount);
		GlobalShape = GetGlobalShape();
	}
	private void TranslateLocally(Vector3I amount)
	{
		for (int i = 0; i < _shape.Count(); i++)
		{
			_shape[i] += amount;
		}
		GlobalShape = GetGlobalShape();
	}

	public Vector3 GetAveragePosition()
	{
		float meanPositionX = 0f;
		float meanPositionY = 0f;
		float meanPositionZ = 0f;
		
		foreach (Vector3I position in GlobalShape)
		{
			meanPositionX += (float)position.X;
			meanPositionY += (float)position.Y;
			meanPositionZ += (float)position.Z;
		}
		
		meanPositionX /= GlobalShape.Count();
		meanPositionY /= GlobalShape.Count();
		meanPositionZ /= GlobalShape.Count();
		
		return new Vector3(meanPositionX,meanPositionY,meanPositionZ);
	}
	
	public Vector3I[] GetFloorPositions(Grid3D<Cell> grid)
	{
		List<Vector3I> result = new List<Vector3I>();
		
		foreach (Vector3I position in GlobalShape)
		{
			if (!grid.InBounds(position + new Vector3I(0,-1,0))) result.Add(position); // If out of bounds
			else if (!grid[position + new Vector3I(0,-1,0)].Segments.Contains(this)) result.Add(position); // If segment below is not this room
		}
		
		return result.ToArray();
	}

	public Grid3D<Cell> ToGrid()
	{
		Abs();
		Grid3D<Cell> grid = new Grid3D<Cell>(Util.GetLargestIndividual(Shape.ToArray()), Vector3I.Zero);

		AssignCells(grid);
		
		return null;
	}
	
	private List<Vector3I> GetGlobalShape()
	{
		List<Vector3I> globalPositions = new List<Vector3I>();
		foreach (Vector3I position in _shape)
		{
			globalPositions.Add(LocalToGlobal(position));
		}
		return globalPositions;
	}

	#endregion
}

#region Room Types

public class MediumRoom : Room
{
	public MediumRoom(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null) : base(pointFrom, direction, context)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		int length = GD.RandRange(3,6);
		int width = GD.RandRange(3,6);
		int height = 1;
		if (GD.Randf() <= 0.4f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		IEnumerable<Vector3I> workingShape = GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		return workingShape.ToList();
	}
}

public class LongRoom : Room
{
	public LongRoom(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null) : base(pointFrom, direction, context)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		int length = GD.RandRange(6,8);
		int width = GD.RandRange(2,3);
		int height = 1;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		IEnumerable<Vector3I> workingShape = GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		return workingShape.ToList();
	}
}

public class LargeRoom : Room
{
	public LargeRoom(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null) : base(pointFrom, direction, context)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
	{
		int length = GD.RandRange(7,9);
		int width = GD.RandRange(7,9);
		int height = 2;
		if (GD.Randf() <= 0.25f) { height += 1; }
		
		int widthOffset = GD.RandRange(-width+1,0);
		
		IEnumerable<Vector3I> workingShape = GetPositionsInBounds(new Vector3I(0+widthOffset,0,0),new Vector3I(width+widthOffset,height,length));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context));
		return workingShape.ToList();
	}
}

public class TShapedRoom : Room
{
	public TShapedRoom(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null) : base(pointFrom, direction, context)
	{
		Shape = GenerateShape(pointFrom, direction);
	}
	
	protected override List<Vector3I> GenerateShape(Vector3I pointFrom, Vector3I direction, List<Vector3I> context = null)
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
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context)).ToList();
		workingShape = workingShape.Concat(AddShapeRandomness(workingShape.ToList(),2, context)).ToList();
		
		return workingShape;
	}
}


#endregion