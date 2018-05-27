using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

#pragma warning disable 0649 // Ignore false warnings "never assigned to, and will always have it's default value null"

//=====================================================================================================================================================================================================

public enum IMDrawZTest // See UnityEngine.Rendering.CompareFunction
{
	Disabled = 0,
	Never = 1,
	Less = 2,
	Equal = 3,
	/// <summary>Default value.</summary>
	LessEqual = 4,
	Greater = 5,
	NotEqual = 6,
	GreaterEqual = 7,
	Always = 8
}

//=====================================================================================================================================================================================================

/// <summary>
/// Cached shader property IDs used by IMDraw.
/// </summary>
public static class IMDrawSPID
{
	public readonly static int _MainTex;
	public readonly static int _Color;
	public readonly static int _ZTest;

	static IMDrawSPID()
	{
		_MainTex = Shader.PropertyToID("_MainTex");
		_Color = Shader.PropertyToID("_Color");
		_ZTest = Shader.PropertyToID("_ZTest");
	}
}

//=====================================================================================================================================================================================================

/// <summary>
/// <para>IMDraw is the core class that holds references to IMDrawCameras in the scene. In addition to this, the purpose of this class
/// is to catch events at the beginning of a frame before any other scripts.Doing so enables the following to happen:</para>
/// <para>1. To ensure that IMDrawCamera components (which are executed last) can be initialised first before other scripts.</para>
/// <para>2. To signal IMDrawCamera components when a frame begins.</para>
/// <para>3. To catch when Unity has been recompiled whilst playing to ensure IMDrawCamera components are reinitialised.</para>
/// </summary>
[AddComponentMenu("IMDraw/IMDraw Manager"), DisallowMultipleComponent]
public class IMDrawManager : MonoBehaviour
{
	[SerializeField]
	private Mesh m_MeshQuad;

	[SerializeField]
	private Mesh m_MeshBox;

	[SerializeField]
	private Mesh m_MeshPyramid;

	[SerializeField]
	private Mesh m_MeshRhombus;

	[SerializeField]
	private Mesh m_MeshDisc;

	[SerializeField]
	private Mesh m_MeshSphere;

	[SerializeField]
	private Mesh m_MeshCone;

	[SerializeField]
	private Mesh m_MeshCapsuleBody;

	[SerializeField]
	private Mesh m_MeshCapsuleCap;

	[SerializeField]
	private Mesh m_MeshCylinder;

	private bool m_EnteredNewFrame;

	// Note: We keep a list of managers in case there happens to be more than one manager in the scene, we want to be sure IMDraw can always render so long as there is at least one manager active
	private static List<IMDrawManager>	s_IMDrawManagerList = new List<IMDrawManager>();
	private static IMDrawManager		s_ActiveManager; // Currently active manager
	private static IMDrawCamera			s_TargetCamera; // Target camera for IMDraw calls
	private static bool					s_SortCameraList; // Flag for triggering sort of the camera list
	private static Texture2D			s_WhiteTexture;
	private static Texture2D			s_WhiteBorderTexture;
	private static GUIStyle				s_RectOutlineGUIStyle;
	public static IMDrawZTest			s_ZTest = IMDrawZTest.LessEqual;

	public float ScreenWidth { get; private set; } // Note: we cache this for performance reasons
	public float ScreenHeight { get; private set; } // Note: we cache this for performance reasons

	public Mesh MeshQuad { get { return m_MeshQuad; } }
	public Mesh MeshBox { get { return m_MeshBox; } }
	public Mesh MeshPyramid { get { return m_MeshPyramid; } }
	public Mesh MeshRhombus { get { return m_MeshRhombus; } }
	public Mesh MeshDisc { get { return m_MeshDisc; } }
	public Mesh MeshSphere { get { return m_MeshSphere; } }
	public Mesh MeshCone { get { return m_MeshCone; } }
	public Mesh MeshCapsuleBody { get { return m_MeshCapsuleBody; } }
	public Mesh MeshCapsuleCap { get { return m_MeshCapsuleCap; } }
	public Mesh MeshCylinder { get { return m_MeshCylinder; } }
	public bool IsMeshMissing { get { return m_MeshQuad == null || m_MeshBox == null || m_MeshPyramid == null || m_MeshRhombus == null || m_MeshDisc == null || m_MeshSphere == null || m_MeshCone == null || m_MeshCapsuleBody == null || m_MeshCylinder == null; } }

