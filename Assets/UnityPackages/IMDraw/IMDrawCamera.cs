//#define ENABLE_DEBUGINFO
//#define ENABLE_PROFILING

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

#pragma warning disable 0649 // Ignore false warnings "never assigned to, and will always have it's default value null"

// IMDraw ordering:
// 1. Mesh primitives.
// 2. GL lines.
// 3. 3D labels (only if sorting by distance is enabled)
// 4. Labels.

public enum IMDrawAxis
{
	X,
	Y,
	Z,
};

public enum IMDrawCommandType
{
	LINE,
	AXIS,
	GRID_SINGLE_COLOR,
	GRID_DOUBLE_COLOR,
	WIRE_PYRAMID_ROTATED,
	WIRE_RHOMBUS_ROTATED,
	WIRE_BOX,
	WIRE_BOX_ROTATED,
	WIRE_DISC_ROTATED,
	WIRE_SPHERE,
	WIRE_SPHERE_ROTATED,
	WIRE_ELLIPSOID,
	WIRE_ELLIPSOID_ROTATED,
	WIRE_CONE_ROTATED,
	WIRE_CAPSULE_ROTATED,
	WIRE_CYLINDER_ROTATED,
	WIRE_FRUSTUM,

	// Mesh primitives - these are always rotated
	QUAD,
	BOX,
	PYRAMID,
	RHOMBUS,
	DISC,
	SPHERE,
	ELLIPSOID,
	CONE,
	CAPSULE,
	CYLINDER,
	CUSTOM_MESH,
}


[Flags]
public enum IMDrawDisabledFlag
{
	NOT_RENDERING		= (1 << 0), // Camera isn't rendering
}

public static class IMDrawVertexCount
{
	public const int LINE = 2;
	public const int AXIS = 6;
	public const int WIRE_BOX = 24;
	public const int WIRE_PYRAMID = 16;
	public const int WIRE_RHOMBUS = 24;
	public const int WIRE_DISC = 48;
	public const int WIRE_SPHERE = 144;
	public const int WIRE_ELLIPSOID = 144;
	public const int WIRE_CONE = 56;
	public const int WIRE_CAPSULE = 144;
	public const int WIRE_CYLINDER = 104;
	public const int WIRE_FRUSTRUM = 24;

	public const int MESH_QUAD = 4;
	public const int MESH_BOX = 8;
	public const int MESH_PYRAMID = 5;
	public const int MESH_RHOMBUS = 6;
	public const int MESH_DISC = 48;
	public const int MESH_SPHERE = 266;
	public const int MESH_CONE = 24;
	public const int MESH_CAPSULE = 48 + (145 * 2);
	public const int MESH_CYLINDER = 48;
}

/// <summary>
/// IMDrawCamera is a primitive/GUI renderer for a camera. These components should be last scripts to be called in a frame, to catch all draw requests.
/// </summary>
[AddComponentMenu("IMDraw/IMDraw Camera"), RequireComponent(typeof(Camera)), DisallowMultipleComponent]
public class IMDrawCamera : MonoBehaviour
{
	// Note: These control the initial stack capacity for command pools. When pool capacity is exceeded, it will expand the array by resizing it which will create garbage as a result
	private const int						INIT_GL_POOL_CAPACITY = 256;
	private const int						INIT_MESH_POOL_CAPACITY = 256;
	private const int						INIT_LABEL_POOL_CAPACITY = 256;

	[SerializeField]
	private int								m_Priority;

	[SerializeField]
	private Material						m_MaterialGL;

	[SerializeField]
	private Material						m_MaterialMesh;

	[SerializeField]
	private Font							m_Font;

	[SerializeField]
	private int								m_FontSize = 14;

	[SerializeField]
	private bool							m_3DDistanceCull;

	[SerializeField]
	private float							m_3DDistanceCullMaxDist = 100;

	// Label settings
	[SerializeField]
	private bool							m_3DLabelFadeOverDistance;

	[SerializeField]
	private float							m_3DLabelMinFadeDistance = 10f;

	[SerializeField]
	private float							m_3DLabelMaxFadeDistance = 100f;

	[SerializeField]
	private bool							m_3DLabelSortByDistance = true;

	private float							m_3DLabelMinFadeDistanceSqrd;
	private float							m_3DLabelMaxFadeDistanceSqrd;
	private float							m_3DLabelFadeDistanceDelta;

	[SerializeField]
	private bool							m_RefreshInspector; // This is used to force the inspector to update, since the inspector is providing real time information

	private Camera							m_Camera;
	private Transform						m_CameraTransform;
	private Vector3							m_CameraPosition;

	private Vector3[]						m_VertexArray;
	private Color[]							m_ColorArray;
	private int								m_VertIndex;

	// GL vertex & primitives
	[SerializeField]
	private int								m_MaxGLVertices = 2048;
	private int								m_GLVertexCount;
	private int								m_GLPrimitiveCount;
	private bool							m_ExceededGLVertLimit;

	// Mesh vertex & primitives
	[SerializeField]
	private int								m_MaxMeshVertices = 4096;
	private int								m_MeshVertexCount;
	private int								m_MeshPrimitiveCount;
	private bool							m_ExceededMeshVertLimit;

	[SerializeField]
	private int								m_MeshLayer; // The layer used for mesh primitives
	
	// GUI text
	[SerializeField]
	private int								m_MaxVisibleLabels = 64;
	private int								m_LabelCount;
	private int								m_VisibleLabelCount;
	private bool							m_ExceededVisibleLabelLimit;

	private static List<IMDrawCamera>		s_IMDrawCameraList = new List<IMDrawCamera>(); // List of cameras which are active

	private GUIStyle						m_FontGUIStyle;

	private LinkedList<IMDrawGLCommand>		m_GLCommandList; // List for active GL draw commands
	private Stack<IMDrawGLCommand>			m_GLCommandPool; // Pool for GL draw command objects - note: when this stack grows, it uses Array.resize which will briefly create garbage

	private LinkedList<IMDrawMeshCommand>	m_MeshCommandList; // List for active mesh draw commands
	private Stack<IMDrawMeshCommand>		m_MeshCommandPool; // Pool for mesh draw command objects - note: when this stack grows, it uses Array.resize which will briefly create garbage

	private LinkedList<IMDrawGUICommand>	m_GUICommandList; // List for GUI draw command objects
	private List<IMDrawGUICommand>			m_GUI2DDrawList;
	private List<IMDrawGUICommand>			m_GUI3DDrawList;
    private Stack<IMDrawLabelCommand>		m_LabelCommandPool; // Pool for GUI text draw command objects - note: when this stack grows, it uses Array.resize which will briefly create garbage

