public class Rectangle
{
    public Vector pos = new Vector();
    public Vector size = new Vector();

    public Rectangle(Vector pos, Vector size)
    {
        this.pos = pos;
        this.size = size;
    }

    public Rectangle(int x, int y, int width, int height) : this((float)x, (float)y, (float)width, (float)height) { }

    public Rectangle(float x, float y, float width, float height)
    {
        this.pos = new Vector(x, y);
        this.size = new Vector(width, height);
    }
}
