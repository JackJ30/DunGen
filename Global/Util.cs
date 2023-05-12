using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

public static class Util
{
	// ONLY WORKS WITH INTEGER VALUES
	public static List<Vector3I> AabbToList(Aabb original)
	{
		List<Vector3I> final = new List<Vector3I>();
		
		GD.Print((int)Math.Round(original.Position.X,0));
		GD.Print((int)Math.Round(original.Size.X,0));
		for (int x = (int)Math.Round(original.Position.X,0); x <= (int)Math.Round(original.Position.X,0) + (int)Math.Round(original.Size.X,0); x++)
		{
			GD.Print("testx");
			for (int y = (int)Math.Round(original.Position.Y,0); y <= (int)Math.Round(original.Position.Y,0) + (int)Math.Round(original.Size.Y,0); y++)
			{
				GD.Print("testy");
				for (int z = (int)Math.Round(original.Position.Z,0); z <= (int)Math.Round(original.Position.Z,0) + (int)Math.Round(original.Size.Z,0); z++)
				{
					GD.Print("testz");
					final.Add(new Vector3I(x,y,z));
				}
			}
		}
		
		return final;
	}
	
	public static Vector3I GetSmallestIndividual(Vector3I[] input)
	{
		Vector3I smallest = new Vector3I(int.MaxValue,int.MaxValue,int.MaxValue);
		
		foreach (Vector3I vector3I in input)
		{
			if (vector3I.X < smallest.X) { smallest.X = vector3I.X; }
			if (vector3I.Y < smallest.Y) { smallest.Y = vector3I.Y; }
			if (vector3I.Z < smallest.Z) { smallest.Z = vector3I.Z; }
		}
		
		return smallest;
	}
	
	public static Vector3I GetLargestIndividual(Vector3I[] input)
	{
		Vector3I largest = new Vector3I(int.MinValue,int.MinValue,int.MinValue);
		
		foreach (Vector3I vector3I in input)
		{
			if (vector3I.X > largest.X) { largest.X = vector3I.X; }
			if (vector3I.Y > largest.Y) { largest.Y = vector3I.Y; }
			if (vector3I.Z > largest.Z) { largest.Z = vector3I.Z; }
		}
		
		return largest;
	}

	public static int DirectionToRotationNumberXZ(Vector3I direction)
	{
		if (direction.X == 1 && direction.Z == 0) return 0;
		if (direction.X == 0 && direction.Z == 1) return 1;
		if (direction.X == -1 && direction.Z == 0) return 2;
		if (direction.X == 0 && direction.Z == -1) return 3;
		GD.PushError("Direction is not aligned to an axis");
		return -1;
	}
	
	public static Vector3I RotationNumberToDirectionXZ(int rotationNumber)
	{
		int remainder = rotationNumber % 4;
		
		if (remainder == 0) return new Vector3I(1,0,0);
		if (remainder == 1) return new Vector3I(0,0,1);
		if (remainder == 2) return new Vector3I(-1,0,0);
		if (remainder == 3) return new Vector3I(0,0,-1);
		GD.PushError("This should never happen, bug with rotation number");
		return Vector3I.Zero;
	}
}

public class FloodFill<T>
{
	public delegate T[] GetNeighbors(T input, List<T> all);
	public delegate bool CriteriaChecker(T input);
	
	private List<T> _all;
	private List<T> _closed;
	private List<T> _final;
	
	public FloodFill(List<T> all, T start, GetNeighbors getNeighbors, CriteriaChecker criteriaChecker)
	{
		_all = all;
		_closed = new List<T>();
		_final = new List<T>();
		
		Flood(start, getNeighbors, criteriaChecker);
	}
	
	public void Flood(T start, GetNeighbors getNeighbors, CriteriaChecker criteriaChecker)
	{
		if (!_all.Contains(start) || _final.Contains(start) || _closed.Contains(start)) return;
		
		if (criteriaChecker(start))
		{
			_final.Add(start);
			
			foreach (T neighbor in getNeighbors(start, _all))
			{
				Flood(neighbor, getNeighbors, criteriaChecker);
			}
		}
		else
		{
			_closed.Add(start);
		}
	}
	
	public List<T> GetOutput()
	{
		return _final;
	}
}


public class LinkedVector3I
{
	public Vector3I A;
	public Vector3I B;

	public LinkedVector3I(Vector3I A, Vector3I B)
	{
		this.A = A;
		this.B = B;
	}

	public bool Contains(Vector3I element)
	{
		return A == element || B == element;
	}
}