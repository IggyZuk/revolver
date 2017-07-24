using System.Collections.Generic;

public class World
{
    public int tickNum = 0;
    public Player player = new Player();
    public List<Bandit> bandits = new List<Bandit>();
    public List<Bullet> bullets = new List<Bullet>();
    public int nextBanditSpawnTick = 0;
}