	public static bool IsGLDrawDisabled  { get { return Instance == null || s_TargetCamera == null || s_ZTest == IMDrawZTest.Never; } } // GL disabled
	public static bool IsMeshDrawDisabled { get { return Instance == null || s_TargetCamera == null || s_ZTest == IMDrawZTest.Never; } } // Mesh disabled
	public static bool IsTextMeshDrawDisabled { get { return Instance == null || s_TargetCamera == null || s_TargetCamera.TextMeshComponent.IsDrawDisabled || s_ZTest == IMDrawZTest.Never; } } // Text mesh disabled
	public static bool IsGUIDrawDisabled { get { return Instance == null || s_TargetCamera == null; } } // GUI disabled

	#region ========== MANAGER INSTANCE MANAGEMENT ==========

	private static void Register(IMDrawManager manager)
	{
		s_IMDrawManagerList.Add(manager);
		s_ActiveManager = s_IMDrawManagerList[0];
	}

	private static void Unregister(IMDrawManager manager)
	{
		s_IMDrawManagerList.Remove(manager);
		s_ActiveManager = s_IMDrawManagerList.Count > 0 ? s_IMDrawManagerList[0] : null; // Ensure the active manager is valid
	}

	public static IMDrawManager Instance
	{
		get
		{
			return s_ActiveManager;
		}
	}

	public static IMDrawCamera TargetCamera
	{
		set
		{
			s_TargetCamera = value;
		}

		get
		{
			return s_TargetCamera;
		}
	}

	public static void SortCameraList ()
	{
		s_SortCameraList = true;
	}

	#endregion

