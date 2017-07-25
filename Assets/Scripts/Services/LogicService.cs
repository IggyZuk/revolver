﻿public static class LogicService
{
	public static void Tick(World model)
	{
		model.tickNum++;

		switch (model.gameState)
		{
			case GameState.PlayerTurn:
			case GameState.Animation:
				TickBullets(model);
				RemoveBullets(model);
				RemoveBandits(model);
				break;

			case GameState.EnemyTurn:
				TickBandits(model);
				model.gameState = GameState.PlayerTurn;
				break;
		}
	}

	public static void TickBandits(World model)
	{
		if (model.tickNum >= model.nextBanditSpawnTick)
		{
			model.nextBanditSpawnTick = model.tickNum + 2;

			for (int i = 0; i < 5; i++)
			{
				SpawnBandit(model);
			}
		}

		foreach (var bandit in model.bandits)
		{
			bandit.pos += bandit.dir * bandit.speed;
			bandit.tickLife--;
			if (bandit.tickLife <= 0) bandit.isActive = false;
		}
	}

	public static void SpawnBandit(World model)
	{
		Bandit b = new Bandit();
		b.pos = new Position(UnityEngine.Random.insideUnitCircle.normalized * 10);
		b.dir = (model.player.pos - b.pos).Normalize();
		model.bandits.Add(b);
	}

	public static void TickBullets(World model)
	{
		foreach (var bullet in model.bullets)
		{
			bullet.pos += bullet.dir * bullet.speed;
			bullet.tickLife--;
			if (bullet.tickLife <= 0) bullet.isActive = false;

			foreach (var bandit in model.bandits)
			{
				Position delta = bullet.pos - bandit.pos;
				float allowedDistance = bullet.radius + bandit.radius;
				float distance = delta.Magnitude();
				if (distance < allowedDistance)
				{
					bandit.isActive = false;

					Position normal = delta.Normalize();
					Position hitPoint = bandit.pos + normal * allowedDistance;
					model.gizmos.Add(hitPoint);

					bullet.pos = hitPoint;
					bullet.dir = (bullet.dir * 0.5f + delta.Normalize() * 0.75f).Normalize();
					bullet.tickLife = Config.BULLET_LIFE_TICKS;

					bullet.hits++;
				}
			}
		}
	}

	public static void ShootBullet(World model, Position clickedWorldPos)
	{
		model.player.dir = (clickedWorldPos - model.player.pos).Normalize();

		Bullet b = new Bullet();
		b.pos = model.player.pos;
		b.dir = model.player.dir;
		model.bullets.Add(b);

		model.gameState = GameState.Animation;
	}

	public static void ShootSuperBullet(World model)
	{
		foreach (var bandit in model.bandits)
		{
			ShootBullet(model, bandit.pos);
		}
	}

	public static void RemoveBullets(World model)
	{
		for (int i = model.bullets.Count - 1; i >= 0; i--)
		{
			Bullet b = model.bullets[i];
			if (!b.isActive)
			{
				if (b.hits > model.bulletHitsScore)
				{
					model.bulletHitsScore = b.hits;
					Logger.Log("New Score: " + model.bulletHitsScore);
				}
				model.bullets.Remove(b);
				model.gameState = GameState.EnemyTurn;
			}
		}
	}

	public static void RemoveBandits(World model)
	{
		for (int i = model.bandits.Count - 1; i >= 0; i--)
		{
			Bandit b = model.bandits[i];
			if (!b.isActive) model.bandits.Remove(b);
		}
	}

	public static World CloneWorldWithoutBullets(World original)
	{
		World clone = new World();
		clone.tickNum = original.tickNum;
		clone.player.pos = original.player.pos;
		clone.player.dir = original.player.dir;
		clone.nextBanditSpawnTick = original.nextBanditSpawnTick;

		foreach (var originalBandit in original.bandits)
		{
			Bandit b = new Bandit();
			b.isActive = originalBandit.isActive;
			b.pos = originalBandit.pos;
			b.dir = originalBandit.dir;
			b.tickLife = originalBandit.tickLife;
			clone.bandits.Add(b);
		}

		return clone;
	}
}