	private IMDrawDisabledFlag				m_Disabled;

	//public Material MaterialGL { get { return m_MaterialGL; } }
	public Material MaterialMesh { get { return m_MaterialMesh; } }
	public bool IsMaterialMissing { get { return m_MaterialGL == null || m_MaterialMesh == null; } }

	public IMDrawDisabledFlag DisabledFlag	{ get { return m_Disabled; } }

	public int GLVertexCount { get { return m_GLVertexCount; } }
	public int MaxGLVertices { get { return m_MaxGLVertices; } }
	public int GLPrimitiveCount { get { return m_GLPrimitiveCount; } }
	public bool ExceededGLVertLimit { get { return m_ExceededGLVertLimit;  } }

	public int MeshVertexCount { get { return m_MeshVertexCount; } }
	public int MaxMeshVertices { get { return m_MaxMeshVertices; } }
	public int MeshPrimitiveCount { get { return m_MeshPrimitiveCount; } }
	public bool ExceededMeshVertLimit { get { return m_ExceededMeshVertLimit; } }

	public int LabelCount { get { return m_LabelCount; } }
	public int MaxVisibleLabels { get { return m_MaxVisibleLabels; } }
	public int VisibleLabelCount { get { return m_VisibleLabelCount; } }
	public bool ExceededVisibleLabelLimit { get { return m_ExceededVisibleLabelLimit; } }

	public GUIStyle FontGUIStyle { get { return m_FontGUIStyle; } }

	public Camera Camera { get { return m_Camera; }	}

	public int MeshLayer { get { return m_MeshLayer; } }

	#region ========== CAMERA INSTANCE MANAGEMENT ==========

	private static void RegisterCamera(IMDrawCamera cam)
	{
		for (int i = 0; i < s_IMDrawCameraList.Count; ++i)
		{
			if (cam.m_Priority < s_IMDrawCameraList[i].m_Priority)
			{
				s_IMDrawCameraList.Insert(i, cam);
				return;
			}
		}

		s_IMDrawCameraList.Add(cam);

		if (IMDrawManager.TargetCamera == null) // There is currently no selected camera so automatically select the one with highest priority
		{
			IMDrawManager.TargetCamera = s_IMDrawCameraList[s_IMDrawCameraList.Count - 1];
		}
    }

	private static void UnregisterCamera(IMDrawCamera cam)
	{
		s_IMDrawCameraList.Remove(cam);

		if (IMDrawManager.TargetCamera == cam) // If the camera being unregistered the current draw target, select the next highest priority camera
		{
			IMDrawManager.TargetCamera = s_IMDrawCameraList.Count > 0 ? s_IMDrawCameraList[s_IMDrawCameraList.Count - 1] : null;
		}
	}

	public static void SortCameraList()
	{
		s_IMDrawCameraList.Sort((a, b) => a.m_Priority.CompareTo(b.m_Priority));
	}

	public static List<IMDrawCamera> GetCameraList()
	{
		return s_IMDrawCameraList;
	}

	/// <summary>
	/// Set this IMDraw camera is the current draw target.
	/// </summary>
	public void SetTarget ()
	{
		IMDrawManager.TargetCamera = this;
	}

	/// <summary>
	/// Set draw priority for this camera. Affects the draw order of labels.
	/// </summary>
	public int Priority
	{
		set
		{
			m_Priority = value;
			IMDrawManager.SortCameraList();
        }

		get
		{
			return m_Priority;
		}
	}

	#endregion

	public void Initialise ()
	{
		m_Camera = (Camera)gameObject.GetComponent(typeof(Camera));

		GLInit();

		MeshInit();

		GUIInit();
	}

	void OnDestroy ()
	{
		UnregisterCamera(this);
	}

	void Awake ()
	{
		Initialise();
    }

	void OnEnable()
	{
		RegisterCamera(this);
	}

	void OnDisable()
	{
		UnregisterCamera(this);
	}

	///<summary>Called at the start of a Unity frame (requires IMDraw).</summary>
	public void OnBeginFrame (float deltaTime)
	{
		ProfilingReset();
		ProfilingStart();

		if (m_GLCommandList == null) // Handle situations where scripts are recompiled whilst running
		{
			Initialise();
		}

		// Check to see if camera is disabled
		if (m_Camera != null && m_Camera.isActiveAndEnabled)
		{
			m_Disabled &= ~IMDrawDisabledFlag.NOT_RENDERING;
		}
		else
		{
			m_Disabled |= IMDrawDisabledFlag.NOT_RENDERING;
		}

		if (m_MeshCommandList.Count > 0)
		{
			LinkedListNode<IMDrawMeshCommand> node = m_MeshCommandList.First, nextNode;

			// We have run out of vertices so execute remaining commands whilst skipping their draw
			while (node != null)
			{
				node.Value.m_T -= deltaTime;

				if (node.Value.m_T <= 0f)
				{
					nextNode = node.Next;
					node.Value.m_Mesh = null; // Ensure mesh references are cleared
					m_MeshCommandPool.Push(node.Value);  // Return this node to the pool
					m_MeshCommandList.Remove(node); // Remove this node from the list
					node = nextNode;
				}
				else
				{
					node = node.Next;
				}
			}
		}

		if (m_GLCommandList.Count > 0)
		{
			LinkedListNode<IMDrawGLCommand> node = m_GLCommandList.First, nextNode;

			while (node != null)
			{
				node.Value.m_T -= deltaTime;

				if (node.Value.m_T <= 0f)
				{
					nextNode = node.Next;
					m_GLCommandPool.Push(node.Value.Dispose());
					m_GLCommandList.Remove(node);
					node = nextNode;
				}
				else
				{
					node = node.Next;
				}
			}
		}

		if (m_GUICommandList.Count > 0)
		{
			LinkedListNode<IMDrawGUICommand> node = m_GUICommandList.First, nextNode;

			while (node != null)
			{
				node.Value.m_T -= deltaTime;

				if (node.Value.m_T <= 0f)
				{
					nextNode = node.Next;
					m_GUICommandList.Remove(node);
					node.Value.Dispose(this);
					node = nextNode;
				}
				else
				{
					node = node.Next;
				}
			}
		}

		#if UNITY_EDITOR
		m_RefreshInspector = !m_RefreshInspector; // This is used to trick the inspector into updating refrequently
		#endif // UNITY_EDITOR

		ProfilingStop();
	}

