﻿public class Bullet
{
	public bool isActive = true;
    public Position pos = new Position();
    public Position dir = new Position(0, 1);
    public int tickLife = 500;
    public float radius = 0.4f;
    public float speed = 0.33f;
}