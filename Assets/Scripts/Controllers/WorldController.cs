using UnityEngine;

public class WorldController : MonoBehaviour
{
	World model = new World();
	WorldView view = new WorldView();

	void Update()
	{
		if (model.gameState == GameState.PlayerTurn)
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
	}

	void FixedUpdate()
	{
		LogicService.Tick(model);
		ViewService.Update(model, view);
	}

	void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		DrawWorld();
		DrawBulletPrediction();
		DrawRevolverMagazine();
	}

	void DrawWorld()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(model.player.pos.Vector3(), model.player.radius);
		Gizmos.DrawLine(model.player.pos.Vector3(), model.player.pos.Vector3() + model.player.dir.Vector3());

		foreach (var b in model.bandits)
		{
			Gizmos.color = Color.Lerp(Color.white, Color.red, 1f - (float)b.turnsTillShoot / Config.DEFAULT_BANDIT_TURNS);
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
	}

	void DrawBulletPrediction()
	{
		World clone = LogicService.CloneWorldWithoutBullets(model);
		Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		LogicService.ShootBullet(
			clone,
			new Position(worldPos.x, worldPos.z)
		);
		int steps = 25;
		for (int i = 0; i < steps; i++)
		{
			for (int j = 0; j < 1; j++)
			{
				LogicService.Tick(clone);
			}

			foreach (var b in clone.bullets)
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
		Vector2 offset = new Vector2(2, 2);
		float radius = 1f;
		for (float i = 0; i < Config.MAGAZINE_SIZE; i++)
		{
			float angle = (i / Config.MAGAZINE_SIZE) * Mathf.PI * 2f;
			Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
			Gizmos.color = i < model.player.revolver.bullets ? Color.white : Color.black;
			Gizmos.DrawSphere(pos, 0.1f);
		}
	}
}
