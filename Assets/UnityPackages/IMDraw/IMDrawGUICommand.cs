using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum LabelPivot
{
	UPPER_LEFT,
	UPPER_CENTER,
	UPPER_RIGHT,
	MIDDLE_LEFT,
	MIDDLE_CENTER,
	MIDDLE_RIGHT,
	LOWER_LEFT,
	LOWER_CENTER,
	LOWER_RIGHT,
}


public enum LabelAlignment
{
	LEFT,
	CENTER,
	RIGHT,
}


[System.Flags]
public enum IMDrawGUICommandFlag : byte
{
	INITIALISED		= (1 << 0), // Draw command has been initialised
	WORLD_SPACE		= (1 << 1), // Element is positioned in 3D space
	SHADOWED		= (1 << 2), // Element features a drop shadow
}


public abstract class IMDrawGUICommand : IMDrawCommand
{
	public Vector3					m_Position;
	public IMDrawGUICommandFlag		m_Flags;
	public float					m_Z; // This is the depth of the element if it is positioned in world space, used for sorting the draw order

	//public int					m_Priority; // Possible TODO - would priority be useful?

	public LinkedListNode<IMDrawGUICommand> m_ListNode;

	public IMDrawGUICommand ()
	{
		m_ListNode = new LinkedListNode<IMDrawGUICommand>(this);
	}

	public abstract bool UpdateLayout (IMDrawCamera component); // This is called on first update to compute position, returns true if the command is visible

	public abstract void Draw (GUIStyle guiStyle);

	public abstract void Dispose (IMDrawCamera component);

	public bool Is3D { get { return (m_Flags & IMDrawGUICommandFlag.WORLD_SPACE) != 0; } }
}


public class IMDrawLabelCommand : IMDrawGUICommand
{
	public GUIContent		m_Text;
	public float			m_PivotX, m_PivotY; // 0,0 = top left, 1,1 = bottom right
	public float			m_OffsetX, m_OffsetY; // Screen space positional offset if 3D positioned
	public Color			m_Color;
	public float			m_BaseAlpha; // Used with 3D distance fading
	public TextAnchor		m_Anchor;
	public Rect				m_Rect;

	private static Color	m_ShadowColor = new Color(0f, 0f, 0f, 0f);

	public IMDrawLabelCommand () : base ()
	{
		m_Text = new GUIContent ();
	}

	/// <summary>Initialisation for screen space labels.</summary>
	public void Init(
		float x,
		float y,
		ref Color color,
		LabelPivot pivot,
		LabelAlignment alignment,
		IMDrawGUICommandFlag flags,
		string label,
		float duration)
	{
		m_Text.text = label;
		m_Position.x = x;
		m_Position.y = y;
		m_Color = color;
		m_Flags = flags;
		SetAlignment(alignment);
		SetPivot(pivot);
		m_T = duration;
	}

	/// <summary>Initialisation for world space labels.</summary>
	public void Init(
		Vector3 position,
		ref Color color,
		LabelPivot pivot,
		LabelAlignment alignment,
		IMDrawGUICommandFlag flags,
		string label,
		float duration)
	{
		m_Text.text = label;
		m_Position = position;
		m_OffsetX = 0f;
		m_OffsetY = 0f;
		m_Color = color;
		m_BaseAlpha = color.a;
		m_Flags = flags;
		SetAlignment(alignment);
		SetPivot(pivot);
		m_T = duration;
	}

	/// <summary>Initialisation for world space labels.</summary>
	public void Init(
		Vector3 position,
		float offsetX,
		float offsetY,
		ref Color color,
		LabelPivot pivot,
		LabelAlignment alignment,
		IMDrawGUICommandFlag flags,
		string label,
		float duration)
	{
		m_Text.text = label;
		m_Position = position;
		m_OffsetX = offsetX;
		m_OffsetY = offsetY;
		m_Color = color;
		m_BaseAlpha = color.a;
		m_Flags = flags;
		SetAlignment(alignment);
		SetPivot(pivot);
		m_T = duration;
	}

