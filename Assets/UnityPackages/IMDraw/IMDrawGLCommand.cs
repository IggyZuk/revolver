using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class IMDrawGLCommand : IMDrawCommand
{
	private static readonly Color COLOR_XAXIS = new Color(1.0f, 0.25f, 0.25f, 1.0f);
	private static readonly Color COLOR_YAXIS = new Color(0.25f, 1.0f, 0.25f, 1.0f);
	private static readonly Color COLOR_ZAXIS = new Color(0.25f, 0.5f, 1.0f, 1.0f);

	public IMDrawCommandType				m_Type;
	public int								m_Verts; // Verts required for this command
	public Vector3							m_P1, m_P2, m_Size;
	public Color							m_C1, m_C2;
	public Quaternion						m_Rotation;
	public IMDrawAxis						m_Axis;

	public Camera							m_Camera; // Used for frustum draw requests
	
	public LinkedListNode<IMDrawGLCommand>	m_ListNode;

	private Vector3[]						m_VertexArray; // Reference to input vertex array
	private Color[]							m_ColorArray; // Reference to input color array

	private static float[]					m_Sine;
	private static float[]					m_Cosine;

	private static float					m_TXx, m_TXy, m_TXz, m_TYx, m_TYy, m_TYz, m_TZx, m_TZy, m_TZz;

	static IMDrawGLCommand ()
	{
		if (m_Sine != null) // Skip if already initialised
			return;

		int step = 15;
		int pcount = (360 / step) + 1;

		int index = 0;

		float anglef;

		m_Sine = new float[pcount];
		m_Cosine = new float[pcount];

		for (int angle = 0; angle <= 360; angle += step)
		{
			anglef = (float)(angle - 90) * Mathf.Deg2Rad;

			m_Sine[index] = Mathf.Sin(anglef);
			m_Cosine[index] = Mathf.Cos(anglef);
			index++;
		}
	}

	public IMDrawGLCommand ()
	{
		m_ListNode = new LinkedListNode<IMDrawGLCommand>(this);
	}

	public IMDrawGLCommand Dispose ()
	{
		m_Camera = null; // Remove reference to camera
		return this;
	}

	public void InitTransform()
	{
		float num = m_Rotation.x * 2.0f;
		float num2 = m_Rotation.y * 2.0f;
		float num3 = m_Rotation.z * 2.0f;

		m_TXx = 1.0f - ((m_Rotation.y * num2) + (m_Rotation.z * num3));
		m_TXy = (m_Rotation.x * num2) + (m_Rotation.w * num3);
		m_TXz = (m_Rotation.x * num3) - (m_Rotation.w * num2);

		m_TYx = (m_Rotation.x * num2) - (m_Rotation.w * num3);
		m_TYy = 1.0f - ((m_Rotation.x * num) + (m_Rotation.z * num3));
		m_TYz = (m_Rotation.y * num3) + (m_Rotation.w * num);

		m_TZx = (m_Rotation.x * num3) + (m_Rotation.w * num2);
		m_TZy = (m_Rotation.y * num3) - (m_Rotation.w * num);
		m_TZz = 1.0f - ((m_Rotation.x * num) + (m_Rotation.y * num2));
	}

	private Vector3 LocalToWorld (ref Vector3 localPos)
	{
		return new Vector3(
			m_P1.x + m_TXx * localPos.x + m_TYx * localPos.y + m_TZx * localPos.z,
			m_P1.y + m_TXy * localPos.x + m_TYy * localPos.y + m_TZy * localPos.z,
			m_P1.z + m_TXz * localPos.x + m_TYz * localPos.y + m_TZz * localPos.z);
	}

	private Vector3 LocalToWorld (Vector3 localPos)
	{
		return new Vector3(
			m_P1.x + m_TXx * localPos.x + m_TYx * localPos.y + m_TZx * localPos.z,
			m_P1.y + m_TXy * localPos.x + m_TYy * localPos.y + m_TZy * localPos.z,
			m_P1.z + m_TXz * localPos.x + m_TYz * localPos.y + m_TZz * localPos.z);
	}

	private Vector3 LocalToWorld (float x, float y, float z)
	{
		return new Vector3(
			m_P1.x + m_TXx * x + m_TYx * y + m_TZx * z,
			m_P1.y + m_TXy * x + m_TYy * y + m_TZy * z,
			m_P1.z + m_TXz * x + m_TYz * y + m_TZz * z);
	}


	private void SizeToDirections(out Vector3 right, out Vector3 up, out Vector3 forward)
	{
		float num = m_Rotation.x * 2.0f;
		float num2 = m_Rotation.y * 2.0f;
		float num3 = m_Rotation.z * 2.0f;

		right.x = m_Size.x * (1.0f - ((m_Rotation.y * num2) + (m_Rotation.z * num3)));
		right.y = m_Size.x * ((m_Rotation.x * num2) + (m_Rotation.w * num3));
		right.z = m_Size.x * ((m_Rotation.x * num3) - (m_Rotation.w * num2));

		up.x = m_Size.y * ((m_Rotation.x * num2) - (m_Rotation.w * num3));
		up.y = m_Size.y * (1.0f - ((m_Rotation.x * num) + (m_Rotation.z * num3)));
		up.z = m_Size.y * ((m_Rotation.y * num3) + (m_Rotation.w * num));

		forward.x = m_Size.z * ((m_Rotation.x * num3) + (m_Rotation.w * num2));
		forward.y = m_Size.z * ((m_Rotation.y * num3) - (m_Rotation.w * num));
		forward.z = m_Size.z * (1.0f - ((m_Rotation.x * num) + (m_Rotation.y * num2)));
	}

	public float GetDistSqrd(ref Vector3 position)
	{
		float dx, dy, dz;

		if (m_Type == IMDrawCommandType.LINE)
		{
			dx = ((m_P1.x + m_P2.x) * 0.5f) - position.x;
			dy = ((m_P1.y + m_P2.y) * 0.5f) - position.y;
			dz = ((m_P1.z + m_P2.z) * 0.5f) - position.z;
		}
		else
		{
			dx = m_P1.x - position.x;
			dy = m_P1.y - position.y;
			dz = m_P1.z - position.z;
		}

		return dx * dx + dy * dy + dz * dz;
	}

	public void SetRotation (ref Quaternion rotation, IMDrawAxis axis)
	{
		switch (axis)
		{
			case IMDrawAxis.X: m_Rotation = rotation * AXIS_X_ROTATION; break;
			//case IMDrawAxis.Y: outRotation = inRotation; break;
			case IMDrawAxis.Z: m_Rotation = rotation * AXIS_Z_ROTATION; break;
			default: m_Rotation = rotation; break;
		}
	}

	public void InitLine (Vector3 p1, Vector3 p2, ref Color color)
	{
		m_Type = IMDrawCommandType.LINE;
		m_Verts = IMDrawVertexCount.LINE;
		m_P1 = p1;
		m_P2 = p2;
		m_C1 = color;
		m_C2 = color;
		m_T = 0f;
    }

	public void InitLine(Vector3 p1, Vector3 p2, ref Color color, float duration)
	{
		m_Type = IMDrawCommandType.LINE;
		m_Verts = IMDrawVertexCount.LINE;
		m_P1 = p1;
		m_P2 = p2;
		m_C1 = color;
		m_C2 = color;
		m_T = duration;
	}

	public void InitLine (Vector3 p1, Vector3 p2, ref Color c1, ref Color c2)
	{
		m_Type = IMDrawCommandType.LINE;
		m_Verts = IMDrawVertexCount.LINE;
		m_P1 = p1;
		m_P2 = p2;
		m_C1 = c1;
		m_C2 = c2;
		m_T = 0f;
	}

	public void InitLine(Vector3 p1, Vector3 p2, ref Color c1, ref Color c2, float duration)
	{
		m_Type = IMDrawCommandType.LINE;
		m_Verts = IMDrawVertexCount.LINE;
		m_P1 = p1;
		m_P2 = p2;
		m_C1 = c1;
		m_C2 = c2;
		m_T = duration;
	}

	public void Draw (Vector3[] vertexArray, Color[] colorArray, ref int vertexIndex)
	{
		m_VertexArray = vertexArray;
		m_ColorArray = colorArray;

		switch (m_Type)
		{
			case IMDrawCommandType.LINE: DrawLine(ref vertexIndex); break;
			case IMDrawCommandType.AXIS: DrawAxis(ref vertexIndex); break;
			case IMDrawCommandType.GRID_SINGLE_COLOR: DrawGridSingleColor(ref vertexIndex); break;
			//case IMDrawCommandType.GRID_DOUBLE_COLOR: DrawGridDoubleColor(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_PYRAMID_ROTATED: DrawPyramid(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_RHOMBUS_ROTATED: DrawRhombus(ref vertexIndex); break;
            case IMDrawCommandType.WIRE_BOX: DrawBox(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_BOX_ROTATED: DrawBoxRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_DISC_ROTATED: DrawDiscRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_SPHERE: DrawSphere(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_SPHERE_ROTATED: DrawSphereRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_ELLIPSOID: DrawEllipsoid(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_ELLIPSOID_ROTATED: DrawEllipsoidRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_CONE_ROTATED: DrawConeRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_CAPSULE_ROTATED: DrawCapsuleRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_CYLINDER_ROTATED: DrawCylinderRotated(ref vertexIndex); break;
			case IMDrawCommandType.WIRE_FRUSTUM: DrawFrustum(ref vertexIndex); break;
		}

		m_VertexArray = null;
		m_ColorArray = null;
	}

	// Hel;per function for setting color with modified alpha
	private static void SetColor(ref Color source, Color target, float alpha)
	{
		source.r = target.r;
		source.g = target.g;
		source.b = target.b;
		source.a = alpha;
	}

	// Helper function for drawing a line, assumes the color used is m_C1
	private void Line(Vector3 p1, Vector3 p2, ref int vertexIndex)
	{
		m_VertexArray[vertexIndex] = p1;
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;

		m_VertexArray[vertexIndex] = p2;
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;
	}

	// Helper function for drawing a local space line in world space (assumes m_P1 is origin and transform values have been initialised)
	// Alternative to Line(Transform(ref p1), Transform(ref p2), ref vertexIndex);
	private void LineTransform (ref Vector3 p1, ref Vector3 p2, ref int vertexIndex)
	{
		m_VertexArray[vertexIndex] = new Vector3(
			m_P1.x + m_TXx * p1.x + m_TYx * p1.y + m_TZx * p1.z,
			m_P1.y + m_TXy * p1.x + m_TYy * p1.y + m_TZy * p1.z,
			m_P1.z + m_TXz * p1.x + m_TYz * p1.y + m_TZz * p1.z);
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;

		m_VertexArray[vertexIndex] = new Vector3(
			m_P1.x + m_TXx * p2.x + m_TYx * p2.y + m_TZx * p2.z,
			m_P1.y + m_TXy * p2.x + m_TYy * p2.y + m_TZy * p2.z,
			m_P1.z + m_TXz * p2.x + m_TYz * p2.y + m_TZz * p2.z);
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;
	}

	// Helper function for drawing a local space line in world space (assumes m_P1 is origin and transform values have been initialised)
	// Alternative to Line(Transform(ref p1), Transform(ref p2), ref vertexIndex);
	private void LineTransform(Vector3 p1, Vector3 p2, ref int vertexIndex)
	{
		m_VertexArray[vertexIndex] = new Vector3(
			m_P1.x + m_TXx * p1.x + m_TYx * p1.y + m_TZx * p1.z,
			m_P1.y + m_TXy * p1.x + m_TYy * p1.y + m_TZy * p1.z,
			m_P1.z + m_TXz * p1.x + m_TYz * p1.y + m_TZz * p1.z);
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;

		m_VertexArray[vertexIndex] = new Vector3(
			m_P1.x + m_TXx * p2.x + m_TYx * p2.y + m_TZx * p2.z,
			m_P1.y + m_TXy * p2.x + m_TYy * p2.y + m_TZy * p2.z,
			m_P1.z + m_TXz * p2.x + m_TYz * p2.y + m_TZz * p2.z);
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;
	}

	private void DrawLine(ref int vertexIndex)
	{
		m_VertexArray[vertexIndex] = m_P1;
		m_ColorArray[vertexIndex] = m_C1;
		++vertexIndex;

		m_VertexArray[vertexIndex] = m_P2;
		m_ColorArray[vertexIndex] = m_C2;
		++vertexIndex;
	}

	// P1 = origin
	// Rotation = rotation
	// Size.x = length
	private void DrawAxis(ref int vertexIndex)
	{
		// Draw X axis
		m_VertexArray[vertexIndex] = m_P1;
		SetColor(ref m_ColorArray[vertexIndex], COLOR_XAXIS, m_C1.a);
		++vertexIndex;

		m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * new Vector3(m_Size.x, 0f, 0f));
		SetColor(ref m_ColorArray[vertexIndex], COLOR_XAXIS, m_C1.a);
		++vertexIndex;

		// Draw Y axis
		m_VertexArray[vertexIndex] = m_P1;
		SetColor(ref m_ColorArray[vertexIndex], COLOR_YAXIS, m_C1.a);
		++vertexIndex;

		m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * new Vector3(0f, m_Size.x, 0f));
		SetColor(ref m_ColorArray[vertexIndex], COLOR_YAXIS, m_C1.a);
		++vertexIndex;

		// Draw Z axis
		m_VertexArray[vertexIndex] = m_P1;
		SetColor(ref m_ColorArray[vertexIndex], COLOR_ZAXIS, m_C1.a);
		++vertexIndex;

		m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * new Vector3(0f, 0f, m_Size.x));
		SetColor(ref m_ColorArray[vertexIndex], COLOR_ZAXIS, m_C1.a);
		++vertexIndex;
	}

	private void DrawGridSingleColor (ref int vertexIndex)
	{
		InitTransform();

		int cellsX = Convert.ToInt32(m_P2.x);
		int cellsY = Convert.ToInt32(m_P2.y);

		float endX = m_Size.x / 2f;
		float endZ = m_Size.y / 2f;

		float startX = -endX;
		float startZ = -endZ;

		Vector3 p0, p1;
		p0.y = p1.y = 0f;
		float v, vStep;

		v = startX;
		vStep = m_Size.x / m_P2.x;
		p0.z = startZ;
		p1.z = endZ;

		for (int i = 0; i <= cellsX; ++i)
		{
			p0.x = p1.x = v;
			v += vStep;

			m_VertexArray[vertexIndex] = LocalToWorld(ref p0);
			m_ColorArray[vertexIndex] = m_C1;
			++vertexIndex;

			m_VertexArray[vertexIndex] = LocalToWorld(ref p1);
			m_ColorArray[vertexIndex] = m_C1;
			++vertexIndex;
		}

		v = startZ;
		vStep = m_Size.y / m_P2.y;
		p0.x = startX;
		p1.x = endX;

		for (int i = 0; i <= cellsY; ++i)
		{
			p0.z = p1.z = v;
			v += vStep;

			m_VertexArray[vertexIndex] = LocalToWorld(ref p0);
			m_ColorArray[vertexIndex] = m_C1;
			++vertexIndex;

			m_VertexArray[vertexIndex] = LocalToWorld(ref p1);
			m_ColorArray[vertexIndex] = m_C1;
			++vertexIndex;
		}
	}

	//private void DrawGridDoubleColor (ref int vertexIndex)
	//{
	//}

	// P1 = origin
	// Size.x = base extents
	// Size.y = height
	// C1 = colour
	private void DrawPyramid (ref int vertexIndex)
	{
		InitTransform();

		Vector3 p1 = LocalToWorld(new Vector3(m_Size.x, 0f, m_Size.x));
		Vector3 p2 = LocalToWorld(new Vector3(-m_Size.x, 0f, m_Size.x));
		Vector3 p3 = LocalToWorld(new Vector3(-m_Size.x, 0f, -m_Size.x));
		Vector3 p4 = LocalToWorld(new Vector3(m_Size.x, 0f, -m_Size.x));
		Vector3 p5 = LocalToWorld(new Vector3(0f, m_Size.y, 0f));

		// Base
		Line(p1, p2, ref vertexIndex);
		Line(p2, p3, ref vertexIndex);
		Line(p3, p4, ref vertexIndex);
		Line(p4, p1, ref vertexIndex);

		// Length
		Line(p1, p5, ref vertexIndex);
		Line(p2, p5, ref vertexIndex);
		Line(p3, p5, ref vertexIndex);
		Line(p4, p5, ref vertexIndex);
	}

	// P1 = origin
	// Size = half extents
	// C1 = colour
	private void DrawRhombus (ref int vertexIndex)
	{
		InitTransform();

		Vector3 p1 = LocalToWorld(new Vector3(m_Size.x, 0f, m_Size.x));
		Vector3 p2 = LocalToWorld(new Vector3(-m_Size.x, 0f, m_Size.x));
		Vector3 p3 = LocalToWorld(new Vector3(-m_Size.x, 0f, -m_Size.x));
		Vector3 p4 = LocalToWorld(new Vector3(m_Size.x, 0f, -m_Size.x));
		Vector3 p5 = LocalToWorld(new Vector3(0f, m_Size.y, 0f));

		// Base
		Line(p1, p2, ref vertexIndex);
		Line(p2, p3, ref vertexIndex);
		Line(p3, p4, ref vertexIndex);
		Line(p4, p1, ref vertexIndex);

		// Up length
		Line(p1, p5, ref vertexIndex);
		Line(p2, p5, ref vertexIndex);
		Line(p3, p5, ref vertexIndex);
		Line(p4, p5, ref vertexIndex);

		// Down length
		p5 = LocalToWorld(new Vector3(0f, -m_Size.y, 0f));
		Line(p1, p5, ref vertexIndex);
		Line(p2, p5, ref vertexIndex);
		Line(p3, p5, ref vertexIndex);
		Line(p4, p5, ref vertexIndex);
	}

	// P1 = origin
	// Size = box half extents
	// C1 = colour
	private void DrawBox(ref int vertexIndex)
	{
		Vector3 p1, p2;

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y - m_Size.y; p2.z = m_P1.z - m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x - m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z - m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x - m_Size.x; p2.y = m_P1.y - m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x + m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z - m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x + m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y - m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y + m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z - m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y + m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x - m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z + m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y - m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z + m_Size.z;
		p2.x = m_P1.x - m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x - m_Size.x; p1.y = m_P1.y + m_Size.y; p1.z = m_P1.z + m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x + m_Size.x; p1.y = m_P1.y - m_Size.y; p1.z = m_P1.z + m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);

		p1.x = m_P1.x + m_Size.x; p1.y = m_P1.y + m_Size.y; p1.z = m_P1.z - m_Size.z;
		p2.x = m_P1.x + m_Size.x; p2.y = m_P1.y + m_Size.y; p2.z = m_P1.z + m_Size.z;
		Line(p1, p2, ref vertexIndex);
	}

	// P1 = origin
	// Rotation = rotation
	// Size = box half extents
	// C1 = colour
	private void DrawBoxRotated(ref int vertexIndex)
	{
		Vector3 sx, sy, sz;
		SizeToDirections(out sx, out sy, out sz);

		Vector3 max = m_P1 + sx + sy + sz;
		Vector3 min = m_P1 - sx - sy - sz;

		Line(min,					m_P1 + sx - sy - sz,	ref vertexIndex);
		Line(min,					m_P1 - sx + sy - sz,	ref vertexIndex);
		Line(min,					m_P1 - sx - sy + sz,	ref vertexIndex);
		Line(m_P1 + sx - sy - sz,	m_P1 + sx + sy - sz,	ref vertexIndex);
		Line(m_P1 + sx - sy - sz,	m_P1 + sx - sy + sz,	ref vertexIndex);
		Line(m_P1 - sx + sy - sz,	m_P1 + sx + sy - sz,	ref vertexIndex);
		Line(m_P1 - sx + sy - sz,	m_P1 - sx + sy + sz,	ref vertexIndex);
		Line(m_P1 + sx - sy + sz,	m_P1 - sx - sy + sz,	ref vertexIndex);
		Line(m_P1 - sx - sy + sz,	m_P1 - sx + sy + sz,	ref vertexIndex);
		Line(max,					m_P1 - sx + sy + sz,	ref vertexIndex);
		Line(m_P1 + sx - sy + sz,	max,					ref vertexIndex);
		Line(m_P1 + sx + sy - sz,	max,					ref vertexIndex);
	}

	private void DrawDiscRotated (ref int vertexIndex)
	{
		InitTransform();

		Vector3 r, s;
		float radius = m_Size.x;
		int index = 0, lastIndex = m_Sine.Length - 1;

		switch (m_Axis)
		{
			case IMDrawAxis.X:
				{
					while (index < lastIndex)
					{
						r.x = 0f;
						r.y = m_Sine[index] * radius; 
						r.z = m_Cosine[index] * radius;

						++index;

						s.x = 0f;
						s.y = m_Sine[index] * radius; 
						s.z = m_Cosine[index] * radius;

						//Line(Transform(ref r), Transform(ref s), ref vertexIndex);
						LineTransform(ref r, ref s, ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.Y:
				{
					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius; 
						r.y = 0f;
						r.z = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = 0f;
						s.z = m_Cosine[index] * radius;

						//Line(Transform(ref r), Transform(ref s), ref vertexIndex);
						LineTransform(ref r, ref s, ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.Z:
				{
					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;
						r.z = 0f;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;
						s.z = 0f;

						//Line(Transform(ref r), Transform(ref s), ref vertexIndex);
						LineTransform(ref r, ref s, ref vertexIndex);
					}
				}
				break;
		}
	}

	private void DrawSphere(ref int vertexIndex)
	{
		Vector3 p1, p2;
		Vector2 r, s;

		float radius = m_Size.x;

		int index = 0, lastIndex = m_Sine.Length - 1;

		while (index < lastIndex)
		{
			r.x = m_Sine[index] * radius;
			r.y = m_Cosine[index] * radius;

			++index;

			s.x = m_Sine[index] * radius;
			s.y = m_Cosine[index] * radius;

			p1.x = m_P1.x + r.x; p1.y = m_P1.y + r.y; p1.z = m_P1.z;
			p2.x = m_P1.x + s.x; p2.y = m_P1.y + s.y; p2.z = m_P1.z;
			Line(p1, p2, ref vertexIndex);

			p1.x = m_P1.x; p1.y = m_P1.y + r.x; p1.z = m_P1.z + r.y;
			p2.x = m_P1.x; p2.y = m_P1.y + s.x; p2.z = m_P1.z + s.y;
			Line(p1, p2, ref vertexIndex);

			p1.x = m_P1.x + r.x; p1.y = m_P1.y; p1.z = m_P1.z + r.y;
			p2.x = m_P1.x + s.x; p2.y = m_P1.y; p2.z = m_P1.z + s.y;
			Line(p1, p2, ref vertexIndex);
		}
	}

	private void DrawSphereRotated(ref int vertexIndex)
	{
		InitTransform();

		Vector3 p1, p2;
		Vector2 r, s;

		float radius = m_Size.x;

		int index = 0, lastIndex = m_Sine.Length - 1;

		while (index < lastIndex)
		{
			r.x = m_Sine[index] * radius;
			r.y = m_Cosine[index] * radius;

			++index;

			s.x = m_Sine[index] * radius;
			s.y = m_Cosine[index] * radius;

			p1.x = r.x; p1.y = r.y; p1.z = 0.0f;
			p2.x = s.x; p2.y = s.y; p2.z = 0.0f;
			LineTransform(ref p1, ref p2, ref vertexIndex);

			p1.x = 0.0f; p1.y = r.x; p1.z = r.y;
			p2.x = 0.0f; p2.y = s.x; p2.z = s.y;
			LineTransform(ref p1, ref p2, ref vertexIndex);

			p1.x = r.x; p1.y = 0.0f; p1.z = r.y;
			p2.x = s.x; p2.y = 0.0f; p2.z = s.y;
			LineTransform(ref p1, ref p2, ref vertexIndex);
		}
	}

	/*
	private void DrawAxisSphereRotated (ref int vertexIndex)
	{
		Vector3 p1, p2;
		Vector2 r, s;

		float radius = m_Size.x;

		int index = 0, lastIndex = m_Sine.Length - 1;

		while (index < lastIndex)
		{
			r.x = m_Sine[index] * radius;
			r.y = m_Cosine[index] * radius;

			++index;

			s.x = m_Sine[index] * radius;
			s.y = m_Cosine[index] * radius;

			p1.x = r.x; p1.y = r.y; p1.z = 0.0f;
			p2.x = s.x; p2.y = s.y; p2.z = 0.0f;
			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p1);
			m_ColorArray[vertexIndex] = COLOR_XAXIS;
			++vertexIndex;

			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p2);
			m_ColorArray[vertexIndex] = COLOR_XAXIS;
			++vertexIndex;

			p1.x = 0.0f; p1.y = r.x; p1.z = r.y;
			p2.x = 0.0f; p2.y = s.x; p2.z = s.y;
			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p1);
			m_ColorArray[vertexIndex] = COLOR_YAXIS;
			++vertexIndex;

			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p2);
			m_ColorArray[vertexIndex] = COLOR_YAXIS;
			++vertexIndex;

			p1.x = r.x; p1.y = 0.0f; p1.z = r.y;
			p2.x = s.x; p2.y = 0.0f; p2.z = s.y;
			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p1);
			m_ColorArray[vertexIndex] = COLOR_ZAXIS;
			++vertexIndex;

			m_VertexArray[vertexIndex] = m_P1 + (m_Rotation * p2);
			m_ColorArray[vertexIndex] = COLOR_ZAXIS;
			++vertexIndex;
		}
	}
	*/

	private void DrawEllipsoid (ref int vertexIndex)
	{
		Vector3 size = m_Size * 0.5f;

		Vector3 p1, p2;
		Vector2 r, s;

		int index = 0, lastIndex = m_Sine.Length - 1;

		while (index < lastIndex)
		{
			r.x = m_Sine[index];
			r.y = m_Cosine[index];

			++index;

			s.x = m_Sine[index];
			s.y = m_Cosine[index];

			p1.x = r.x * size.x; p1.y = r.y * size.y; p1.z = 0.0f;
			p2.x = s.x * size.x; p2.y = s.y * size.y; p2.z = 0.0f;
			Line(m_P1 + p1, m_P1 = p2, ref vertexIndex);

			p1.x = 0.0f; p1.y = r.x * size.y; p1.z = r.y * size.z;
			p2.x = 0.0f; p2.y = s.x * size.y; p2.z = s.y * size.z;
			Line(m_P1 + p1, m_P1 = p2, ref vertexIndex);

			p1.x = r.x * size.x; p1.y = 0.0f; p1.z = r.y * size.z;
			p2.x = s.x * size.x; p2.y = 0.0f; p2.z = s.y * size.z;
			Line(m_P1 + p1, m_P1 = p2, ref vertexIndex);
		}
	}

	private void DrawEllipsoidRotated (ref int vertexIndex)
	{
		InitTransform();

		Vector3 size = m_Size * 0.5f;

		Vector3 p1, p2;
		Vector2 r, s;

		int index = 0, lastIndex = m_Sine.Length - 1;

		while (index < lastIndex)
		{
			r.x = m_Sine[index];
			r.y = m_Cosine[index];

			++index;

			s.x = m_Sine[index];
			s.y = m_Cosine[index];

			p1.x = r.x * size.x; p1.y = r.y * size.y; p1.z = 0.0f;
			p2.x = s.x * size.x; p2.y = s.y * size.y; p2.z = 0.0f;
			Line(LocalToWorld(ref p1), LocalToWorld(ref p2), ref vertexIndex);

			p1.x = 0.0f; p1.y = r.x * size.y; p1.z = r.y * size.z;
			p2.x = 0.0f; p2.y = s.x * size.y; p2.z = s.y * size.z;
			Line(LocalToWorld(ref p1), LocalToWorld(ref p2), ref vertexIndex);

			p1.x = r.x * size.x; p1.y = 0.0f; p1.z = r.y * size.z;
			p2.x = s.x * size.x; p2.y = 0.0f; p2.z = s.y * size.z;
			Line(LocalToWorld(ref p1), LocalToWorld(ref p2), ref vertexIndex);
		}
	}

	private void DrawConeRotated(ref int vertexIndex)
	{
		InitTransform();

		Vector3 r, s;
		float radius = m_Size.x;
		int index = 0, lastIndex = m_Sine.Length - 1;

		switch (m_Axis)
		{
			case IMDrawAxis.X:
				{
					// Draw base
					while (index < lastIndex)
					{
						r.x = 0f;
						r.y = m_Sine[index] * radius;
						r.z = m_Cosine[index] * radius;

						++index;

						s.x = 0f;
						s.y = m_Sine[index] * radius;
						s.z = m_Cosine[index] * radius;

						LineTransform(ref r, ref s, ref vertexIndex);
					}

					// Draw sides
					Vector3 p = LocalToWorld(m_Size.y, 0f, 0f);
					Line(p, LocalToWorld(0f, m_Size.x, 0f), ref vertexIndex);
					Line(p, LocalToWorld(0f, -m_Size.x, 0f), ref vertexIndex);
					Line(p, LocalToWorld(0f, 0f, m_Size.x), ref vertexIndex);
					Line(p, LocalToWorld(0f, 0f, -m_Size.x), ref vertexIndex);
				}
				break;

			case IMDrawAxis.Y:
				{
					// Draw base
					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = 0f;
						r.z = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = 0f;
						s.z = m_Cosine[index] * radius;

						LineTransform(ref r, ref s, ref vertexIndex);
					}

					LineTransform(Vector3.zero, new Vector3(0f, m_Size.y, 0f), ref vertexIndex);

					// Draw sides
					Vector3 p = LocalToWorld(0f, m_Size.y, 0f);
					Line(p, LocalToWorld(m_Size.x, 0f, 0f), ref vertexIndex);
					Line(p, LocalToWorld(-m_Size.x, 0f, 0f), ref vertexIndex);
					Line(p, LocalToWorld(0f, 0f, m_Size.x), ref vertexIndex);
					Line(p, LocalToWorld(0f, 0f, -m_Size.x), ref vertexIndex);
				}
				break;

			case IMDrawAxis.Z:
				{
					// Draw base
					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;
						r.z = 0f;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;
						s.z = 0f;

						//Line(Transform(ref r), Transform(ref s), ref vertexIndex);
						LineTransform(ref r, ref s, ref vertexIndex);
					}

					// Draw sides
					Vector3 p = LocalToWorld(0f, 0f, m_Size.y);
					Line(p, LocalToWorld(m_Size.x, 0f, 0f), ref vertexIndex);
					Line(p, LocalToWorld(-m_Size.x, 0f, 0f), ref vertexIndex);
					Line(p, LocalToWorld(0f, m_Size.x, 0f), ref vertexIndex);
					Line(p, LocalToWorld(0f, -m_Size.x, 0f), ref vertexIndex);
				}
				break;
		}
	}

	private void DrawCapsuleRotated (ref int vertexIndex)
	{
		float diameter = m_Size.y * 2.0f;
		float height = m_Size.x;

		if (height <= diameter) // If the diameter is equal to or less than the height, then we revert to a sphere
		{
			DrawSphereRotated(ref vertexIndex);
			return;
		}

		InitTransform();

		float radius = m_Size.y;
		float mid = height - diameter;
		float halfMid = mid * 0.5f;

		Vector3 p1, p2;
		Vector2 r, s;

		int index = 0, lastIndex;

		switch (m_Axis)
		{
			case IMDrawAxis.Z:
				{
					p1.x = radius; p1.y = 0.0f; p1.z = halfMid;
					p2.x = radius; p2.y = 0.0f; p2.z = -halfMid;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = -radius; p1.y = 0.0f; p1.z = halfMid;
					p2.x = -radius; p2.y = 0.0f; p2.z = -halfMid;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = radius; p1.z = halfMid;
					p2.x = 0.0f; p2.y = radius; p2.z = -halfMid;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = -radius; p1.z = halfMid;
					p2.x = 0.0f; p2.y = -radius; p2.z = -halfMid;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					lastIndex = m_Sine.Length / 2;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = 0.0f; p1.y = r.x; p1.z = r.y + halfMid;
						p2.x = 0.0f; p2.y = s.x; p2.z = s.y + halfMid;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);

						p1.x = r.x; p1.y = 0.0f; p1.z = r.y + halfMid;
						p2.x = s.x; p2.y = 0.0f; p2.z = s.y + halfMid;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}

					index = 0;
					lastIndex = m_Sine.Length - 1;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = r.x; p1.y = r.y; p1.z = halfMid;
						p2.x = s.x; p2.y = s.y; p2.z = halfMid;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.Y:
				{
					p1.x = radius; p1.y = halfMid; p1.z = 0.0f;
					p2.x = radius; p2.y = -halfMid; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = -radius; p1.y = halfMid; p1.z = 0.0f;
					p2.x = -radius; p2.y = -halfMid; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = halfMid; p1.z = radius;
					p2.x = 0.0f; p2.y = -halfMid; p2.z = radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = halfMid; p1.z = -radius;
					p2.x = 0.0f; p2.y = -halfMid; p2.z = -radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					lastIndex = m_Sine.Length / 2;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = 0.0f; p1.y = r.y + halfMid; p1.z = r.x;
						p2.x = 0.0f; p2.y = s.y + halfMid; p2.z = s.x;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);

						p1.x = r.x; p1.y = r.y + halfMid; p1.z = 0.0f;
						p2.x = s.x; p2.y = s.y + halfMid; p2.z = 0.0f;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}

					index = 0;
					lastIndex = m_Sine.Length - 1;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = r.x; p1.y = halfMid; p1.z = r.y;
						p2.x = s.x; p2.y = halfMid; p2.z = s.y;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.X:
				{
					p1.x = halfMid; p1.y = radius; p1.z = 0.0f;
					p2.x = -halfMid; p2.y = radius; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfMid; p1.y = -radius; p1.z = 0.0f;
					p2.x = -halfMid; p2.y = -radius; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfMid; p1.y = 0.0f; p1.z = radius;
					p2.x = -halfMid; p2.y = 0.0f; p2.z = radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfMid; p1.y = 0.0f; p1.z = -radius;
					p2.x = -halfMid; p2.y = 0.0f; p2.z = -radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					lastIndex = m_Sine.Length / 2;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = r.y + halfMid; p1.y = 0.0f; p1.z = r.x;
						p2.x = s.y + halfMid; p2.y = 0.0f; p2.z = s.x;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);

						p1.x = r.y + halfMid; p1.y = r.x; p1.z = 0.0f;
						p2.x = s.y + halfMid; p2.y = s.x; p2.z = 0.0f;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}

					index = 0;
					lastIndex = m_Sine.Length - 1;

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = halfMid; p1.y = r.x; p1.z = r.y;
						p2.x = halfMid; p2.y = s.x; p2.z = s.y;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;
		}
	}

	private void DrawCylinderRotated (ref int vertexIndex)
	{
		InitTransform();

		float length = m_Size.x;
		float halfLength = length * 0.5f;
		float radius = m_Size.y;

		Vector3 p1, p2;
		Vector2 r, s;

		int index = 0, lastIndex = m_Sine.Length - 1;

		switch (m_Axis)
		{
			case IMDrawAxis.Z:
				{
					p1.x = radius; p1.y = 0.0f; p1.z = halfLength;
					p2.x = radius; p2.y = 0.0f; p2.z = -halfLength;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = -radius; p1.y = 0.0f; p1.z = halfLength;
					p2.x = -radius; p2.y = 0.0f; p2.z = -halfLength;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = radius; p1.z = halfLength;
					p2.x = 0.0f; p2.y = radius; p2.z = -halfLength;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = -radius; p1.z = halfLength;
					p2.x = 0.0f; p2.y = -radius; p2.z = -halfLength;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = r.x; p1.y = r.y; p1.z = halfLength;
						p2.x = s.x; p2.y = s.y; p2.z = halfLength;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.Y:
				{
					p1.x = radius; p1.y = halfLength; p1.z = 0.0f;
					p2.x = radius; p2.y = -halfLength; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = -radius; p1.y = halfLength; p1.z = 0.0f;
					p2.x = -radius; p2.y = -halfLength; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = halfLength; p1.z = radius;
					p2.x = 0.0f; p2.y = -halfLength; p2.z = radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = 0.0f; p1.y = halfLength; p1.z = -radius;
					p2.x = 0.0f; p2.y = -halfLength; p2.z = -radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = r.x; p1.y = halfLength; p1.z = r.y;
						p2.x = s.x; p2.y = halfLength; p2.z = s.y;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;

			case IMDrawAxis.X:
				{
					p1.x = halfLength; p1.y = radius; p1.z = 0.0f;
					p2.x = -halfLength; p2.y = radius; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfLength; p1.y = -radius; p1.z = 0.0f;
					p2.x = -halfLength; p2.y = -radius; p2.z = 0.0f;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfLength; p1.y = 0.0f; p1.z = radius;
					p2.x = -halfLength; p2.y = 0.0f; p2.z = radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					p1.x = halfLength; p1.y = 0.0f; p1.z = -radius;
					p2.x = -halfLength; p2.y = 0.0f; p2.z = -radius;
					LineTransform(ref p1, ref p2, ref vertexIndex);

					while (index < lastIndex)
					{
						r.x = m_Sine[index] * radius;
						r.y = m_Cosine[index] * radius;

						++index;

						s.x = m_Sine[index] * radius;
						s.y = m_Cosine[index] * radius;

						p1.x = halfLength; p1.y = r.x; p1.z = r.y;
						p2.x = halfLength; p2.y = s.x; p2.z = s.y;
						Line(LocalToWorld(p1), LocalToWorld(p2), ref vertexIndex);
						Line(LocalToWorld(-p1), LocalToWorld(-p2), ref vertexIndex);
					}
				}
				break;
		}
	}

	private void DrawFrustum (ref int vertexIndex)
	{
		if (m_Camera == null) // Ensure the camera reference is still valid, if the camera was deleted between the time the draw was issued and when it is drawn, this would become null
			return;

		Transform camTransform = m_Camera.transform;
		Vector3 pos = camTransform.position;
		Vector3 right = camTransform.right;
		Vector3 up = camTransform.up;
		Vector3 forward = camTransform.forward;
		float nearDist = m_Camera.nearClipPlane;
		float farDist = m_Camera.farClipPlane;
		Vector3 near = pos + forward * nearDist;
		Vector3 far = pos + forward * farDist;
		Rect rect = m_Camera.rect;

		Vector3 xOffset, yOffset, n1, n2, n3, n4 , f1, f2, f3, f4;

		if (m_Camera.orthographic)
		{
			float height = m_Camera.orthographicSize;
			
			float width = height * m_Camera.aspect * rect.width / rect.height;

			yOffset = up * height;
			xOffset = right * width;

			n1 = near - yOffset - xOffset;
			n2 = near - yOffset + xOffset;
			n3 = near + yOffset + xOffset;
			n4 = near + yOffset - xOffset;

			//f1 = far - yOffset - xOffset;
			//f2 = far - yOffset + xOffset;
			//f3 = far + yOffset + xOffset;
			//f4 = far + yOffset - xOffset;
		}
		else // Perspective
		{
			float aspect = m_Camera.aspect * rect.width / rect.height;
			float fov = 2f * (float)Math.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);

			float nearHeight = fov * nearDist;
			float nearWidth = nearHeight * aspect;

			float farHeight = fov * farDist;
			float farWidth = farHeight * aspect;

			yOffset = up * nearHeight * 0.5f;
			xOffset = right * nearWidth * 0.5f;

			n1 = near - yOffset - xOffset;
			n2 = near - yOffset + xOffset;
			n3 = near + yOffset + xOffset;
			n4 = near + yOffset - xOffset;

			yOffset = up * farHeight * 0.5f;
			xOffset = right * farWidth * 0.5f;

			//f1 = far - yOffset - xOffset;
			//f2 = far - yOffset + xOffset;
			//f3 = far + yOffset + xOffset;
			//f4 = far + yOffset - xOffset;
		}

		f1 = far - yOffset - xOffset;
		f2 = far - yOffset + xOffset;
		f3 = far + yOffset + xOffset;
		f4 = far + yOffset - xOffset;

		// Draw near plane
		Line(n1, n2, ref vertexIndex);
		Line(n2, n3, ref vertexIndex);
		Line(n3, n4, ref vertexIndex);
		Line(n4, n1, ref vertexIndex);

		// Draw far plane
		Line(f1, f2, ref vertexIndex);
		Line(f2, f3, ref vertexIndex);
		Line(f3, f4, ref vertexIndex);
		Line(f4, f1, ref vertexIndex);

		// Draw frustum edges
		Line(n1, f1, ref vertexIndex);
		Line(n2, f2, ref vertexIndex);
		Line(n3, f3, ref vertexIndex);
		Line(n4, f4, ref vertexIndex);
	}
}