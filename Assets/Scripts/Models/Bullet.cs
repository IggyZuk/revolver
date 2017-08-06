public class Bullet
{
    public int id = -1;
	public bool isActive = true;
	public Vector pos = new Vector();
	public Vector dir = new Vector(0, 1);
    public int ricochetLifeHits = 10;
	public float radius = 0.4f;
	public float speed = 0.6f;
	public int hits = 0;
}