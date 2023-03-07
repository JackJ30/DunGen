using System.Collections;
using System.Collections.Generic;
using System;
using Godot;

public partial class Grid3D<T> {
	T[] data;

	public Vector3I Size { get; private set; }
	public Aabb Bounds { get; private set; }
	public Vector3I Offset { get; set; }

	public Grid3D(Vector3I size, Vector3I offset) {
		Size = size;
		Offset = offset;
		Bounds = new Aabb(offset, size);

		data = new T[size.X * size.Y * size.Z];
	}

	public void AssignAll(Func<Vector3I, T> valueFunction)
	{
		for(int x = 0; x < data.GetLength(0); x++)
		{
			for(int y = 0; y < data.GetLength(0); y++)
			{
				for(int z = 0; z < data.GetLength(0); z++)
				{
					this[x,y,z] = valueFunction(new Vector3I(x,y,z));
				}
			}
		}
	}
	
	public void AssignBounds(Aabb bounds, Func<Vector3I, T> valueFunction)
	{
		if (!Bounds.Encloses(bounds)) return;
		
		for(int x = (int)bounds.Position.X; x < (int)bounds.Position.X + (int)bounds.Size.X; x++)
		{
			for(int y = (int)bounds.Position.Y; y < (int)bounds.Position.Y + (int)bounds.Size.Y; y++)
			{
				for(int z = (int)bounds.Position.Z; z < (int)bounds.Position.Z + (int)bounds.Size.Z; z++)
				{
					this[x,y,z] = valueFunction(new Vector3I(x,y,z));
				}
			}
		}
	}
	
	public int GetIndex(Vector3I pos) {
		return pos.X + (Size.X * pos.Y) + (Size.X * Size.Y * pos.Z);
	}

	public bool InBounds(Vector3I pos) {
		Vector3I posOffset = pos + Offset;
		return (posOffset.X < Size.X && posOffset.X >= 0) && (posOffset.Y < Size.Y && posOffset.Y >= 0) && (posOffset.Z < Size.Z && posOffset.Z >= 0);
	}

	public T this[int X, int Y, int Z] {
		get {
			return this[new Vector3I(X, Y, Z)];
		}
		set {
			this[new Vector3I(X, Y, Z)] = value;
		}
	}

	public T this[Vector3I pos] {
		get {
			pos += Offset;
			return data[GetIndex(pos)];
		}
		set {
			pos += Offset;
			data[GetIndex(pos)] = value;
		}
	}
}