	// This should only be called if the camera is rendered
	// Note: if Graphics.DrawMesh is called on a camera that doesn't render, then it gets queued to be rendered the next time the
	// camera is enabled or Camera.Render() is called so we need to be careful.
    void OnPreCull ()
	{
		if (m_MeshCommandList == null) // Handle situations where assembly is recompiled
			return;

		if (IMDrawManager.Instance == null)
		{
			MeshFlushCommands(); // If there is no manager, flush draw commands
			return;
		}

		ProfilingStart();

		MeshDraw();

		m_LabelCount = m_GUICommandList.Count;

		ProfilingStop();
	}

	void OnPostRender ()
	{
		if (m_GLCommandList == null) // Handle situations where assembly is recompiled
			return;

		if (IMDrawManager.Instance == null)
		{
			GLFlushCommands(); // If there is no manager, flush draw commands
			return;
		}

		if (m_Camera == null)
        {
			m_VertIndex = 0;
			m_GLVertexCount = 0;
			return;
		}

		ProfilingStart();

		m_Disabled &= ~IMDrawDisabledFlag.NOT_RENDERING;  // If OnPostRender has been called, we can reset certain disabled flags

		if (m_Disabled == 0)
		{
			GLDraw();
		}

		ProfilingStop();
	}

	void OnGUI ()
	{
		if (m_GUICommandList == null) // Handle situations where assembly is recompiled
			return;

		if (IMDrawManager.Instance == null)
		{
			GUIFlushCommands(); // If there is no manager, flush draw commands
			return;
		}
	}

	/// <summary>Immediately flush all current draw commands.</summary>
	public void FlushImmediate()
	{
		if (m_GLCommandList == null) // Handle situations where scripts are recompiled whilst running
			return;

		GLFlushCommands();
		MeshFlushCommands();
		GUIFlushCommands();
	}

	public float GetCameraDistSqrd(ref Vector3 position)
	{
		float dx = position.x - m_CameraPosition.x;
		float dy = position.y - m_CameraPosition.y;
		float dz = position.z - m_CameraPosition.z;
		return dx * dx + dy * dy + dz * dz;
	}

#if UNITY_EDITOR
	public void EditorDebugInfo (System.Text.StringBuilder sb)
	{
		sb.Append("Instance ID:");
		sb.Append(GetInstanceID());

		sb.Append("\nDisabled: ");
		sb.Append(Convert.ToString((int)m_Disabled, 2));

		// GL info
		if (m_GLCommandList != null)
		{
			sb.Append("\nGL command list: ");
			sb.Append(m_GLCommandList.Count);
			sb.Append("\nGL command pool: ");
			sb.Append(m_GLCommandPool.Count);
			sb.Append("\nGL vertex count: ");
			sb.Append(m_GLVertexCount);
			sb.Append("\nGL primitive count: ");
			sb.Append(m_GLPrimitiveCount);
			sb.Append("\nVertIndex: ");
			sb.Append(m_VertIndex);

			// Mesh info
			sb.Append("\nMesh command list: ");
			sb.Append(m_MeshCommandList.Count);
			sb.Append("\nMesh command pool: ");
			sb.Append(m_MeshCommandPool.Count);
			sb.Append("\nMesh vertex count: ");
			sb.Append(m_MeshVertexCount);
			sb.Append("\nMesh primitive count: ");
			sb.Append(m_MeshPrimitiveCount);

			// GUI info
			sb.Append("\nGUI command list: ");
			sb.Append(m_GUICommandList.Count);
			sb.Append("\nLabel command pool: ");
			sb.Append(m_LabelCommandPool.Count);
			sb.Append("\nLabel count: ");
			sb.Append(m_LabelCount);
			sb.Append("\nLabels visible: ");
			sb.Append(m_VisibleLabelCount);

			#if ENABLE_PROFILING
			sb.Append("\nFrame time: ");
			sb.Append(ProfilingTime);
			#endif // ENABLE_PROFILING
		}
	}
#endif // UNITY_EDITOR

	#region ==================== GL ====================

	private void GLInit ()
	{
		m_VertexArray = new Vector3 [m_MaxGLVertices];
		m_ColorArray = new Color [m_MaxGLVertices];

		m_GLCommandPool = new Stack<IMDrawGLCommand>(INIT_GL_POOL_CAPACITY);
		m_GLCommandList = new LinkedList<IMDrawGLCommand>();
	}

	public IMDrawGLCommand CreateGLCommand ()
	{
		IMDrawGLCommand command = m_GLCommandPool.Count > 0 ? m_GLCommandPool.Pop() : new IMDrawGLCommand();
		m_GLCommandList.AddLast(command.m_ListNode);
		return command;
	}

	private void GLPrepareDraw ()
	{
		m_VertIndex = 0;

		m_ExceededGLVertLimit = false;

		LinkedListNode<IMDrawGLCommand> node = m_GLCommandList.First;
		IMDrawGLCommand command;

		if (m_3DDistanceCull)
		{
			float maxDistSqrd = m_3DDistanceCullMaxDist * m_3DDistanceCullMaxDist;

			while (node != null)
			{
				command = node.Value;

				if ((m_VertIndex + command.m_Verts) <= m_MaxGLVertices) // Check if there are enough verts left to render
				{
					if (command.GetDistSqrd(ref m_CameraPosition) < maxDistSqrd) // Check if the mesh is inside our culling range
					{
						node.Value.Draw(m_VertexArray, m_ColorArray, ref m_VertIndex);
					}

					node = node.Next;
				}
				else
				{
					m_ExceededGLVertLimit = true;
					break; // We have run out of vertices
				}
			}
		}
		else // We are not using 3D distance culling
		{
			while (node != null)
			{
				command = node.Value;

				if ((m_VertIndex + command.m_Verts) <= m_MaxGLVertices) // Check if there are enough verts left to render
				{
					node.Value.Draw(m_VertexArray, m_ColorArray, ref m_VertIndex);
					node = node.Next;
				}
				else
				{
					m_ExceededGLVertLimit = true;
					break; // We have run out of vertices
				}
			}
		}
	}

	private void GLDraw ()
	{
		if (m_MaterialGL == null) // If material is missing, skip
			return;

		ProfilingStart();

		m_GLPrimitiveCount = m_GLCommandList.Count;
		
		GLPrepareDraw();

		// TODO: Multi-threading - Block until outstanding draw requests to be completed

		m_GLVertexCount = m_VertIndex;

		if (m_VertIndex > 0)
		{
			GL.PushMatrix();

			m_MaterialGL.SetPass(0);

			GL.Begin(GL.LINES);

			int vertex = 0;

			while (vertex < m_VertIndex)
			{
				GL.Color(m_ColorArray[vertex]);
				GL.Vertex(m_VertexArray[vertex]);

				++vertex;

				GL.Color(m_ColorArray[vertex]);
				GL.Vertex(m_VertexArray[vertex]);

				++vertex;
			}

			GL.End();

			GL.PopMatrix();

			//m_VertIndex = 0;
		}

		ProfilingStop();
	}

