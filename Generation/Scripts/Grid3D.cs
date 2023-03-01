using System.Collections;
using System.Collections.Generic;
using System;
using Godot;

public partial class Grid3D<T> {
	T[] data;

	public Vector3I Size { get; private set; }
	public Vector3I Offset { get; set; }

	public Grid3D(Vector3I size, Vector3I offset) {
		Size = size;
		Offset = offset;

		data = new T[size.X * size.Y * size.Z];
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
