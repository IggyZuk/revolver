public class Bandit
{
    public int id = -1;
	public bool isActive = true;
	public Position pos = new Position();
	public Position dir = new Position(0, 1);
	public float speed = 1.5f;
	public float radius = 1.15f;
    public float distance = float.MaxValue;
    public int hp = 3;
}