	public void ValidateExecutionOrder ()
	{
		#if UNITY_EDITOR
		MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)this);

		if (script != null && MonoImporter.GetExecutionOrder(script) != -10000)
		{
			MonoImporter.SetExecutionOrder(script, -10000);
		}
		#endif // UNITY_EDITOR
	}

	public void Reset ()
	{
		#if UNITY_EDITOR
		ValidateExecutionOrder(); // When this script is added, ensure it is set to be executed first
		#endif // UNITY_EDITOR
	}

	void OnEnable ()
	{
		Register(this);
    }

	void OnDisable()
	{
		Unregister(this);
	}

	void OnDestroy()
	{
		Unregister(this);
	}

	void OnApplicationQuit ()
	{
		//Debug.Log("OnApplicationQuit");
		DestroyTextures();
    }

	void FixedUpdate ()
	{
		if (IMDrawManager.Instance != this)
			return;

		if (s_SortCameraList)
		{
			IMDrawCamera.SortCameraList();
			s_SortCameraList = false;
        }

		//m_InsideFixedUpdate = true;

		// FixedUpdate is called before Update, however it is not guaranteed to be called so instead it will be caught in Update
		if (!m_EnteredNewFrame)
		{
			OnBeginFrame();
			
			// Notify XDraw component 
			m_EnteredNewFrame = true;
		}
	}

	void Update ()
	{
		if (IMDrawManager.Instance != this)
			return;

		//m_InsideFixedUpdate = false;

		// If FixedUpdate is skipped this frame, we catch the beginning of the frame here
		if (!m_EnteredNewFrame)
		{
			OnBeginFrame();
        }

		m_EnteredNewFrame = false;
	}

	private void OnBeginFrame ()
	{
		ScreenWidth = Screen.width;
		ScreenHeight = Screen.height;

		float deltaTime = Time.deltaTime;

		List<IMDrawCamera> cameraList = IMDrawCamera.GetCameraList();

		for (int i = 0; i < cameraList.Count; ++i)
		{
			if (cameraList[i] != null)
			{
				cameraList[i].OnBeginFrame(deltaTime);
			}
		}
	}

	void OnGUI ()
	{
		if (IMDrawManager.Instance != this)
			return;

		List<IMDrawCamera> cameraList = IMDrawCamera.GetCameraList();

		for (int i = 0; i < cameraList.Count; ++i)
			cameraList[i].GUIDraw();
    }

	private IMDrawCamera GetCamera(int instanceID)
	{
		if (instanceID != 0)
		{
			List<IMDrawCamera> cameraList = IMDrawCamera.GetCameraList();

			for (int i = 0; i < cameraList.Count; ++i)
			{
				if (instanceID == cameraList[i].GetInstanceID())
				{
					return cameraList[i];
				}
			}
		}

		return null;
	}

	/// <summary>Flush draw commands on the default camera.</summary>
	public void Flush()
	{
		if (TargetCamera != null)
		{
			TargetCamera.FlushImmediate(); 
        }
	}
	
	/// <summary>Flush draw commands on all cameras.</summary>
	public void FlushAll ()
	{
		List<IMDrawCamera> cameraList = IMDrawCamera.GetCameraList();

		for (int i = 0; i < cameraList.Count; ++i)
		{
			cameraList[i].FlushImmediate();
		}
    }

	#region ========== STATIC RESOURCES ==========

	private static void DestroyTextures()
	{
		if (s_WhiteTexture != null)
		{
			UnityEngine.Object.DestroyImmediate(s_WhiteTexture);
		}

		if (s_WhiteBorderTexture != null)
		{
			UnityEngine.Object.DestroyImmediate(s_WhiteBorderTexture);
		}
	}

	public static Texture2D WhiteTexture
	{
		get
		{
			if (s_WhiteTexture == null)
			{
				s_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
				s_WhiteTexture.filterMode = FilterMode.Point;
				s_WhiteTexture.hideFlags = HideFlags.HideAndDontSave;
				#if UNITY_EDITOR
				s_WhiteTexture.name = "IMDraw white texture";
				#endif // UNITY_EDITOR
				s_WhiteTexture.SetPixel(0, 0, new Color (1f,1f,1f,1f));
				s_WhiteTexture.Apply();
			}

			return s_WhiteTexture;
		}
	}

	private static readonly Color32 COLOR32_WHITE = new Color32 (255,255,255,255);
	private static readonly Color32 COLOR32_CLEAR = new  Color32(0,0,0,0);

	private static readonly Color32 [] s_BorderTexturePixels = new Color32 [] {
		COLOR32_WHITE, COLOR32_WHITE, COLOR32_WHITE, COLOR32_WHITE,
		COLOR32_WHITE, COLOR32_CLEAR, COLOR32_CLEAR, COLOR32_WHITE,
		COLOR32_WHITE, COLOR32_CLEAR, COLOR32_CLEAR, COLOR32_WHITE,
		COLOR32_WHITE, COLOR32_WHITE, COLOR32_WHITE, COLOR32_WHITE };

	public static Texture2D WhiteBorderTexture
	{
		get
		{
			if (s_WhiteBorderTexture == null)
			{
				s_WhiteBorderTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
				s_WhiteBorderTexture.filterMode = FilterMode.Point;
				s_WhiteBorderTexture.hideFlags = HideFlags.HideAndDontSave;
				#if UNITY_EDITOR
				s_WhiteBorderTexture.name = "IMDraw white border texture";
				#endif // UNITY_EDITOR
				s_WhiteBorderTexture.SetPixels32(s_BorderTexturePixels);
				s_WhiteBorderTexture.Apply();
			}

			return s_WhiteBorderTexture;
		}
	}

	public static GUIStyle GUIStyleOutlineRect
	{
		get
		{
			if (s_RectOutlineGUIStyle == null)
			{
				s_RectOutlineGUIStyle = new GUIStyle();
				s_RectOutlineGUIStyle.normal.background = WhiteBorderTexture;
                s_RectOutlineGUIStyle.border = new RectOffset(2,2,2,2);
			}

			return s_RectOutlineGUIStyle;
        }
	}

	#endregion

