public class Bandit
{
	public bool isActive = true;
	public Position pos = new Position();
	public Position dir = new Position(0, 1);
	public int turnsTillShoot = Config.DEFAULT_BANDIT_TURNS;
	public float speed = 1f;
	public float radius = 0.6f;
}