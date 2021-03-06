using System;

public static class CollisionService
{
	public static Position ClosestPointOnLine(Position from, Position to, Position point)
	{
		float A1 = to.y - from.y;
		float B1 = from.x - to.x;

		double C1 = (to.y - from.y) * from.x + (from.x - to.x) * from.y;
		double C2 = -B1 * point.x + A1 * point.y;

		double det = A1 * A1 - -B1 * B1;

		Position result = new Position();

		if (det > float.Epsilon)
		{
			result.x = (float)((A1 * C1 - B1 * C2) / det);
			result.y = (float)((A1 * C2 - -B1 * C1) / det);
		}
		else
		{
			result = point;
		}

		return result;
	}

	public static bool DynamicToStaticCircleCollision(Circle circle1, Circle circle2, out Position hit)
	{
		double maxDistSq = Math.Pow(circle1.radius + circle2.radius, 2);

		if ((circle1.pos + circle1.vel - circle2.pos).MagnitudeSq() > maxDistSq) {
			hit = new Position();
			return false;
		}

		Position d = ClosestPointOnLine(
			circle1.pos,
			new Position(circle1.pos.x + circle1.vel.x, circle1.pos.y + circle1.vel.y),
			circle2.pos
		);

		double closestDistSq = Math.Pow(circle2.pos.x - d.x, 2) + Math.Pow(circle2.pos.y - d.y, 2);

		if (closestDistSq <= maxDistSq)
		{
			double backDist = Math.Sqrt(maxDistSq - closestDistSq);
			double movementVectorLength = Math.Sqrt(Math.Pow(circle1.vel.x, 2) + Math.Pow(circle1.vel.y, 2));
			double finalX = d.x - backDist * (circle1.vel.x / movementVectorLength);
			double finalY = d.y - backDist * (circle1.vel.y / movementVectorLength);

			hit = new Position((float)finalX, (float)finalY);
			return true;
		}

		hit = new Position();
		return false;
	}

	public static Circle CreateCircle(Position pos, Position vel, float radius)
	{
		return new Circle
		{
			pos = pos,
			vel = vel,
			radius = radius
		};
	}
}