using UnityEngine;

public class GameOverEvent : BaseEvent
{
    public void Execute(World model, WorldView view)
    {
        Object.Destroy(view.predictionRoot);

        Object.Destroy(view.playerView.gameObject);

        foreach (BulletView bulletView in view.bulletViews.Values)
        {
            Object.Destroy(bulletView.gameObject);
        }
        view.bulletViews.Clear();

        foreach (var banditView in view.banditViews.Values)
        {
            Object.Destroy(banditView.gameObject);
        }
        view.banditViews.Clear();
    }
}
