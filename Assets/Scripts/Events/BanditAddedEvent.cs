public class BanditAddedEvent : BaseEvent
{
    public int id;

    public BanditAddedEvent(int id) {
        this.id = id;    
    }

    public void Execute(World model, WorldView view)
    {
        Bandit bandit = model.bandits[id];
        view.banditViews.Add(id, ViewService.CreateBanditView(bandit));
    }
}