#if UNITY_EDITOR
	// Here we handle reload of scripts (usually the result of recompiling during play)
	[UnityEditor.Callbacks.DidReloadScripts]
	private static void OnScriptsReloaded()
	{
		if (Application.isPlaying)
		{
			// Destroy all IMDrawTextMesh in the scene. When the scripts are reloaded,
			// the IMDrawTextMeshComponent loses its references to existing IMDrawTextMesh objects in the scene and must reinitialise.
			// This basically destroys all orphaned objects.
			IMDrawTextMesh.DestroyAllInstances();
		}
	}
#endif // UNITY_EDITOR
}

//=====================================================================================================================================================================================================

public abstract class IMDrawCommand
{
	protected static readonly Quaternion AXIS_X_ROTATION = Quaternion.Euler(-90f, -90f, 0f);
	protected static readonly Quaternion AXIS_Z_ROTATION = Quaternion.Euler(90f, 0f, 0f);

	public float m_T; // Time remaining

	public static void SetRotationAxis (out Quaternion outRotation, ref Quaternion inRotation, IMDrawAxis axis)
	{
		// Assumes Y-axis is default
		switch (axis)
		{
			case IMDrawAxis.X: outRotation = inRotation * AXIS_X_ROTATION; break;
			//case IMDrawAxis.Y: outRotation = inRotation; break;
			case IMDrawAxis.Z: outRotation = inRotation * AXIS_Z_ROTATION; break;
			default: outRotation = inRotation; break;
		}
	}
}

#if UNITY_EDITOR // ===================================================================================================================================================================================

// Note: Unity appears to leak any active inspector Editor objects if the game is recompiled whilst playing.
// This results in "The referenced script on this Behaviour is missing!" messages when compilation is finished.
// There is no workaround for this, but otherwise it is fairly harmless.

public abstract class IMDrawEditorBase : Editor
{
	protected static GUIStyle					m_PanelStyle; 
	protected static GUIStyle					m_FoldoutStyle;

	private static GUIStyle						m_HelpboxStyle;
	private static GUIContent					m_HelpBoxMessage;

	private delegate Texture2D EditorGUIUtilityGetHelpIcon (MessageType messageType);
	private static EditorGUIUtilityGetHelpIcon s_GetHelpIcon;

	protected static System.Text.StringBuilder m_SB;

	static IMDrawEditorBase()
	{
		// Expose hidden internal EditorGUIUtility.GetHelpIcon function using reflection so we can use it for our custom helpbox
		Type type = typeof(EditorGUIUtility);
		System.Reflection.MethodInfo dynMethod = type.GetMethod("GetHelpIcon",
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Static);

		if (dynMethod != null)
		{
			s_GetHelpIcon = (EditorGUIUtilityGetHelpIcon)Delegate.CreateDelegate(typeof(EditorGUIUtilityGetHelpIcon), dynMethod);
		}

		m_HelpBoxMessage = new GUIContent();

		m_SB = new System.Text.StringBuilder(64);
	}

	private void InitPanelStyle ()
	{
		if (m_PanelStyle != null)
			return;

		m_PanelStyle = new GUIStyle(GUI.skin.GetStyle("HelpBox"));
		m_PanelStyle.richText = false;
		m_PanelStyle.fontStyle = FontStyle.Bold;
		m_PanelStyle.normal.textColor = Color.white;
	}

	private void InitFoldoutStyle ()
	{
		if (m_FoldoutStyle != null)
			return;

		m_FoldoutStyle = new GUIStyle(EditorStyles.toolbarButton);
		m_FoldoutStyle.fontSize = 11;
	}

