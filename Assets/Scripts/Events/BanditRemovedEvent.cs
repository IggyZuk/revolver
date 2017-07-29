using UnityEngine;

public class BanditRemovedEvent : BaseEvent
{
    public int id;

    public BanditRemovedEvent(int id)
    {
        this.id = id;
    }

    public void Execute(World model, WorldView view)
    {
        BanditView banditView = ViewService.GetBanditViewWithId(view, id);
        if (banditView != null)
        {
            Object.Destroy(banditView.gameObject);
            view.banditViews.Remove(id);
            VisualEffectsService.AddExplosion(view, banditView.transform.position);
            AudioController.Instance.PlaySound(AudioController.Sound.BanditDeath);
        }
    }
}
