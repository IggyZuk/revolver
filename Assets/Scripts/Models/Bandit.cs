public class Bandit
{
	public bool isActive = true;
	public Position pos = new Position();
	public Position dir = new Position(0, 1);
	public int tickLife = 1000;
	public float radius = 0.5f;
	public float speed = 1f;
}