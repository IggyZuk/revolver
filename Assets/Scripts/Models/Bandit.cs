public class Bandit
{
    public int id = -1;
	public bool isActive = true;
	public Vector pos = new Vector();
	public Vector dir = new Vector(0, 1);
	public float speed = 1.5f;
	public float radius = 1.15f;
    public float distance = float.MaxValue;
    public int hp = 5;
}