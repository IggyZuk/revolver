using UnityEngine;

public static class ImmediateRenderer
{
    public static void Render(World model, InputController input)
    {
        DrawWorld(model);
        DrawBulletPrediction(model, input);
        DrawRevolverMagazine(model);
        DrawInput(input.GetModel());

        IMDraw.Line3D(Vector3.left * 10f, Vector3.right * 10f, new Color(0.2f, 0.2f, 0.2f));
        IMDraw.Line3D(Vector3.forward * 20f, Vector3.back * 20f, new Color(0.2f, 0.2f, 0.2f));
    }

    public static void DrawWorld(World model)
    {
        IMDraw.Sphere3D(model.player.pos.Vector3(), model.player.radius, Color.green);
        IMDraw.Line3D(model.player.pos.Vector3(), model.player.pos.Vector3() + model.player.dir.Vector3(), Color.green);

        foreach (var b in model.bandits)
        {
            Color c = Color.Lerp(Color.white, Color.red, 1f - (float)b.turnsTillShoot / Config.DEFAULT_BANDIT_TURNS);
            IMDraw.Sphere3D(b.pos.Vector3(), b.radius, c);
            IMDraw.Line3D(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3(), c);
            IMDraw.LabelShadowed(b.pos.Vector3(), Color.white, LabelPivot.MIDDLE_CENTER, LabelAlignment.CENTER, b.turnsTillShoot.ToString());
        }

        foreach (var b in model.bullets)
        {
            IMDraw.Sphere3D(b.pos.Vector3(), b.radius, Color.blue);
            IMDraw.Line3D(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3(), Color.blue);
        }

        foreach (var g in model.gizmos)
        {
            IMDraw.Sphere3D(g.Vector3(), 0.1f, Color.black);
        }
        model.gizmos.Clear();
    }

    public static void DrawBulletPrediction(World model, InputController input)
    {
        World clone = LogicService.CloneWorldWithoutBullets(model);

        LogicService.ShootBullet(
            clone,
            input.GetShootDir()
        );

        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
            for (int j = 0; j < 1; j++)
            {
                LogicService.Tick(clone);
            }

            foreach (var b in clone.bullets)
            {
                Color c = new Color(0f, 0f, 1f, 1f - (float)i / steps);
                IMDraw.WireSphere3D(b.pos.Vector3(), b.radius, c);
                IMDraw.Line3D(b.pos.Vector3(), b.pos.Vector3() + (b.dir * b.speed).Vector3(), c);
            }
            foreach (var g in clone.gizmos)
            {
                IMDraw.Sphere3D(g.Vector3(), 0.1f, Color.black);
            }
        }
    }

    public static void DrawRevolverMagazine(World model)
    {
        float radius = 1f;
        for (float i = 0; i < Config.MAGAZINE_SIZE; i++)
        {
            float angle = (i / Config.MAGAZINE_SIZE) * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Color c = i < model.player.revolver.bullets ? Color.white : Color.black;
            IMDraw.Sphere3D(pos, 0.1f, c);
        }

        IMDraw.LabelShadowed(
            model.player.pos.Vector3(),
            Color.white,
            LabelPivot.MIDDLE_CENTER,
            LabelAlignment.CENTER,
            model.player.revolver.bullets.ToString() + "/" + Config.MAGAZINE_SIZE.ToString());
    }

    public static void DrawInput(InputController.InputModel inputModel)
    {
        if (inputModel.isDragging)
        {
            var p1 = Camera.main.ScreenToWorldPoint(inputModel.startDragPos);
            p1.y = 0f;
            var p2 = Camera.main.ScreenToWorldPoint(inputModel.currentDragPos);
            p2.y = 0f;

            IMDraw.Line3D(p1, p2, Color.black);

            IMDraw.Sphere3D(p1, 1f, Color.black);
        }
    }
}