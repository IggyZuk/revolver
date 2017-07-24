using UnityEngine;

public class WorldController : MonoBehaviour
{
    World model = new World();
    WorldView view = new WorldView();

    void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            LogicService.ShootBullet(
                model,
                new Position(worldPos.x, worldPos.z)
            );
        }

        LogicService.Tick(model);
        ViewService.Update(model, view);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(model.player.pos.Vector3(), 0.25f);
        Gizmos.DrawLine(model.player.pos.Vector3(), model.player.pos.Vector3() + model.player.dir.Vector3());

        foreach (var b in model.bandits)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(b.pos.Vector3(), 0.25f);
            Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
        }

        foreach (var b in model.bullets)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(b.pos.Vector3(), 0.1f);
            Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
        }
    }
}
