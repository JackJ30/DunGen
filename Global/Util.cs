using Godot;
using System;
using System.Collections.Generic;

public static class Util
{
	// ONLY WORKS WITH INTEGER VALUES
	public static List<Vector3I> AabbToList(Aabb original)
	{
		List<Vector3I> final = new List<Vector3I>();
		
		for (int x = (int)original.Position.X; x <= (int)original.Position.X + (int)original.Size.X; x++)
		{
			for (int y = (int)original.Position.Y; y <= (int)original.Position.Y + (int)original.Size.Y; y++)
			{
				for (int z = (int)original.Position.Z; z <= (int)original.Position.Z + (int)original.Size.Z; z++)
				{
					final.Add(new Vector3I(x,y,z));
				}
			}
		}
		
		return final;
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
