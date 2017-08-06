using UnityEngine;
using System.Collections;

[AddComponentMenu("IMDraw/Examples/IMDraw Example"), DisallowMultipleComponent]
public class IMDrawExample : MonoBehaviour
{
	public GameObject [] m_Scene;
	private string m_Title;
	private string m_UnityVersion;
	private int m_CurrentScene = 0;

	void Start ()
	{
		m_Title = string.Format("IMMEDIATE MODE DRAW v{0} DEMO", IMDraw.VERSION);

		m_UnityVersion = string.Format("UNITY v{0}", Application.unityVersion);

		UpdateActiveScene();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}

		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			--m_CurrentScene;

			if (m_CurrentScene < 0)
				m_CurrentScene = m_Scene.Length - 1;

			UpdateActiveScene();
		}

		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			++m_CurrentScene;

			if (m_CurrentScene >= m_Scene.Length)
				m_CurrentScene = 0;

			UpdateActiveScene();
		}
	}

	void LateUpdate ()
	{
		IMDraw.LabelShadowed(10f, 10f, Color.white, LabelPivot.UPPER_LEFT, LabelAlignment.LEFT, m_Title);
		IMDraw.LabelShadowed(10f, 30f, Color.white, LabelPivot.UPPER_LEFT, LabelAlignment.LEFT, m_UnityVersion);
	}

	private void UpdateActiveScene()
	{
		for (int i = 0; i < m_Scene.Length; ++i)
		{
			if (m_Scene[i] != null)
			{
				m_Scene[i].SetActive(i == m_CurrentScene);
			}
		}
	}
}
