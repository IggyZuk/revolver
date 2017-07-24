public static class LogicService
{
    public static void Tick(World model)
    {
        model.tickNum++;
        UnityEngine.Debug.Log(model.tickNum);

        TickBandits(model);
        TickBullets(model);
        RemoveBandits(model);
        RemoveBullets(model);
    }

    public static void TickBandits(World model)
    {
        if (model.tickNum >= model.nextBanditSpawnTick)
        {
            model.nextBanditSpawnTick = model.tickNum + 50;
            SpawnBandit(model);
        }

        foreach (var b in model.bandits)
        {
            b.pos += b.dir * 0.1f;
            b.tickLife--;
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
        foreach (var b in model.bullets)
        {
            b.pos += b.dir * 0.1f;
            b.tickLife--;
        }
    }

    public static void ShootBullet(World model, Position clickedWorldPos)
    {
        model.player.dir = (clickedWorldPos - model.player.pos).Normalize();

        Bullet b = new Bullet();
        b.pos = model.player.pos;
        b.dir = model.player.dir;
        model.bullets.Add(b);
    }

    public static void RemoveBullets(World model)
    {
        for (int i = model.bullets.Count - 1; i >= 0; i--)
        {
            Bullet b = model.bullets[i];
            if (b.tickLife <= 0)
            {
                model.bullets.Remove(b);
            }
        }
    }

    public static void RemoveBandits(World model)
    {
        for (int i = model.bandits.Count - 1; i >= 0; i--)
        {
            Bandit b = model.bandits[i];
            if (b.tickLife <= 0)
            {
                model.bandits.Remove(b);
            }
        }
    }
}