	private void GLFlushCommands ()
	{
		while (m_GLCommandList.Count > 0)
		{
			m_GLCommandPool.Push(m_GLCommandList.Last.Value.Dispose());
			m_GLCommandList.RemoveLast();
		}

		m_GLPrimitiveCount = 0;
		m_GLVertexCount = 0;
	}

	#endregion

	#region ==================== MESH ====================

	private void MeshInit ()
	{
		m_MeshCommandList = new LinkedList<IMDrawMeshCommand>(); // List for active mesh draw commands
		m_MeshCommandPool = new Stack<IMDrawMeshCommand>(INIT_MESH_POOL_CAPACITY); // Pool for mesh draw command objects
	}

	public IMDrawMeshCommand CreateMeshCommand()
	{
		IMDrawMeshCommand command = m_MeshCommandPool.Count > 0 ? m_MeshCommandPool.Pop() : new IMDrawMeshCommand();
		m_MeshCommandList.AddLast(command.m_ListNode);
		return command;
	}

	private void MeshDraw ()
	{
		// Note: if the camera is not rendering, Graphics.DrawMesh requests can accumulate, which will then get executed when the camera is finally rendered
		m_ExceededMeshVertLimit = false;

		m_MeshVertexCount = 0;

		m_MeshPrimitiveCount = m_MeshCommandList.Count;

		if (IMDrawManager.Instance == null || m_Camera == null || m_Disabled != 0 || m_MaterialMesh == null)
        {
			return;
		}

		LinkedListNode<IMDrawMeshCommand> node = m_MeshCommandList.First;

		IMDrawMeshCommand command;

		if (m_3DDistanceCull) // We are using distance culling
		{
			float maxDistSqrd = m_3DDistanceCullMaxDist * m_3DDistanceCullMaxDist;

			while (node != null)
			{
				command = node.Value;

				if ((m_MeshVertexCount + command.m_Verts) <= m_MaxMeshVertices) // Check if there are enough verts left to render
				{
					if (command.GetDistSqrd(ref m_CameraPosition) < maxDistSqrd) // Check if the mesh is inside our culling range
					{
						m_MeshVertexCount += command.m_Verts;

						command.Draw(this);
					}

					node = node.Next;
				}
				else
				{
					m_ExceededMeshVertLimit = true;
					break; // We have run out of vertices
				}
			}

		}
		else // We are not using 3D distance culling
		{
			while (node != null)
			{
				command = node.Value;

				if ((m_MeshVertexCount + command.m_Verts) <= m_MaxMeshVertices) // Check if there are enough verts left to render
				{
					m_MeshVertexCount += command.m_Verts;

					command.Draw(this);
					node = node.Next;
				}
				else
				{
					m_ExceededMeshVertLimit = true;
					break; // We have run out of vertices
				}
			}
		}
	}

	private void MeshFlushCommands ()
	{
		while (m_MeshCommandList.Count > 0)
		{
			m_MeshCommandList.Last.Value.m_Mesh = null; // Ensure this command doesn't reference a mesh
			m_MeshCommandPool.Push(m_MeshCommandList.Last.Value);
			m_MeshCommandList.RemoveLast();
		}

		m_MeshPrimitiveCount = 0;
		m_MeshVertexCount = 0;
    }

	#endregion

	#region ==================== GUI ====================

	private void GUIInit ()
	{
		m_GUICommandList = new LinkedList<IMDrawGUICommand>(); // List for GUI draw command objects
		m_GUI2DDrawList = new List<IMDrawGUICommand>(INIT_LABEL_POOL_CAPACITY); // List for 2D GUI draw command objects
		m_GUI3DDrawList = new List<IMDrawGUICommand>(INIT_LABEL_POOL_CAPACITY); // List for 3D GUI draw command objects
		m_LabelCommandPool = new Stack<IMDrawLabelCommand>(INIT_LABEL_POOL_CAPACITY); // Pool for GUI text draw command objects

		m_FontGUIStyle = new GUIStyle();

		if (m_Font != null)
			m_FontGUIStyle.font = m_Font;

		m_FontGUIStyle.fontStyle = FontStyle.Normal;
		m_FontGUIStyle.fontSize = m_FontSize;
		m_FontGUIStyle.clipping = TextClipping.Overflow;
		m_FontGUIStyle.normal.textColor = Color.white;
	}

	public IMDrawLabelCommand CreateLabelCommand()
	{
		IMDrawLabelCommand command = m_LabelCommandPool.Count > 0 ? m_LabelCommandPool.Pop() : new IMDrawLabelCommand();
		m_GUICommandList.AddLast(command.m_ListNode);
		return command;
	}

	public void Dispose(IMDrawLabelCommand command)
	{
		m_LabelCommandPool.Push(command);
	}

	private void GUIFlushCommands ()
	{
		while (m_GUICommandList.Count > 0)
		{
			m_GUICommandList.Last.Value.Dispose(this);
			m_GUICommandList.RemoveLast();
		}

		m_LabelCount = 0;
		m_VisibleLabelCount = 0;
	}

	private void GUIUpdateLayout ()
	{
		m_FontGUIStyle.fontSize = m_FontSize;
		m_FontGUIStyle.font = m_Font;

		m_ExceededVisibleLabelLimit = false;

		// Reset our draw lists
		m_GUI3DDrawList.Clear();
		m_GUI2DDrawList.Clear();

		m_VisibleLabelCount = 0;

		LinkedListNode<IMDrawGUICommand> node = m_GUICommandList.First, nextNode;
		IMDrawGUICommand command;

		while (node != null)
		{
			nextNode = node.Next;

			command = node.Value;

			if (command.UpdateLayout(this))
			{
				++m_VisibleLabelCount;

				if (m_VisibleLabelCount == m_MaxVisibleLabels) // We have reached our limit of visible labels, so break out of this loop
				{
					m_ExceededVisibleLabelLimit = true;
                    break;
				}

				// Element is visible, so add it to the correct draw list
				if ((command.m_Flags & IMDrawGUICommandFlag.WORLD_SPACE) != 0)  // Element is 3D
				{
					m_GUI3DDrawList.Add(command);
				}
				else // Element is 2D
				{
					m_GUI2DDrawList.Add(command);
				}
			}
			else
			{
				if (command.m_T == 0f) // Element is not visible, so if it is a single frame item, dispose of it
				{
					command.Dispose(this);
					m_GUICommandList.Remove(node);
				}
			}

			node = nextNode;
		}

		/*
		// If there are any labels remaining that we can't draw, remove the ones that are only displayed for a single frame
		while(node != null)
		{
			nextNode = node.Next;

			if (node.Value.m_T == 0f) // Element is not visible, so if it is a single frame item, dispose of it
			{

				node.Value.Dispose(this);
				m_GUICommandList.Remove(node);
			}

			node = nextNode;
		}*/

		if (m_3DLabelSortByDistance)
		{
			// Sort the 3D draw list in descending order so that elements furthest away get drawn first
			m_GUI3DDrawList.Sort((a, b) => b.m_Z.CompareTo(a.m_Z));
		}
	}

