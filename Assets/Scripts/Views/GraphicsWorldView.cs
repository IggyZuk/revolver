using UnityEngine;

public class GraphicsWorldView : MonoBehaviour
{
    public void Tick(World model)
    {
        IMDraw.Axis3D(Vector3.zero, Quaternion.identity, 1000f, 0.1f);
        IMDraw.Grid3D(Vector3.zero, Quaternion.identity, 75f, 75f, 32, 32, new Color(1f, 1f, 1f, 0.5f));
        IMDraw.Bounds(
            new Bounds(
                new Vector3(0f, 0f),
                new Vector3(
                    Mathf.Abs(model.bounds.pos.x) + model.bounds.size.x,
                    0f,
                    Mathf.Abs(model.bounds.pos.y) + model.bounds.size.y
                )
            ),
            Color.red
        );

        foreach (var b in model.bandits.Values)
        {
            GraphicsService.DrawCyllinder(b.pos.Vector3(), 1f, b.radius, Color.Lerp(Color.white, Color.red, b.hp / 5f));
            GraphicsService.DrawLabel(b.pos.Vector3(), Color.white, b.hp.ToString());
        }

        foreach (var b in model.bullets.Values)
        {
            GraphicsService.DrawLine3D(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3(), b.radius, Color.blue);
            GraphicsService.DrawLabel(b.pos.Vector3(), Color.white, b.ricochetLifeHits.ToString());
        }

        GraphicsService.DrawCube(model.player.pos.Vector3(), Vector3.one * model.player.radius, Color.green);
        GraphicsService.DrawLabel(model.player.pos.Vector3(), Color.green, "PLAYER" + "\n" + model.player.pos);

        Vector prev = model.player.pos;
        foreach (var p in model.predictionPoints)
        {
            GraphicsService.DrawLine(prev.Vector3(), p.Vector3(), Color.white);
            prev = p;
        }
    }
}
