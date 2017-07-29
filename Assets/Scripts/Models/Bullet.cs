public class Bullet
{
    public int id = -1;
	public bool isActive = true;
	public Position pos = new Position();
	public Position dir = new Position(0, 1);
    public int ricochetLifeHits = 10;
	public float radius = 0.4f;
	public float speed = 0.6f;
	public int hits = 0;
}