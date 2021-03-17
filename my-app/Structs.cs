using System;

public class Structs
{
  public struct  Vector2Int
  {
    private int m_X;
    private int m_Y;
    public int x { get { return m_X; } set { m_X = value; } }
    public int y { get { return m_Y; } set { m_Y = value; } }

    public Vector2Int(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public void Set(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public int this[int index]
    {
      get
      {
        switch (index)
        {
          case 0: return x;
          case 1: return y;
          default:
            throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
        }
      }

      set
      {
        switch (index)
        {
          case 0: x = value; break;
          case 1: y = value; break;
          default:
            throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
        }
      }
    }
  }
}