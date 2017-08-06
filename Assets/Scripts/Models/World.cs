using System.Collections.Generic;

public class World
{
    public int tickNum = 0;
    public int turnNum = 0;
    public GameState gameState = GameState.EnemyTurn;
    public Player player = new Player();
    public Dictionary<int, Bandit> bandits = new Dictionary<int, Bandit>();
    public int nextBanditId = 0;
    public Dictionary<int, Bullet> bullets = new Dictionary<int, Bullet>();
    public int nextBulletId = 0;
    public int nextBanditSpawnTick = 0;
    public int bulletHitsScore = 0;
    public List<Vector> gizmos = new List<Vector>();
    public Vector wind = new Vector(0.01f, 0.01f);
    public Queue<BaseEvent> events = new Queue<BaseEvent>();
    public List<Vector> predictionPoints = new List<Vector>();
    public float radius = Config.SPAWN_RANGE;
}