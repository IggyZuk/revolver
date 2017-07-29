using UnityEngine;

public class BulletRemovedEvent : BaseEvent
{
    public int id;

    public BulletRemovedEvent(int id) {
        this.id = id;    
    }

    public void Execute(World model, WorldView view)
    {
        BulletView bulletView = ViewService.GetBulletViewWithId(view, id);
        if (bulletView != null)
        {
            Object.Destroy(bulletView.gameObject);
            view.bulletViews.Remove(id);
            VisualEffectsService.AddExplosion(view, bulletView.transform.position);
        }
        AudioController.Instance.PlaySound(AudioController.Sound.BulletDisappear);
    }
}