	public bool GUIApplyDistanceFade (IMDrawLabelCommand label) // Returns true if we are to skip drawing this label
	{
		if (m_3DLabelFadeOverDistance)
        {
			if (label.m_Z > m_3DLabelMaxFadeDistanceSqrd) // Skip drawing this label since it is beyond the fade max distance
				return true;

			if (m_3DLabelFadeDistanceDelta > 0f && // Ensure an actual fade range has been set, to avoid divide by zero error
				label.m_Z > m_3DLabelMinFadeDistanceSqrd) 
			{
				// Label is between the min and max fade distances, so attenuate the alpha
				label.m_Color.a = label.m_BaseAlpha * (1f - ((float)System.Math.Sqrt(label.m_Z) - m_3DLabelMinFadeDistance) / m_3DLabelFadeDistanceDelta);
			}
		}

		return false;
	}

	/// <summary>This function is responsible for drawing GUI elements and is called from IMDraw.</summary>
	public void GUIDraw ()
	{
		if (m_Disabled != 0 || IMDrawManager.Instance == null)
			return;

		ProfilingStart();

		if (Event.current.type == EventType.Repaint) // OnGUI is called multiple times, but for now we only care about the final Repaint event
		{
			if (m_CameraTransform != null)
			{
				m_CameraPosition = m_CameraTransform.position;
				m_3DLabelMinFadeDistanceSqrd = m_3DLabelMinFadeDistance * m_3DLabelMinFadeDistance;
				m_3DLabelMaxFadeDistanceSqrd = m_3DLabelMaxFadeDistance * m_3DLabelMaxFadeDistance;
				m_3DLabelFadeDistanceDelta = m_3DLabelMaxFadeDistance - m_3DLabelMinFadeDistance;
			}

			GUIUpdateLayout(); // Update positions of GUI elements and build 3d and 2d draw lists

			int i;

			for (i = 0; i < m_GUI3DDrawList.Count; ++i)
			{
				m_GUI3DDrawList[i].Draw(m_FontGUIStyle);
			}

			for (i = 0; i < m_GUI2DDrawList.Count; ++i)
			{
				m_GUI2DDrawList[i].Draw(m_FontGUIStyle);
			}
		}

		ProfilingStop();
	}

	#endregion

	#region HELPER FUNCTIONS

	public void DrawCollider(SphereCollider sphereCollider, ref Color color, float scaleOffset, bool solid)
	{
		Transform colliderTransform = sphereCollider.transform;
		Vector3 position = colliderTransform.position;
		Quaternion rotation = colliderTransform.rotation;
		Vector3 scale = colliderTransform.lossyScale;

		// SphereColliders appear to scale based on the largest component of the transforms final scale....
		float scalar = scale.x > scale.y ? scale.x : scale.y;
		scalar = scalar > scale.z ? scalar : scale.z;
		scale *= 1f + scaleOffset;

		float radius = scalar * sphereCollider.radius;

		IMDrawGLCommand wireSphere = CreateGLCommand();
		wireSphere.m_Type = IMDrawCommandType.WIRE_SPHERE_ROTATED;
		wireSphere.m_Verts = IMDrawVertexCount.WIRE_SPHERE;
		wireSphere.m_P1 = position;
		wireSphere.m_C1 = color;
		wireSphere.m_Rotation = rotation;
		wireSphere.m_Size.x = radius;
		wireSphere.m_T = 0f;

		if (solid)
		{
			color.a *= 0.5f;

			IMDrawMeshCommand meshSphere = CreateMeshCommand();
			meshSphere.m_Type = IMDrawCommandType.SPHERE;
			meshSphere.m_Verts = IMDrawVertexCount.MESH_SPHERE;
			meshSphere.m_Position = position;
			meshSphere.m_Rotation = rotation;
			meshSphere.m_Color = color;
			meshSphere.m_Size = new Vector3(radius, radius, radius);
			meshSphere.m_T = 0f;
		}
	}

	public void DrawCollider(BoxCollider boxCollider, ref Color color, float scaleOffset, bool solid)
	{
		Transform colliderTransform = boxCollider.transform;
		Vector3 position = colliderTransform.position;
		Quaternion rotation = colliderTransform.rotation;
		Vector3 scale = colliderTransform.lossyScale;
		scale *= (1f + scaleOffset);

		scale.Scale(boxCollider.size); // Box colliders are directly scaled by the transform scale

		IMDrawGLCommand wireBox = CreateGLCommand();
		wireBox.m_Type = IMDrawCommandType.WIRE_BOX_ROTATED;
		wireBox.m_Verts = IMDrawVertexCount.WIRE_BOX;
		wireBox.m_P1 = position;
		wireBox.m_Rotation = rotation;
		wireBox.m_C1 = color;
		wireBox.m_Size = scale * 0.5f; // Convert to half extents
		wireBox.m_T = 0f;

		if (solid)
		{
			color.a *= 0.5f;

			IMDrawMeshCommand meshBox = CreateMeshCommand();
			meshBox.m_Type = IMDrawCommandType.BOX;
			meshBox.m_Verts = IMDrawVertexCount.MESH_BOX;
			meshBox.m_Position = position;
			meshBox.m_Rotation = rotation;
			meshBox.m_Color = color;
			meshBox.m_Size = scale;
			meshBox.m_T = 0f;
		}
	}

