﻿public class Bullet
{
	public bool isActive = true;
	public Position pos = new Position();
	public Position dir = new Position(0, 1);
	public int tickLife = Config.BULLET_LIFE_TICKS;
	public float radius = 0.4f;
	public float speed = 0.8f;
	public int hits = 0;
}