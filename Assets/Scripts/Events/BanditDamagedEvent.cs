public class BanditDamagedEvent : BaseEvent
{
    public int id;

    public BanditDamagedEvent(int id)
    {
        this.id = id;
    }

    public void Execute(World model, WorldView view)
    {
        if (model.bandits.ContainsKey(id))
        {
            Bandit bandit = model.bandits[id];
            BanditView banditView = ViewService.GetBanditViewWithId(view, id);
            if (banditView != null)
            {
                banditView.UpdateHP(bandit.hp);
            }
            AudioController.Instance.PlaySound(AudioController.Sound.DamageBandit);
        }
    }
}
