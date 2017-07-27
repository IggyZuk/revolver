using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    World model;
    WorldView view;

    InputController input = new InputController();
    InputController.InputModel inputModel;

    void Awake()
    {
        model = new World();
        view = ViewService.CreateWorldView();

        inputModel = input.GetModel();

        inputModel.shootAction += (touchUpPos) =>
        {
            if (model.gameState == GameState.PlayerTurn)
            {
                if (inputModel.distance > 1f)
                {
                    LogicService.ShootBullet(
                        model,
                        input.GetShootDir()
                    );
                }
            }
        };
    }

    void Update()
    {
        input.Update();

        if (inputModel.isDragging)
        {
            World clone = LogicService.CloneWorldWithoutBullets(model);

            LogicService.ShootBullet(
                clone,
                input.GetShootDir()
            );

            List<Position> predictionPoints = new List<Position>();
            for (int i = 0; i < Config.PREDICTION_STEPS; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    LogicService.Tick(clone);
                }

                foreach (var b in clone.bullets.Values)
                {
                    predictionPoints.Add(b.pos);
                }
            }

            ViewService.DrawPredictionPoints(view, predictionPoints);
        }
    }

    void FixedUpdate()
    {
        LogicService.Tick(model);
        ViewService.Tick(model, view);
    }

    void OnGUI()
    {
        GUI.Label(
            new Rect(0, 0, 100, 100),
            string.Format("{0}/{1}", model.player.revolver.bullets, Config.MAGAZINE_SIZE)
        );

        GUI.Label(
            new Rect(Screen.width - 100, 0, 100, 100),
            string.Format("Highscore: {0}", model.bulletHitsScore)
        );
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        DrawWorld();
        DrawBulletPrediction();
        DrawRevolverMagazine();
        DrawInput();
    }

    void DrawWorld()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(model.player.pos.Vector3(), model.player.radius);
        Gizmos.DrawLine(model.player.pos.Vector3(), model.player.pos.Vector3() + model.player.dir.Vector3());

        foreach (var b in model.bandits.Values)
        {
            Gizmos.color = Color.Lerp(Color.white, Color.red, 1f - (float)b.turnsTillShoot / Config.DEFAULT_BANDIT_TURNS);
            Gizmos.DrawSphere(b.pos.Vector3(), b.radius);
            Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + b.dir.Vector3());
        }

        foreach (var b in model.bullets.Values)
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
    }

    void DrawBulletPrediction()
    {
        World clone = LogicService.CloneWorldWithoutBullets(model);

        LogicService.ShootBullet(
            clone,
            input.GetShootDir()
        );

        int steps = 25;
        for (int i = 0; i < steps; i++)
        {
            for (int j = 0; j < 1; j++)
            {
                LogicService.Tick(clone);
            }

            foreach (var b in clone.bullets.Values)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 1f - (float)i / steps);
                Gizmos.DrawWireSphere(b.pos.Vector3(), b.radius);
                Gizmos.DrawLine(b.pos.Vector3(), b.pos.Vector3() + (b.dir * b.speed).Vector3());
            }
            foreach (var g in clone.gizmos)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(g.Vector3(), 0.1f);
            }
        }
    }

    void DrawRevolverMagazine()
    {
        float radius = 1f;
        for (float i = 0; i < Config.MAGAZINE_SIZE; i++)
        {
            float angle = (i / Config.MAGAZINE_SIZE) * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.color = i < model.player.revolver.bullets ? Color.white : Color.black;
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }

    void DrawInput()
    {
        if (inputModel.isDragging)
        {
            var p1 = Camera.main.ScreenToWorldPoint(inputModel.startDragPos);
            p1.y = 0;
            var p2 = Camera.main.ScreenToWorldPoint(inputModel.currentDragPos);
            p2.y = 0;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p1, p2);

            Gizmos.DrawSphere(p1, 1f);
        }
    }
}
