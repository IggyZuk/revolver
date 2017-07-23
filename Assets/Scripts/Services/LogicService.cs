public static class LogicService
{
    public static void Tick(World model)
    {
        foreach (var b in model.bullets)
        {
            b.pos += b.dir * 0.1f;
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
}
