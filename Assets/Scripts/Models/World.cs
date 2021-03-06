﻿using System.Collections.Generic;

public class World
{
	public int tickNum = 0;
	public int turnNum = 0;
	public GameState gameState = GameState.EnemyTurn;
	public Player player = new Player();
	public List<Bandit> bandits = new List<Bandit>();
	public List<Bullet> bullets = new List<Bullet>();
	public int nextBanditSpawnTick = 0;
	public int bulletHitsScore = 0;
	public List<Position> gizmos = new List<Position>();
	public Position wind = new Position(0.01f, 0.01f);
}