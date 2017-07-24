using UnityEngine;

public class WorldController : MonoBehaviour
{
    World model = new World();
    WorldView view = new WorldView();

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            LogicService.ShootBullet(
                model,
                new Position(worldPos.x, worldPos.z)
            );
        }
        if (Input.GetMouseButtonUp(1))
        {
            LogicService.ShootSuperBullet(model);
        }
    }

    void FixedUpdate()
    {
        LogicService.Tick(model);
        ViewService.Update(model, view);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(model.player.pos.Vector3(), model.player.radius);
        Gizmos.DrawLine(model.player.pos.Vector3(), model.player.pos.Vector3() + model.player.dir.Vector3());

        foreach (var b in model.bandits)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(b.pos.Vector3(), b.radius);
            Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
        }

        foreach (var b in model.bullets)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(b.pos.Vector3(), b.radius);
            Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
        }

        foreach (var g in model.gizmos)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(g.Vector3(), 0.1f);
        }
        model.gizmos.Clear();

        World clone = LogicService.CloneWorldWithoutBullets(model);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        LogicService.ShootBullet(
            clone,
            new Position(worldPos.x, worldPos.z)
        );
        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                LogicService.Tick(clone);
            }

			foreach (var b in clone.bullets)
			{
				Gizmos.color = new Color(0f, 0f, 1f, 1f - (float)i / steps);
				Gizmos.DrawWireSphere(b.pos.Vector3(), b.radius);
				Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
			}
        }
    }
}