	public void DrawCollider(CapsuleCollider capsuleCollider, ref Color color, float scaleOffset, bool solid)
	{
		Transform colliderTransform = capsuleCollider.transform;
		Vector3 position = colliderTransform.position;
		Quaternion rotation = colliderTransform.rotation;
		Vector3 scale = colliderTransform.lossyScale;
		scale *= (1f + scaleOffset);

		IMDrawAxis axis = (IMDrawAxis)capsuleCollider.direction;
		float height = capsuleCollider.height;
		float radius = capsuleCollider.radius;

		// Capsules respond differently to transform scale depending on their reference axis
		switch (axis)
		{
			case IMDrawAxis.X:
				height *= scale.x;
				radius *= scale.y > scale.z ? scale.y : scale.z; // Radius is scaled by the larger of y/z scale
				break;

			case IMDrawAxis.Y:
				height *= scale.y;
				radius *= scale.x > scale.z ? scale.x : scale.z; // Radius is scaled by the larger of x/z scale
				break;

			case IMDrawAxis.Z:
				height *= scale.x;
				radius *= scale.x > scale.y ? scale.x : scale.y; // Radius is scaled by the larger of x/y scale
				break;
		}

		IMDrawGLCommand wireCapsule = CreateGLCommand();
		wireCapsule.m_Type = IMDrawCommandType.WIRE_CAPSULE_ROTATED;
		wireCapsule.m_Verts = IMDrawVertexCount.WIRE_CAPSULE;
		wireCapsule.m_P1 = position;
		wireCapsule.m_C1 = color;
		wireCapsule.m_Rotation = rotation;
		wireCapsule.m_Size.x = height;
		wireCapsule.m_Size.y = radius;
		wireCapsule.m_Axis = axis;
		wireCapsule.m_T = 0f;

		if (solid)
		{
			color.a *= 0.5f;

			IMDrawMeshCommand command = CreateMeshCommand();
			command.m_Type = IMDrawCommandType.CAPSULE;
			command.m_Verts = IMDrawVertexCount.MESH_CAPSULE;
			command.m_Position = position;
			command.SetRotation(ref rotation, axis);
			command.m_Size = new Vector3(radius, height, radius);
			command.m_Color = color;
			command.m_T = 0f;
		}
	}

	public void DrawCollider(WheelCollider wheelCollider, ref Color color, float scaleOffset, bool solid)
	{
		// Note: wheel colliders appear to make use of the Y component of the transform scale, however I'm unsure what aspects of the collider properties are actually scaled.
		// This collider drawing function currently ignores the transform scale.

		float radius = wheelCollider.radius * (1f + scaleOffset);
		Vector3 wheelOrigin;
		Quaternion wheelRotation;
		wheelCollider.GetWorldPose(out wheelOrigin, out wheelRotation);

		//Vector3 up = wheelRotation * Vector3.up;
		Vector3 offset = wheelRotation * new Vector3(0f, 0f, radius);

		//Vector3 forceAppPointPos = wheelOrigin + (up * (-radius + wheelCollider.forceAppPointDistance));

		IMDrawGLCommand wireDisc = CreateGLCommand();
		wireDisc.m_Type = IMDrawCommandType.WIRE_DISC_ROTATED;
		wireDisc.m_Verts = IMDrawVertexCount.WIRE_DISC;
		wireDisc.m_P1 = wheelOrigin;
		wireDisc.m_C1 = color;
		wireDisc.m_Rotation = wheelRotation;
		wireDisc.m_Size.x = radius;
		wireDisc.m_Axis = IMDrawAxis.X;
		wireDisc.m_T = 0f;

		CreateGLCommand().InitLine(wheelOrigin + offset, wheelOrigin - offset, ref color);

		if (wheelCollider.suspensionDistance > 0f)
		{
			CreateGLCommand().InitLine(transform.position, wheelOrigin, ref color);
		}

		if (solid)
		{
			IMDrawMeshCommand command = CreateMeshCommand();
			command.m_Type = IMDrawCommandType.DISC;
			command.m_Verts = IMDrawVertexCount.MESH_DISC;
			command.m_Position = wheelOrigin;
			command.SetRotation(ref wheelRotation, IMDrawAxis.X);
			command.m_Size = new Vector3(radius, 1f, radius);
			command.m_Color = color;
			command.m_T = 0f;
		}
	}

	public void DrawCollider(MeshCollider meshCollider, ref Color color, float scaleOffset)
	{
		if (meshCollider.sharedMesh != null)
		{
			Transform transform = meshCollider.transform;
			Mesh mesh = meshCollider.sharedMesh;

			IMDrawMeshCommand command = CreateMeshCommand();
			command.m_Type = IMDrawCommandType.CUSTOM_MESH;
			command.m_Verts = mesh.vertexCount;
			command.m_Mesh = mesh;
			command.m_Position = transform.position;
			command.m_Rotation = transform.rotation;
			command.m_Color = color;
			command.m_T = 0f;

			if (scaleOffset != 0.0f)
			{
				Vector3 scale;
				scale.x = scale.y = scale.z = 1f + scaleOffset;
				command.m_Size = scale;
			}
			else
			{
				command.m_Size.Set(1f,1f,1f);
			}
		}
    }

	#endregion

#if ENABLE_PROFILING
	private System.Diagnostics.Stopwatch m_Stopwatch = new System.Diagnostics.Stopwatch();

	private const float		PROFILING_INTERVAL = 1f;
	private long			m_ProfilingTimeElapsed = 0;
	private int				m_ProfiliingFrames = 0; 
	private float			m_ProfilingTime = PROFILING_INTERVAL;
	private string			m_ProfilingTimeString = string.Empty;

	public string ProfilingTime { get { return m_ProfilingTimeString; } }

#endif // ENABLE_PROFILING

	[System.Diagnostics.Conditional("ENABLE_PROFILING")]
    private void ProfilingReset ()
	{
#if ENABLE_PROFILING
		float deltaTime = Time.unscaledDeltaTime;

		m_ProfilingTime -= deltaTime;
		m_ProfilingTimeElapsed += m_Stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);
		++m_ProfiliingFrames;

		// Interval ended - update GUI text and start new interval
		if (m_ProfilingTime <= 0.0)
		{
			m_ProfilingTimeString = System.String.Format("{0} μs", m_ProfilingTimeElapsed / m_ProfiliingFrames);

			m_ProfilingTime += PROFILING_INTERVAL;

			if (m_ProfilingTime <= 0f)
				m_ProfilingTime = PROFILING_INTERVAL;

			m_ProfilingTimeElapsed = 0;
			m_ProfiliingFrames = 0;
		}

		m_Stopwatch.Reset();
#endif // ENABLE_PROFILING
	}

	[System.Diagnostics.Conditional("ENABLE_PROFILING")]
	private void ProfilingStart ()
	{
#if ENABLE_PROFILING
		m_Stopwatch.Start();
#endif // ENABLE_PROFILING
	}

	[System.Diagnostics.Conditional("ENABLE_PROFILING")]
	private void ProfilingStop ()
	{
#if ENABLE_PROFILING
		m_Stopwatch.Stop();
#endif // ENABLE_PROFILING
	}
}

#if UNITY_EDITOR // ======================================================================================================================================================================

