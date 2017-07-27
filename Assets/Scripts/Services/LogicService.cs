public static class LogicService
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
				model.wind = new Position(
					UnityEngine.Random.Range(-Config.WIND_STRENGTH, Config.WIND_STRENGTH),
					UnityEngine.Random.Range(-Config.WIND_STRENGTH, Config.WIND_STRENGTH)
				);

				break;
			case GameState.GameOver:
				model.player.revolver.bullets = Config.MAGAZINE_SIZE;
				model.bandits.Clear();
				model.bandits.Clear();
				model.gameState = GameState.PlayerTurn;
				break;
		}
	}

	public static void TickBandits(World model)
	{
		GameState state = GameState.PlayerTurn;

		for (int i = 0; i < Config.SPAWN_BANDIT_COUNT; i++)
		{
			SpawnBandit(model);
		}

		foreach (var bandit in model.bandits)
		{
			bandit.pos += bandit.dir * bandit.speed;
			bandit.turnsTillShoot--;
			if (bandit.turnsTillShoot <= 0)
			{
				bandit.isActive = false;
				state = GameState.GameOver;
			}

			foreach (var otherBandit in model.bandits)
			{
				if (bandit != otherBandit)
				{
					float maxDistSq = (float)System.Math.Pow(bandit.radius + otherBandit.radius, 2);
					Position dir = bandit.pos - otherBandit.pos;
					float distSq = dir.MagnitudeSq();
					if (distSq < maxDistSq)
					{
						bandit.pos += dir * bandit.radius;
						otherBandit.pos -= dir * otherBandit.radius;
					}
				}
			}
		}

		model.gameState = state;
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
			bullet.dir += model.wind;
			bullet.pos += bullet.dir * bullet.speed;

			bullet.tickLife--;
			if (bullet.tickLife <= 0) bullet.isActive = false;

			foreach (var bandit in model.bandits)
			{
				Circle bulletCircle = CollisionService.CreateCircle(bullet.pos, bullet.dir * bullet.radius, bullet.radius);
				Circle banditCircle = CollisionService.CreateCircle(bandit.pos, new Position(), bandit.radius);

				Position hitPoint;
				if (CollisionService.DynamicToStaticCircleCollision(bulletCircle, banditCircle, out hitPoint))
				{
					model.gizmos.Add(hitPoint);

					Position delta = hitPoint - bandit.pos;
					Position normal = delta.Normalize();

					bullet.pos = hitPoint;
					bullet.dir = (bullet.dir * 0.5f + normal * 0.75f).Normalize();
					bullet.tickLife = Config.BULLET_LIFE_TICKS;
					bullet.hits++;

					bandit.isActive = false;
				}
			}
		}
	}

	public static void ShootBullet(World model, Position dir)
	{
		if (model.player.revolver.bullets > 0)
		{
			model.player.revolver.bullets--;

			model.player.dir = dir;

			Bullet b = new Bullet();
			b.pos = model.player.pos;
			b.dir = model.player.dir;
			model.bullets.Add(b);

			model.gameState = GameState.Animation;
		}
		else
		{
			model.player.revolver.bullets = Config.MAGAZINE_SIZE;

			model.gameState = GameState.EnemyTurn;
		}
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
		clone.wind = original.wind;

		foreach (var originalBandit in original.bandits)
		{
			Bandit b = new Bandit();
			b.isActive = originalBandit.isActive;
			b.pos = originalBandit.pos;
			b.dir = originalBandit.dir;
			b.turnsTillShoot = originalBandit.turnsTillShoot;
			clone.bandits.Add(b);
		}

		return clone;
	}
}
