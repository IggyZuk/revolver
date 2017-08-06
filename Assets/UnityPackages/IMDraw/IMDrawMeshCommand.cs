using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class IMDrawMeshCommand : IMDrawCommand
{
	private const string				SHADER_PROPERTY_COLOR = "_Color";
	private static int					SHADER_PROPERTY_ID_COLOR;

	private static readonly Quaternion	CAPSULE_BOTTOM_CAP_ROTATION = Quaternion.Euler(180f, 0f, 0f);

	public IMDrawCommandType			m_Type;
	public int							m_Verts;
	public Vector3						m_Position;
	public Quaternion					m_Rotation;
	public Vector3						m_Size;
	public Color						m_Color;
	//public IMDrawAxis					m_Axis; // Only used when creating the command, so not needed here
	public Mesh							m_Mesh;

	public LinkedListNode<IMDrawMeshCommand> m_ListNode;

	private static MaterialPropertyBlock	m_MaterialPropertyBlock;
	private static Matrix4x4				m_Matrix;

	static IMDrawMeshCommand ()
	{
		m_MaterialPropertyBlock = new MaterialPropertyBlock();
		m_Matrix = new Matrix4x4 ();
		SHADER_PROPERTY_ID_COLOR = Shader.PropertyToID(SHADER_PROPERTY_COLOR); // Cache the color shader property ID
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
		m_MaterialPropertyBlock.SetColor(SHADER_PROPERTY_ID_COLOR, m_Color);

		switch (m_Type)
		{
			case IMDrawCommandType.QUAD:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshQuad, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.BOX:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshBox, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.PYRAMID:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshPyramid, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.RHOMBUS:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshRhombus, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.DISC:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshDisc, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.SPHERE:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshSphere, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CONE:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCone, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CAPSULE:
				{
					float bodyHeight = m_Size.y - (m_Size.x * 2f);
					m_Matrix.SetTRS(m_Position, m_Rotation, new Vector3(m_Size.x, bodyHeight, m_Size.z));
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleBody, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);

					Vector3 capOffset = ToUpVector(bodyHeight * 0.5f);

					m_Matrix.SetTRS(
						m_Position + capOffset,
						m_Rotation,
						new Vector3(m_Size.x, m_Size.x, m_Size.x));
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleCap, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);

					m_Matrix.SetTRS(
						m_Position - capOffset,
						m_Rotation * CAPSULE_BOTTOM_CAP_ROTATION,
						new Vector3(m_Size.x, m_Size.x, m_Size.x));

					Graphics.DrawMesh(IMDrawManager.Instance.MeshCapsuleCap, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CYLINDER:
				{
					m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
					Graphics.DrawMesh(IMDrawManager.Instance.MeshCylinder, m_Matrix, component.MaterialMesh, component.MeshLayer, component.Camera, 0, m_MaterialPropertyBlock);
				}
				break;

			case IMDrawCommandType.CUSTOM_MESH:
				{
					if (m_Mesh != null)
					{
						m_Matrix.SetTRS(m_Position, m_Rotation, m_Size);
						Graphics.DrawMesh(m_Mesh, m_Matrix, component.MaterialMesh, 0, component.Camera, component.MeshLayer, m_MaterialPropertyBlock);
					}
				}
				break;
		}
	}
}