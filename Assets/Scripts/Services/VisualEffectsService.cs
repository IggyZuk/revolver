using UnityEngine;

public static class VisualEffectsService
{
    public static void AddExplosion(WorldView view, Vector3 pos)
    {
        GameObject go = Object.Instantiate(Resources.Load<GameObject>("Prefabs/VisualEffects/Explosion"));
        go.transform.position = pos;
        go.transform.SetParent(view.root.transform);
    }
}
