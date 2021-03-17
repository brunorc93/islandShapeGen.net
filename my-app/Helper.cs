using System.Collections.Generic;
using System;
using static System.Math;
using static Structs;

public static class Helper 
{
  
    public static Vector2Int[] Neighbours(this Vector2Int point, int size)
    {
      Vector2Int[] neighbours = new Vector2Int[8]
      {
        new Vector2Int(Min(point.x+1,size-1),point.y),                // + 0
        new Vector2Int(Max(point.x-1,0),point.y),                     // - 0
        new Vector2Int(point.x,Min(point.y+1,size-1)),                // 0 +
        new Vector2Int(point.x,Max(point.y-1,0)),                     // 0 -
        new Vector2Int(Min(point.x+1,size-1),Min(point.y+1,size-1)),  // + +
        new Vector2Int(Max(point.x-1,0),Max(point.y-1,0)),            // - -
        new Vector2Int(Min(point.x+1,size-1),Max(point.y-1,0)),       // + -
        new Vector2Int(Max(point.x-1,0),Min(point.y+1,size-1))        // - +
      };
      return neighbours;
    }

    public static T[] Shuffle<T>(this T[] array, Random rnd)
    {
      int n = array.Length;
      while (n>1)
      {
        int k = rnd.Next(0,n--);
        T temp = array[n];
        array[n] = array[k];
        array[k] = temp;
      }
      return array;
    }

    public static List<T> Shuffle<T>(this List<T> list, Random rnd)
    {
      int n = list.Count;
      while (n>1)
      {
        int k = rnd.Next(0,n--);
        T temp = list[n];
        list[n] = list[k];
        list[k] = temp;
      }
      return list;
    }
}