	protected void DrawPanel (string text, int fontSize = 13)
	{
		InitPanelStyle();

		m_PanelStyle.alignment = TextAnchor.UpperLeft;
		m_PanelStyle.fontSize = fontSize;
		EditorGUILayout.LabelField(text, m_PanelStyle);
	}

	protected void DrawPanel(string text, TextAnchor anchor, int fontSize = 13)
	{
		InitPanelStyle();

		m_PanelStyle.alignment = anchor;
		m_PanelStyle.fontSize = fontSize;
		EditorGUILayout.LabelField(text, m_PanelStyle);
	}

	protected bool Foldout (string label, ref bool foldout)
	{
		InitFoldoutStyle();

		if (GUILayout.Button(label, m_FoldoutStyle))
		{
			foldout = !foldout;
		}

		GUI.Label(GUILayoutUtility.GetLastRect(), foldout ? "▼" : "►");

		return foldout;
	}

	protected void Header (string label) 
	{
		InitFoldoutStyle();

		GUILayout.Label(label, m_FoldoutStyle);
	}

	protected void SetExecutionOrder (int order)
	{
		MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);

		if (script != null)
		{
			MonoImporter.SetExecutionOrder(script, order);
			return;
		}

		Debug.LogWarning(string.Format("IMDrawManager: Script for {0} not found!", target.name));
	}

	protected int GetExecutionOrder ()
	{
		MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);

		if (script != null)
		{
			return MonoImporter.GetExecutionOrder(script);
		}

		Debug.LogWarning(string.Format("IMDrawManager: Script for {0} not found!", target.name));

		return 0;
	}

	protected void HelpBox (string message, MessageType messageType)
	{
		if (m_HelpboxStyle == null)
		{
			m_HelpboxStyle = new GUIStyle(GUI.skin.GetStyle("HelpBox"));
			m_HelpboxStyle.fontSize = 11;
			m_HelpboxStyle.normal.textColor = Color.red;
			m_HelpboxStyle.fontStyle = FontStyle.Bold;
		}

		switch(messageType)
		{
			case MessageType.Error: m_HelpboxStyle.normal.textColor = Color.red; break;
			case MessageType.Warning: m_HelpboxStyle.normal.textColor = Color.yellow; break;
			default: m_HelpboxStyle.normal.textColor = Color.white; break;
		}

		m_HelpBoxMessage.text = message;
		m_HelpBoxMessage.image = s_GetHelpIcon != null ? s_GetHelpIcon(messageType) : null;

		EditorGUILayout.LabelField(GUIContent.none, m_HelpBoxMessage, m_HelpboxStyle, null);// new GUILayoutOption[0]);
	}

	protected void LogFunction (string functionName)
	{
		Debug.Log(string.Format("#{0} {1}.{2}", GetInstanceID(), GetType().ToString(), functionName));
	}

	protected void Space ()
	{
		GUILayout.Space(EditorGUIUtility.singleLineHeight / 2);
	}
}

[UnityEditor.CustomEditor(typeof(IMDrawManager))]
public class IMDrawManagerEditor : IMDrawEditorBase
{
	private const string MESH_ASSET_PATH = "Assets/IMDraw/Mesh/";

	private SerializedProperty						m_MeshQuadProperty;
	private SerializedProperty						m_MeshBoxProperty;
	private SerializedProperty						m_MeshPyramidProperty;
	private SerializedProperty						m_MeshRhombusProperty;
	private SerializedProperty						m_MeshDiscProperty;
	private SerializedProperty						m_MeshSphereProperty;
	private SerializedProperty						m_MeshConeProperty;
	private SerializedProperty						m_MeshCapsuleBodyProperty;
	private SerializedProperty						m_MeshCapsuleCapProperty;
	private SerializedProperty						m_MeshCylinderProperty;

	private string									m_Title;
	private bool									m_MeshFoldout;
	private int										m_ExecutionOrder;

	private static GUIContent						m_GUIContent = new GUIContent ();