[UnityEditor.CustomEditor(typeof(IMDrawCamera))]
public class IMDrawCameraEditor : IMDrawEditorBase
{
	private static GUIContent LABEL_PRIORITY = new GUIContent("Priority", "Priority affects the draw order of labels. Lower priority will be drawn before higher priority.");
	private static GUIContent LABEL_MATERIALGL = new GUIContent("GL material", "Material used for rendering GL primitives such as line and wire shapes.");
	private static GUIContent LABEL_MATERIALMESH = new GUIContent("Mesh material", "Material used for rendering mesh primitives.");
	private static GUIContent LABEL_FONT = new GUIContent("Font override", "Font used for labels. If no font is specified, the default font is used.");
	private static GUIContent LABEL_FONTSIZE = new GUIContent("Font size", "Label font size.");
	private static GUIContent LABEL_3DDISTANCECULL = new GUIContent("3D cull by distance", "Enable/disable culling of 3D primitives based on their distance from the camera.");
	private static GUIContent LABEL_3DDISTANCECULLMAXDIST = new GUIContent("3D cull max distance", "Culling distance for 3D primitives (if culling is enabled).");

	private static GUIContent LABEL_MAXGLVERTICES = new GUIContent("Max GL vertices", "Maximum number of vertices used for GL based primitives (wireframe and lines).");
	private static GUIContent LABEL_MAXMESHVERTICES = new GUIContent("Max mesh vertices", "Maximum number of vertices used for mesh primitives.");
	private static GUIContent LABEL_MAXVISIBLELABELS = new GUIContent("Max visible labels", "Maximum number of visible labels.");
	private static GUIContent LABEL_MESHLAYER = new GUIContent("Mesh layer", "The target layer for rendered meshes.");

	private static GUIContent LABEL_3DLABELFADEOVERDISTANCE = new GUIContent("Distance fade", "Enable/disable fading of 3D positioned labels based on their distance from the camera. Beyond the maximum distance, 3D labels will be at zero opacity and therefore their rendering will be skipped.");
	private static GUIContent LABEL_3DLABELMINFADEDISTANCE = new GUIContent("Minimum distance", "Minimum fade distance for labels.");
	private static GUIContent LABEL_3DLABELMAXFADEDISTANCE = new GUIContent("Maximum distance", "Maximum fade distance for labels.");
	private static GUIContent LABEL_3DLABELSORTBYDISTANCE = new GUIContent("Sort by distance", "Enable/disable draw order sorting based on the distance of a 3D label from the camera. When enabled, the 3D labels which are closest to the camera will be drawn on top of other labels.");

	private SerializedProperty m_PriorityProperty;

	private SerializedProperty m_MaterialGLProperty;
	private SerializedProperty m_MaterialMeshProperty;

	private SerializedProperty m_FontProperty;
	private SerializedProperty m_FontSizeProperty;

	private SerializedProperty m_MaxGLVerticesProperty;
	private SerializedProperty m_MaxMeshVerticesProperty;
	private SerializedProperty m_MaxVisibleLabelsProperty;
	private SerializedProperty m_MeshLayerProperty;

	private SerializedProperty m_3DDistanceCullProperty; // Enable culling of 3D primitives based on their distance from the camera
	private SerializedProperty m_3DDistanceCullMaxDistProperty; // Culling distance for 3D primitives (if enabled)

	private SerializedProperty m_3DLabelFadeOverDistanceProperty;
	private SerializedProperty m_3DLabelMinFadeDistanceProperty;
	private SerializedProperty m_3DLabelMaxFadeDistanceProperty;
	private SerializedProperty m_3DLabelSortByDistanceProperty;

	private int		m_ExecutionOrder;
	private bool	m_MaterialsFoldout;
	private bool	m_Register, m_Unregister;
	private int		m_Priority;