	/// <summary>Compute the position of the label. Return true if the label should be drawn.</summary>
	public override bool UpdateLayout (IMDrawCamera component)
	{
		if ((m_Flags & IMDrawGUICommandFlag.WORLD_SPACE) != 0) // Element is positioned in world space
		{
			Vector3 screenPos = component.Camera.WorldToScreenPoint(m_Position);

			if (screenPos.z <= 0f)
			{
				return false; // Element is behind the camera, if the element is displayed for just one frame, dispose of this command
			}

			m_Z = component.GetCameraDistSqrd(ref m_Position);

			if (component.GUIApplyDistanceFade(this)) // Apply distance fade to this label - if the label is beyond the max fade range, skip drawing it
				return false;

			m_Rect.x = screenPos.x + m_OffsetX;
			m_Rect.y = IMDrawManager.Instance.ScreenHeight - screenPos.y + m_OffsetY;
		}
		else // Element is positioned in screen space
		{
			m_Rect.x = m_Position.x;
			m_Rect.y = m_Position.y;
		}

		//Vector2 size = GUI.skin.label.CalcSize(m_Text);
		Vector2 size = component.FontGUIStyle.CalcSize(m_Text);

		m_Rect.width = size.x;
		m_Rect.height = size.y;

		m_Rect.x -= (m_Rect.width * m_PivotX);
		m_Rect.y -= (m_Rect.height * m_PivotY);

		return (m_Rect.x > -m_Rect.width && m_Rect.x < IMDrawManager.Instance.ScreenWidth && m_Rect.y > -m_Rect.height && m_Rect.y < IMDrawManager.Instance.ScreenHeight);
	}

	

	public override void Draw (GUIStyle guiStyle)
	{
		if (m_Color.a == 0f) // Skip drawing any labels which are completely transparent
			return;

		//GUI.skin.label.alignment = m_Anchor;
		guiStyle.alignment = m_Anchor;

		if ((m_Flags & IMDrawGUICommandFlag.SHADOWED) != 0)
		{
			//GUI.color = new Color(0f, 0f, 0f, m_Color.a);
			m_ShadowColor.a = m_Color.a;
			guiStyle.normal.textColor = m_ShadowColor;
			GUI.Label(new Rect(m_Rect.x + 2f, m_Rect.y + 2f, m_Rect.width, m_Rect.height), m_Text, guiStyle);
		}

		//GUI.color = m_Color;
		guiStyle.normal.textColor = m_Color;
        GUI.Label(m_Rect, m_Text, guiStyle);
	}

	public void SetAlignment (LabelAlignment alignment)
	{
		switch(alignment)
		{
			case LabelAlignment.LEFT: m_Anchor = TextAnchor.UpperLeft; return;
			case LabelAlignment.CENTER: m_Anchor = TextAnchor.UpperCenter; return;
			case LabelAlignment.RIGHT: m_Anchor = TextAnchor.UpperRight; return;
		}
	}

	public void SetPivot (LabelPivot pivot)
	{
		switch(pivot)
		{
			case LabelPivot.UPPER_LEFT:		m_PivotX = 0f;		m_PivotY = 0f; return;
			case LabelPivot.UPPER_CENTER:	m_PivotX = 0.5f;	m_PivotY = 0f; return;
			case LabelPivot.UPPER_RIGHT:	m_PivotX = 1f;		m_PivotY = 0f; return;
			case LabelPivot.MIDDLE_LEFT:	m_PivotX = 0f;		m_PivotY = 0.5f; return;
			case LabelPivot.MIDDLE_CENTER:	m_PivotX = 0.5f;	m_PivotY = 0.5f; return;
			case LabelPivot.MIDDLE_RIGHT:	m_PivotX = 1f;		m_PivotY = 0.5f; return;
			case LabelPivot.LOWER_LEFT:		m_PivotX = 0f;		m_PivotY = 1f; return;
			case LabelPivot.LOWER_CENTER:	m_PivotX = 0.5f;	m_PivotY = 1f; return;
			case LabelPivot.LOWER_RIGHT:	m_PivotX = 1f;		m_PivotY = 1f; return;
		}
	}

	public void SetDefaultPivot ()
	{
		m_PivotX = m_PivotY = 0f;
	}

	public override void Dispose (IMDrawCamera component)
	{
		component.Dispose(this);
    }
}