	void OnEnable ()
	{
		m_Title = string.Format("Immediate Mode Draw v{0}", IMDraw.VERSION);

		m_MeshQuadProperty = serializedObject.FindProperty("m_MeshQuad");
		m_MeshBoxProperty = serializedObject.FindProperty("m_MeshBox");
		m_MeshPyramidProperty = serializedObject.FindProperty("m_MeshPyramid");
		m_MeshRhombusProperty = serializedObject.FindProperty("m_MeshRhombus");
		m_MeshDiscProperty = serializedObject.FindProperty("m_MeshDisc");
		m_MeshSphereProperty = serializedObject.FindProperty("m_MeshSphere");
		m_MeshConeProperty = serializedObject.FindProperty("m_MeshCone");
		m_MeshCapsuleBodyProperty = serializedObject.FindProperty("m_MeshCapsuleBody");
		m_MeshCapsuleCapProperty = serializedObject.FindProperty("m_MeshCapsuleCap");
		m_MeshCylinderProperty = serializedObject.FindProperty("m_MeshCylinder");

		m_ExecutionOrder = GetExecutionOrder();
	}

	public override void OnInspectorGUI()
	{
		bool isPlaying = Application.isPlaying;

		IMDrawManager component = (IMDrawManager)target;

		if (isPlaying)
		{
			if (IMDrawManager.Instance == null)
			{
				HelpBox("There are no active IMDrawManagers in this scene!", MessageType.Error);
				return;
			}
			else if (IMDrawManager.Instance != component)
			{
				HelpBox("There are multiple IMDrawManagers in this scene!", MessageType.Error);
				return;
			}
		}

		serializedObject.Update();

		DrawPanel(m_Title, TextAnchor.MiddleCenter);

		DisplayErrors(component);

		if (m_ExecutionOrder != -10000)
		{
			if (!isPlaying)
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.HelpBox("This script must be FIRST in the execution order!", MessageType.Warning);

				if (GUILayout.Button("FIX", GUILayout.Width(100f), GUILayout.Height(40f)))
				{
					SetExecutionOrder(-10000);
					m_ExecutionOrder = -10000;
				}

				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.HelpBox("This script must be FIRST in the execution order!", MessageType.Warning);
			}
		}

		DrawCameraList(component, isPlaying);

		Space();

		if (Foldout("Meshes", ref m_MeshFoldout))
		{
			GUI.enabled = !isPlaying;
			EditorGUI.indentLevel = EditorGUI.indentLevel + 1;
			PropertyField(m_MeshQuadProperty, "Quad");
			PropertyField(m_MeshBoxProperty, "Box");
			PropertyField(m_MeshPyramidProperty, "Pyramid");
			PropertyField(m_MeshRhombusProperty, "Rhombus");
			PropertyField(m_MeshDiscProperty, "Disc");
			PropertyField(m_MeshSphereProperty, "Sphere");
			PropertyField(m_MeshConeProperty, "Cone");
			PropertyField(m_MeshCapsuleBodyProperty, "Capsule body");
			PropertyField(m_MeshCapsuleCapProperty, "Capsule cap");
			PropertyField(m_MeshCylinderProperty, "Cylinder");
			EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
			GUI.enabled = true;
		}

		if (component.IsMeshMissing)
		{
			GUILayout.Space(4f);

			if (GUILayout.Button("Assign missing meshes as default"))
			{
				AssignMissingMeshAsDefault(m_MeshQuadProperty, "IMDrawQuad");
				AssignMissingMeshAsDefault(m_MeshBoxProperty, "IMDrawBox");
				AssignMissingMeshAsDefault(m_MeshPyramidProperty, "IMDrawPyramid");
				AssignMissingMeshAsDefault(m_MeshRhombusProperty, "IMDrawRhombus");
				AssignMissingMeshAsDefault(m_MeshDiscProperty, "IMDrawDisc");
				AssignMissingMeshAsDefault(m_MeshSphereProperty, "IMDrawSphere");
				AssignMissingMeshAsDefault(m_MeshConeProperty, "IMDrawCone");
				AssignMissingMeshAsDefault(m_MeshCapsuleBodyProperty, "IMDrawCapsuleBody");
				AssignMissingMeshAsDefault(m_MeshCapsuleCapProperty, "IMDrawCapsuleCap");
				AssignMissingMeshAsDefault(m_MeshCylinderProperty, "IMDrawCylinder");
			}
		}

