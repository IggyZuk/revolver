using UnityEngine;

public struct Position
{
    public float x;
    public float y;

    public Position(float x = 0f, float y = 0f)
    {
        this.x = x;
        this.y = y;
    }

    public Position(Vector2 v) : this(v.x, v.y)
    {
    }

    public Position(Vector3 v) : this(v.x, v.z)
    {
    }

    public Vector2 Vector2()
    {
        return new Vector2(x, y);
    }

    public Vector3 Vector3()
    {
        return new Vector3(x, 0f, y);
    }

    public Position Normalize()
    {
        float m = Magnitude();
        if (m > 0f) return new Position(x / m, y / m);
        return new Position();
    }

    public float Magnitude()
    {
        return Mathf.Sqrt(x * x + y * y);
    }

    public float MagnitudeSq()
    {
        return x * x + y * y;
    }

    public static float Distance(Position a, Position b)
    {
        return (b - a).Magnitude();
    }

    public static Position Project(Position a, Position b)
    {
        Position bNorm = b.Normalize();
        return bNorm * Dot(a, bNorm);
    }

    public static Position RotateLeft(Position a)
    {
        return new Position(-a.y, a.x);
    }

    public static Position RotateRight(Position a)
    {
        return new Position(a.y, -a.x);
    }

    public static float Dot(Position a, Position b)
    {
        return a.x * b.x + a.y * b.y;
    }

    public static Position operator +(Position left, Position right)
    {
        return new Position(left.x + right.x, left.y + right.y);
    }

    public static Position operator -(Position left, Position right)
    {
        return new Position(left.x - right.x, left.y - right.y);
    }

    public static Position operator *(Position left, float right)
    {
        return new Position(left.x * right, left.y * right);
    }

    public static Position operator /(Position left, float right)
    {
        return new Position(left.x / right, left.y / right);
    }

    public static Position Lerp(Position a, Position b, float f)
    {
        return new Position(Mathf.Lerp(a.x, b.x, f), Mathf.Lerp(a.y, b.y, f));
    }

    public override string ToString()
    {
        return string.Format("Position(x: {0}, y: {1})", x, y);
    }
}
