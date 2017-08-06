using System.Collections.Generic;
using UnityEngine;

public static class ViewService
{
    public static WorldView CreateWorldView()
    {
        WorldView view = new WorldView();
        view.root = new GameObject("WorldView");
        return view;
    }

    public static void Tick(World model, WorldView view)
    {
        while (model.events.Count > 0)
        {
            BaseEvent e = model.events.Dequeue();
            e.Execute(model, view);
        }

        if (model.player != null)
        {
            if (view.playerView == null)
            {
                view.playerView = CreatePlayerView(model.player);
            }
            view.playerView.transform.position = model.player.pos.Vector3();
            view.playerView.transform.forward = Vector3.Lerp(view.playerView.transform.forward, model.player.dir.Vector3(), Time.deltaTime * Config.VIEW_LERP);
        }

        foreach (BulletView bulletView in view.bulletViews.Values)
        {
            Bullet bullet = model.bullets[bulletView.id];
            bulletView.transform.position = Vector3.Lerp(bulletView.transform.position, (bullet.pos + bullet.dir * bullet.speed).Vector3(), Time.deltaTime * Config.VIEW_LERP * 2.5f);
            bulletView.transform.forward = Vector3.Lerp(bulletView.transform.forward, bullet.dir.Vector3(), Time.deltaTime * Config.VIEW_LERP);
        }

        foreach (BanditView banditView in view.banditViews.Values)
        {
            Bandit bandit = model.bandits[banditView.id];
            banditView.transform.position = Vector3.Lerp(banditView.transform.position, bandit.pos.Vector3(), Time.deltaTime * Config.VIEW_LERP * 0.5f);
            banditView.transform.forward = Vector3.Lerp(banditView.transform.forward, bandit.dir.Vector3(), Time.deltaTime * Config.VIEW_LERP);
        }
    }

    public static PlayerView CreatePlayerView(Player player)
    {
        PlayerView playerView = Object.Instantiate(Resources.Load<PlayerView>("Prefabs/Views/PlayerView"));
        playerView.transform.position = player.pos.Vector3();
        playerView.transform.localScale = Vector3.one * player.radius * 2f;
        playerView.transform.forward = player.dir.Vector3();
        return playerView;
    }

    public static BulletView CreateBulletView(Bullet bullet)
    {
        BulletView bulletView = Object.Instantiate(Resources.Load<BulletView>("Prefabs/Views/BulletView"));
        bulletView.id = bullet.id;
        bulletView.transform.position = bullet.pos.Vector3();
        bulletView.transform.localScale = Vector3.one * bullet.radius * 2f;
        bulletView.transform.forward = bullet.dir.Vector3();
        return bulletView;
    }

    public static BanditView CreateBanditView(Bandit bandit)
    {
        BanditView banditView = Object.Instantiate(Resources.Load<BanditView>("Prefabs/Views/BanditView"));
        banditView.id = bandit.id;
        banditView.transform.localScale = Vector3.one * bandit.radius * 2f;
        banditView.transform.position = bandit.pos.Vector3();
        banditView.transform.forward = bandit.dir.Vector3();
        return banditView;
    }

    public static BulletView GetBulletViewWithId(WorldView view, int id)
    {
        if (view.bulletViews.ContainsKey(id))
        {
            return view.bulletViews[id];
        }
        return null;
    }

    public static BanditView GetBanditViewWithId(WorldView view, int id)
    {
        if (view.banditViews.ContainsKey(id))
        {
            return view.banditViews[id];
        }
        return null;
    }

    public static void DrawPredictionPoints(WorldView view, List<Vector> points)
    {
        if (view.predictionRoot != null)
        {
            Object.Destroy(view.predictionRoot);
        }

        view.predictionRoot = new GameObject("PredictionPoints");

        foreach (Vector point in points)
        {
            GameObject predictionGo = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/PredictionView"));
            predictionGo.transform.position = point.Vector3();
            predictionGo.transform.SetParent(view.predictionRoot.transform);
        }
    }

    public static void DrawInputUI(WorldView view, Vector3 startPos, Vector3 endPos)
    {
        if (view.inputRoot != null)
        {
            Object.Destroy(view.inputRoot);
        }

        view.inputRoot = new GameObject("InputRoot");

        GameObject startGo = AddInputView(3f, view.inputRoot.transform);
        startGo.transform.position = startPos;
        GameObject endGo = AddInputView(1f, view.inputRoot.transform);
        endGo.transform.position = endPos;

        float distance = (startPos - endPos).magnitude;
        int steps = (int)(distance * 1f);
        for (int i = 0; i < steps; i++)
        {
            GameObject midGo = AddInputView(0.25f, view.inputRoot.transform);
            midGo.transform.position = Vector3.Lerp(startPos, endPos, (float)i / steps);
        }

    }

    public static GameObject AddInputView(float scale, Transform parent) {
        GameObject go = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/InputView"));
        go.transform.localScale = Vector3.one * scale;
        go.transform.SetParent(parent);
        return go;
    }
}