		if (isPlaying && IMDrawManager.TargetCamera != null)
		{
			Space();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(string.Format("Flush Target: {0}", IMDrawManager.TargetCamera.name)))
			{
				component.Flush();
			}
			
			if (GUILayout.Button("Flush All"))
			{
				component.FlushAll();
			
			}

			EditorGUILayout.EndHorizontal();
		}

		Space();

		serializedObject.ApplyModifiedProperties();
	}

	private void AssignMissingMeshAsDefault (SerializedProperty property, string meshName)
	{
		if (property.objectReferenceValue == null)
		{
			string fullPath = MESH_ASSET_PATH + meshName + ".obj";

#if UNITY_5_0_0
			Mesh resource = Resources.LoadAssetAtPath<Mesh>(fullPath);
#else
			Mesh resource = AssetDatabase.LoadAssetAtPath<Mesh>(fullPath);
#endif

			if (resource != null)
			{
				property.objectReferenceValue = resource;
				return;
			}
			
			Debug.LogError(string.Format("IMDrawManager: Unable to assign missing mesh as {0}, mesh not found!", fullPath));
		}
	}

	private void PropertyField(SerializedProperty property, string label)
	{
		m_GUIContent.text = label;
		EditorGUILayout.PropertyField(property, m_GUIContent);
	}

	private void LogHelpBox (string text)
	{
		if (m_SB.Length != 0)
		{
			m_SB.Append('\n');
		}

		m_SB.Append(text);
	}

	private void DisplayErrors (IMDrawManager component)
	{
		// Warnings and errors
		m_SB.Length = 0;

		if (!component.isActiveAndEnabled)
		{
			LogHelpBox("IMDrawManager is disabled!");
		}

		if (component.IsMeshMissing)
		{
			LogHelpBox("Mesh reference is missing!");
		}

		if (m_SB.Length > 0)
		{
			Space();

			HelpBox(m_SB.ToString(), MessageType.Error);
		}
	}

	private void DrawCameraList(IMDrawManager component, bool isPlaying)
	{
		if (!isPlaying) // Unity isn't running
		{
			return;
		}

		Space();

		List<IMDrawCamera> cameraList = IMDrawCamera.GetCameraList();

		if (cameraList.Count > 0)
		{
			IMDrawCamera child;

			for (int index = 0; index < cameraList.Count; ++index)
			{
				child = cameraList[index];

				if (child == null)
					continue;

				EditorGUILayout.BeginHorizontal();

				if (isPlaying)
				{
					// Indicate if this camera is the current target
					if (child == IMDrawManager.TargetCamera)
					{
						GUILayout.Label("►", EditorStyles.whiteLabel, GUILayout.Width(15f));
					}
					else
					{
						GUILayout.Space(20f);
					}
				}

				GUILayout.Label(string.Format("Priority:{0}", child.Priority), EditorStyles.whiteLabel, GUILayout.Width(100f));

				GUILayout.Label(child != null ? child.name : "null", EditorStyles.whiteLabel);

				if (IMDrawManager.TargetCamera != child)
				{
					if (GUILayout.Button("Set as target", GUILayout.Width(90f)))
					{
						IMDrawManager.TargetCamera = child;
					}
				}

				EditorGUILayout.EndHorizontal();
			}
		}
		else
		{
			GUILayout.Label("No active IMDrawCamera components found.", EditorStyles.whiteLabel);
		}
	}
}

#endif // UNITY_EDITOR