using Godot;
using System;
using System.Collections.Generic;

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