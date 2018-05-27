using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class IMDrawMeshCommand : IMDrawCommand
{
	private static readonly Quaternion	CAPSULE_BOTTOM_CAP_ROTATION = Quaternion.Euler(180f, 0f, 0f);

	public IMDrawCommandType			m_Type;
	public int							m_Verts;
	public Vector3						m_Position;
	public Quaternion					m_Rotation;
	public Vector3						m_Size;
	public Color						m_Color;
	//public IMDrawAxis					m_Axis; // Only used when creating the command, so not needed here
	public Mesh							m_Mesh;
	public IMDrawZTest					m_ZTest;

	public LinkedListNode<IMDrawMeshCommand> m_ListNode;

	private static MaterialPropertyBlock	s_MaterialPropertyBlock;
	private static Matrix4x4				s_Matrix;

	static IMDrawMeshCommand ()
	{
		s_MaterialPropertyBlock = new MaterialPropertyBlock();
		s_Matrix = new Matrix4x4 ();
    }

	public IMDrawMeshCommand ()
	{
		m_ListNode = new LinkedListNode<IMDrawMeshCommand>(this);
    }

	public float GetDistSqrd (ref Vector3 position)
	{
		float dx = position.x - m_Position.x;
		float dy = position.y - m_Position.y;
		float dz = position.z - m_Position.z;
		return dx * dx + dy * dy + dz * dz;
	}

	private Vector3 ToUpVector (float length)
	{
		float num = m_Rotation.x * 2.0f;
		float num2 = m_Rotation.y * 2.0f;
		float num3 = m_Rotation.z * 2.0f;
		return new Vector3(
			((m_Rotation.x * num2) - (m_Rotation.w * num3)) * length,
			(1.0f - ((m_Rotation.x * num) + (m_Rotation.z * num3))) * length,
			((m_Rotation.y * num3) + (m_Rotation.w * num)) * length);
	}

	private Vector3 CalculateCapsuleCapOffset (float radius, float bodyHeight)
	{
		float num = m_Rotation.x * 2.0f;
		float num2 = m_Rotation.y * 2.0f;
		float num3 = m_Rotation.z * 2.0f;

		return new Vector3(
			bodyHeight * ((m_Rotation.x * num2) - (m_Rotation.w * num3)),
			bodyHeight * (1.0f - ((m_Rotation.x * num) + (m_Rotation.z * num3))),
			bodyHeight * ((m_Rotation.y * num3) + (m_Rotation.w * num)));
	}

	public void SetRotation(ref Quaternion rotation, IMDrawAxis axis)
	{
		switch (axis)
		{
			case IMDrawAxis.X: m_Rotation = rotation * AXIS_X_ROTATION; break;
			//case IMDrawAxis.Y: outRotation = inRotation; break;
			case IMDrawAxis.Z: m_Rotation = rotation * AXIS_Z_ROTATION; break;
			default: m_Rotation = rotation; break;
		}
	}

	public void Draw (IMDrawCamera component)
	{
		s_MaterialPropertyBlock.SetColor(IMDrawSPID._Color, m_Color);
		//s_MaterialPropertyBlock.SetFloat(IMDrawSPID._ZTest, (float)m_ZTest); // This doesn't work for some reason - probably a Unity bug?
		component.MaterialMesh.SetInt(IMDrawSPID._ZTest, (int)m_ZTest);

		switch (m_Type)
		{
			case IMDrawCommandType.QUAD:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshQuad, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.BOX:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshBox, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.PYRAMID:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshPyramid, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.RHOMBUS:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshRhombus, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.DISC:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshDisc, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.SPHERE:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshSphere, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CONE:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCone, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CAPSULE:
				{
					float bodyHeight = m_Size.y - (m_Size.x * 2f);
					s_Matrix.SetTRS(m_Position, m_Rotation, new Vector3(m_Size.x, bodyHeight, m_Size.z));
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleBody, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);

					Vector3 capOffset = ToUpVector(bodyHeight * 0.5f);

					s_Matrix.SetTRS(
						m_Position + capOffset,
						m_Rotation,
						new Vector3(m_Size.x, m_Size.x, m_Size.x));
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleCap, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);

					s_Matrix.SetTRS(
						m_Position - capOffset,
						m_Rotation * CAPSULE_BOTTOM_CAP_ROTATION,
						new Vector3(m_Size.x, m_Size.x, m_Size.x));

					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleCap, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CYLINDER:
				{
					s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCylinder, s_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, s_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CUSTOM_MESH:
				{
					if (m_Mesh != null)
					{
						s_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
						Graphics.DrawMesh(m_Mesh, s_Matrix, component.MaterialMesh, 0, component.Camera, component.MeshLayer, s_MaterialPropertyBlock);
					}
				}
				break;
		}
	}
}