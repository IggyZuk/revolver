public class BulletAddedEvent : BaseEvent
{
    public int id;

    public BulletAddedEvent(int id)
    {
        this.id = id;
    }

    public void Execute(World model, WorldView view)
    {
        Bullet bullet = model.bullets[id];
        view.bulletViews.Add(id, ViewService.CreateBulletView(bullet));
    }
}
