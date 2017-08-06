using UnityEngine;
using UnityEditor;

namespace Unik
{
    public class JSONSource : EditorWindow
    {
        private static string _source = "";

        private string _searchWord = "";
        private static string _startSource;

        private const string ProSearchColor = "#D7BA7D";
        private const string SearchColor = "#FFF48A";

        public static void Init(string source)
        {
            if (source != null)
                _source = source;
            _startSource = source;
#if UNITY_5
            JSONSource window = GetWindow<JSONSource>( "JSON" );
            string windowIcon = EditorGUIUtility.isProSkin ? "Icons/d_JSON.png" : "Icons/JSON.png";
            window.titleContent = new GUIContent( "JSON", (Texture)EditorGUIUtility.Load( windowIcon ) );
#else
            GetWindow<JSONSource>("JSON");
#endif
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent("Save"), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                Save();
            }
            GUILayout.Space(2);
            if (_searchWord !=
                (_searchWord =
                    EditorGUILayout.TextField(_searchWord, GUI.skin.FindStyle("ToolbarSeachTextField"),
                        GUILayout.ExpandWidth(true))))
            {
                Search();
            }
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
            {
                _searchWord = "";
                GUI.FocusControl("");
                Search();
            }
            EditorGUILayout.EndHorizontal();

            GUIStyle textStyle = EditorStyles.textField;
            textStyle.wordWrap = true;
            textStyle.richText = true;
            textStyle.stretchHeight = true;

            _source = EditorGUILayout.TextField(_source, textStyle);
        }

        private void Save()
        {
            JSONInspector.Save(_source);
        }

        private void Search()
        {
            string s = _startSource;
            string resultString = _startSource;

            if (!string.IsNullOrEmpty(_searchWord))
            {
                resultString = s.Replace(_searchWord,
                    "<color=" + (EditorGUIUtility.isProSkin ? ProSearchColor : SearchColor) + "><b>" + _searchWord +
                    "</b></color>");
            }

            _source = resultString;
        }
    }
}
