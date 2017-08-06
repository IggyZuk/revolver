using UnityEngine;

public struct Vector
{
    public float x;
    public float y;

    public Vector(float x = 0f, float y = 0f)
    {
        this.x = x;
        this.y = y;
    }

    public Vector(Vector2 v) : this(v.x, v.y)
    {
    }

    public Vector(Vector3 v) : this(v.x, v.z)
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

    public Vector Normalize()
    {
        float m = Magnitude();
        if (m > 0f) return new Vector(x / m, y / m);
        return new Vector();
    }

    public float Magnitude()
    {
        return Mathf.Sqrt(x * x + y * y);
    }

    public float MagnitudeSq()
    {
        return x * x + y * y;
    }

    public static float Distance(Vector a, Vector b)
    {
        return (b - a).Magnitude();
    }

    public static Vector Project(Vector a, Vector b)
    {
        Vector bNorm = b.Normalize();
        return bNorm * Dot(a, bNorm);
    }

    public static Vector RotateLeft(Vector a)
    {
        return new Vector(-a.y, a.x);
    }

    public static Vector RotateRight(Vector a)
    {
        return new Vector(a.y, -a.x);
    }

    public static float Dot(Vector a, Vector b)
    {
        return a.x * b.x + a.y * b.y;
    }

    public static Vector operator +(Vector left, Vector right)
    {
        return new Vector(left.x + right.x, left.y + right.y);
    }

    public static Vector operator -(Vector left, Vector right)
    {
        return new Vector(left.x - right.x, left.y - right.y);
    }

    public static Vector operator *(Vector left, float right)
    {
        return new Vector(left.x * right, left.y * right);
    }

    public static Vector operator /(Vector left, float right)
    {
        return new Vector(left.x / right, left.y / right);
    }

    public static Vector operator -(Vector self)
    {
        return new Vector(-self.x, -self.y);
    }

    public static Vector Lerp(Vector a, Vector b, float f)
    {
        return new Vector(Mathf.Lerp(a.x, b.x, f), Mathf.Lerp(a.y, b.y, f));
    }

    public override string ToString()
    {
        return string.Format("x:{0}, y:{1}", x, y);
    }
}