	void OnEnable() 
	{
		m_PriorityProperty = serializedObject.FindProperty("m_Priority");
		m_Priority = m_PriorityProperty.intValue;

		m_MaterialGLProperty = serializedObject.FindProperty("m_MaterialGL");
		m_MaterialMeshProperty = serializedObject.FindProperty("m_MaterialMesh");

		m_FontProperty = serializedObject.FindProperty("m_Font");
		m_FontSizeProperty = serializedObject.FindProperty("m_FontSize");

		m_MaxGLVerticesProperty = serializedObject.FindProperty("m_MaxGLVertices");
		m_MaxMeshVerticesProperty = serializedObject.FindProperty("m_MaxMeshVertices");
		m_MaxVisibleLabelsProperty = serializedObject.FindProperty("m_MaxVisibleLabels");
		m_MeshLayerProperty = serializedObject.FindProperty("m_MeshLayer");

		m_3DDistanceCullProperty = serializedObject.FindProperty("m_3DDistanceCull");
		m_3DDistanceCullMaxDistProperty = serializedObject.FindProperty("m_3DDistanceCullMaxDist");

		m_3DLabelFadeOverDistanceProperty = serializedObject.FindProperty("m_3DLabelFadeOverDistance");
		m_3DLabelMinFadeDistanceProperty = serializedObject.FindProperty("m_3DLabelMinFadeDistance");
		m_3DLabelMaxFadeDistanceProperty = serializedObject.FindProperty("m_3DLabelMaxFadeDistance");
		m_3DLabelSortByDistanceProperty = serializedObject.FindProperty("m_3DLabelSortByDistance");

		m_ExecutionOrder = GetExecutionOrder();
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		Color currentGUIColor = GUI.color;

		//EventType eventType = Event.current.type;
		bool isPlaying = Application.isPlaying;

		IMDrawCamera component = (IMDrawCamera)target;

		#if ENABLE_DEBUGINFO
		if (isPlaying)
		{
			DebugInfo(component);
        }
		#endif // ENABLE_DEBUGINFO

		bool errorDetected = DisplayErrors(component, isPlaying);

		DisplayWarnings(component, isPlaying);

		if (isPlaying && !errorDetected)
		{
			Space();

			m_SB.Length = 0;
			m_SB.AppendFormat("GL line vertices: {0} / {1}\nGL shape primitives: {2}\nMesh vertices: {3} / {4}\nMesh shape primitives: {5}\nLabel count: {6} (Visible: {7} / {8})",
				component.GLVertexCount,
				component.MaxGLVertices,
				component.GLPrimitiveCount,
				component.MeshVertexCount,
				component.MaxMeshVertices,
				component.MeshPrimitiveCount,
				component.LabelCount,
				component.VisibleLabelCount,
				component.MaxVisibleLabels);

			DrawPanel(m_SB.ToString());
		}

		if (m_ExecutionOrder != 10000)
		{
			if (!isPlaying)
			{
				EditorGUILayout.BeginHorizontal();

				HelpBox("This script must be LAST in the execution order!", MessageType.Warning);

				if (GUILayout.Button("FIX", GUILayout.Width(100f), GUILayout.Height(40f)))
				{
					SetExecutionOrder(10000);
					m_ExecutionOrder = 10000;
				}

				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.HelpBox("This script must be LAST in the execution order!", MessageType.Warning);
			}
		}

		EditorGUILayout.LabelField("General settings", EditorStyles.boldLabel);

		m_PriorityProperty.intValue = EditorGUILayout.IntSlider(LABEL_PRIORITY, m_PriorityProperty.intValue, -100, 100);
		
		if (m_Priority != m_PriorityProperty.intValue) // Detect if priority has changed
		{
			m_Priority = m_PriorityProperty.intValue;
			IMDrawManager.SortCameraList(); // Notify IMDrawManager that the camera list needs to be sorted
		}

		GUI.enabled = !isPlaying;
		m_MaxGLVerticesProperty.intValue = Mathf.Max(EditorGUILayout.IntField(LABEL_MAXGLVERTICES, m_MaxGLVerticesProperty.intValue), 0);
		m_MaxMeshVerticesProperty.intValue = Mathf.Max(EditorGUILayout.IntField(LABEL_MAXMESHVERTICES, m_MaxMeshVerticesProperty.intValue), 0);
		m_MaxVisibleLabelsProperty.intValue = Mathf.Max(EditorGUILayout.IntField(LABEL_MAXVISIBLELABELS, m_MaxVisibleLabelsProperty.intValue), 0);
		GUI.enabled = true;

		m_3DDistanceCullProperty.boolValue = EditorGUILayout.Toggle(LABEL_3DDISTANCECULL, m_3DDistanceCullProperty.boolValue);
		m_3DDistanceCullMaxDistProperty.floatValue = Mathf.Max(EditorGUILayout.FloatField(LABEL_3DDISTANCECULLMAXDIST, m_3DDistanceCullMaxDistProperty.floatValue), 0f);

		m_MeshLayerProperty.intValue = EditorGUILayout.LayerField(LABEL_MESHLAYER, m_MeshLayerProperty.intValue);

		Space();

		EditorGUILayout.LabelField("3D label settings", EditorStyles.boldLabel);

		m_3DLabelFadeOverDistanceProperty.boolValue = EditorGUILayout.Toggle(LABEL_3DLABELFADEOVERDISTANCE, m_3DLabelFadeOverDistanceProperty.boolValue);

		float labelMinDist = m_3DLabelMinFadeDistanceProperty.floatValue;
		float labelMaxDist = m_3DLabelMaxFadeDistanceProperty.floatValue;

		labelMinDist = EditorGUILayout.FloatField(LABEL_3DLABELMINFADEDISTANCE, labelMinDist);
		labelMinDist = Mathf.Clamp(labelMinDist, 0, labelMaxDist);

		labelMaxDist = EditorGUILayout.FloatField(LABEL_3DLABELMAXFADEDISTANCE, labelMaxDist);
		labelMaxDist = Mathf.Max(labelMaxDist, labelMinDist);

		m_3DLabelMinFadeDistanceProperty.floatValue = labelMinDist;
		m_3DLabelMaxFadeDistanceProperty.floatValue = labelMaxDist;

		m_3DLabelSortByDistanceProperty.boolValue = EditorGUILayout.Toggle(LABEL_3DLABELSORTBYDISTANCE, m_3DLabelSortByDistanceProperty.boolValue);

		Space();

		GUILayout.Label("Font settings", EditorStyles.boldLabel);

		EditorGUILayout.PropertyField(m_FontProperty, LABEL_FONT);

		m_FontSizeProperty.intValue = EditorGUILayout.IntSlider(LABEL_FONTSIZE, m_FontSizeProperty.intValue, 1, 128);

		Space();

		if (Foldout("Materials", ref m_MaterialsFoldout))
		{
			GUI.enabled = !isPlaying;
			EditorGUI.indentLevel = EditorGUI.indentLevel + 1;
			EditorGUILayout.PropertyField(m_MaterialGLProperty, LABEL_MATERIALGL);
			EditorGUILayout.PropertyField(m_MaterialMeshProperty, LABEL_MATERIALMESH);
			EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
			GUI.enabled = true;
		}

		if (isPlaying)
		{
			Space();

			if (GUILayout.Button("Flush"))
			{
				component.FlushImmediate(); 
			}
		}

		Space();

		serializedObject.ApplyModifiedProperties();

		GUI.color = currentGUIColor;
	}

	private void LogHelpBox(string text)
	{
		if (m_SB.Length != 0)
		{
			m_SB.Append('\n');
		}

		m_SB.Append(text);
	}

	private bool DisplayErrors(IMDrawCamera component, bool isPlaying) // Return true if there was an error detected
	{
		bool error = false;

		// Warnings and errors
		m_SB.Length = 0;

		//if (isPlaying && (component.DisabledFlag & IMDrawDisabledFlag.UNINITIALISED) != 0)
		//{
		//	LogHelpBox("IMDrawCamera is uninitialised!");
		//}

		if (!component.gameObject.activeSelf)
		{
			LogHelpBox("GameObject is disabled!");
			error = true;
        }

		if (!component.enabled)
		{
			LogHelpBox("IMDrawCamera is disabled!");
			error = true;
		}

		if (component.IsMaterialMissing)
		{
			LogHelpBox("Material is missing!");
			error = true;
		}

		if (isPlaying)
		{
			if (IMDrawManager.Instance == null)
			{
				LogHelpBox("No active IMDrawManager found!");
				error = true;
			}

			if (component.Camera == null)
			{
				LogHelpBox("Camera is missing!");
				error = true;
			}
			else if (!component.Camera.enabled)
			{
				LogHelpBox("Camera is disabled!");
				error = true;
			}
		}

		if (m_SB.Length > 0)
		{
			Space();
			HelpBox(m_SB.ToString(), MessageType.Error);
		}

		return error;
	}

	private void DisplayWarnings(IMDrawCamera component, bool isPlaying)
	{
		m_SB.Length = 0;

		if (isPlaying)
		{
			if (component.ExceededGLVertLimit)
			{
				LogHelpBox("Max GL vertices reached!");
			}

			if (component.ExceededMeshVertLimit)
			{
				LogHelpBox("Max mesh vertices reached!");
			}

			if (component.ExceededVisibleLabelLimit)
			{
				LogHelpBox("Visible label limit reached!");
			}
		}

		if (m_SB.Length > 0)
		{
			Space();
			HelpBox(m_SB.ToString(), MessageType.Warning);
		}
	}

	private void DebugInfo(IMDrawCamera component)
	{
		Space();
		m_SB.Length = 0;
		component.EditorDebugInfo(m_SB);
		DrawPanel(m_SB.ToString());
	}
}

#endif // UNITY_EDITOR
