using UnityEditor;
using UnityEngine;
using Unik;

[CustomEditor(typeof(WorldController))]
public class WorldControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldController controller = (WorldController)target;

        if (GUILayout.Button("Update JSON"))
        {
            JSONInspector.Open(controller.GetModelJson());
            JSONInspector window = EditorWindow.GetWindow<JSONInspector>("JSON");
            window.Refresh();
        }
    }
}
