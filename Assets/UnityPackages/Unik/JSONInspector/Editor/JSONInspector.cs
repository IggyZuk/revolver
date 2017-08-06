using System;
using System.Collections.Generic;
using Unik.JSON;
using UnityEditor;
using UnityEngine;

namespace Unik
{
    public class JSONInspector : EditorWindow
    {
        private static string _stringJson = "";
        private const int Space = 20;
        private const int StartX = 10;
        private const int StartY = 15;
        private const int FieldHeight = 16;

        private const string ProStringColor = "#E27A5E";
        private const string ProIntColor = "#57A64A";
        private const string ProBoolColor = "#569CD6";
        private const string ProSearchColor = "#D7BA7D";
        private const string StringColor = "#923318";
        private const string IntColor = "#32662A";
        private const string BoolColor = "#1F5A8A";
        private const string SearchColor = "#FFF48A";

        private int _y = 1;
        private float _viewY = 15;
        private Vector2 _scrollPosition;
        private Field _rootField;
        private static Action<string> _callback;

        private string _searchWord = "";

        [MenuItem("Window/Unik/JSON Inspector")]
        private static void Init()
        {
#if UNITY_5
            JSONInspector window = GetWindow<JSONInspector>( "JSON" );
            string windowIcon = EditorGUIUtility.isProSkin ? "Icons/d_JSON.png" : "Icons/JSON.png";
            window.titleContent = new GUIContent( "JSON", (Texture)EditorGUIUtility.Load( windowIcon ) );
#else
            GetWindow<JSONInspector>("JSON");
#endif
        }

        /// <summary>
        /// Open JSONInspector
        /// </summary>
        /// <param name="json">json source</param>
        /// <param name="callback">return json string after save</param>
        public static void Open(string json, Action<string> callback = null)
        {
            _stringJson = json;
            _callback = callback;
            Init();
        }

        public static void Save(string json)
        {
            _stringJson = json;
            if (_callback != null)
                _callback(_stringJson);
        }

        void OnEnable()
        {
            _rootField = TryParse(_stringJson, "ROOT", null);
            _rootField.IsOpened = true;
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string refreshIcon = EditorGUIUtility.isProSkin ? "icons/d_Refresh.png" : "icons/Refresh.png";
            if (GUILayout.Button(new GUIContent((Texture2D) EditorGUIUtility.Load(refreshIcon), "Refresh"),
                EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                Refresh();
            }
            if (GUILayout.Button(new GUIContent("Expand all"), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                ExpandReduce(_rootField, true);
            }
            if (GUILayout.Button(new GUIContent("Reduce all"), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                ExpandReduce(_rootField, false);
            }
            GUILayout.Space(2);
            if (_searchWord !=
                (_searchWord =
                    EditorGUILayout.TextField(_searchWord, GUI.skin.FindStyle("ToolbarSeachTextField"),
                        GUILayout.ExpandWidth(true))))
            {
                Search(_rootField);
            }
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
            {
                _searchWord = "";
                GUI.FocusControl("");
            }
            if (GUILayout.Button(new GUIContent("Source"), EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                ShowSource();
            }
            EditorGUILayout.EndHorizontal();

            _y = 1;

            _scrollPosition = GUI.BeginScrollView(new Rect(0, 17, position.width, position.height - 18), _scrollPosition,
                new Rect(0, 25, position.width - 25, _viewY));
            _viewY = 15;
            ShowField(_rootField);
            GUI.EndScrollView();
        }

        public void Refresh()
        {
            _rootField = TryParse(_stringJson, "ROOT", null);
            _rootField.IsOpened = true;
        }

        private void ShowSource()
        {
            JSONSource.Init(_stringJson);
        }

        private void ExpandReduce(Field field, bool expend)
        {
            if (field == null)
                return;

            field.IsOpened = expend;
            if (field.Childs.Count <= 0)
                return;

            foreach (Field f in field.Childs)
                ExpandReduce(f, expend);
        }

        private void Search(Field field)
        {
            if (string.IsNullOrEmpty(_searchWord.Trim()))
                return;

            if (field == null)
                return;

            if (field.Value.ToLower().Contains(_searchWord))
            {
                field.IsOpened = true;
                Field p = field.Parent;
                while (p != null)
                {
                    p.IsOpened = true;
                    p = p.Parent;
                }
                _rootField.IsOpened = true;
            }

            if (field.Childs.Count <= 0)
                return;

            foreach (Field f in field.Childs)
                Search(f);
        }

        private void ShowField(Field field)
        {
            if (field == null)
                return;

            bool search = !string.IsNullOrEmpty(_searchWord.Trim()) && field.Value.ToLower().Contains(_searchWord);
            string text = (field.HasPlus ? (field.IsOpened ? "▼ " : "► ") : "") +
                          (search
                              ? "<color=" + (EditorGUIUtility.isProSkin ? ProSearchColor : SearchColor) + "><b>"
                              : "") + field.Value + (search ? "</b></color>" : "");
            GUIStyle style = EditorStyles.label;
            style.richText = true;
            _viewY += FieldHeight;

            if (GUI.Button(new Rect(field.X, StartY + _y*FieldHeight, 500, FieldHeight), text, style))
            {
                field.IsOpened = !field.IsOpened;
            }

            if (!field.IsOpened || field.Childs.Count <= 0)
                return;

            foreach (Field f in field.Childs)
            {
                _y++;
                ShowField(f);
            }
        }

        private Field TryParse(string json, string fieldName, Field parent)
        {
            JSONNode obj;
            JSONArray arr = null;

            try
            {
                obj = JSONNode.Parse(json);
            }
            catch
            {
                obj = null;
            }

            if (obj == null || obj.AsArray != null)
            {
                try
                {
                    arr = JSONArray.Parse(json).AsArray;
                }
                catch
                {
                    arr = null;
                }
            }

            if (obj == null && arr == null)
                return new Field();

            if (arr != null)
            {
                Field arrField = new Field
                {
                    Value = fieldName,
                    HasPlus = true,
                    Parent = parent
                };
                arrField.X = arrField.Parent.X + Space;
                int i = 0;
                foreach (JSONNode t in arr)
                {
                    if (t.Count == 0)
                    {
                        Field field = new Field
                        {
                            Value = t.ToString(),
                            HasPlus = false,
                            Parent = arrField
                        };
                        field.X = field.Parent.X + Space;
                        arrField.Childs.Add(field);
                    }
                    else
                    {
                        Field field = TryParse(t.ToString(), "[" + i + "]", arrField);
                        field.X = field.Parent.X + Space;
                        arrField.Childs.Add(field);
                    }
                    i++;
                }
                return arrField;
            }

            Field returnField = new Field
            {
                Value = fieldName,
                HasPlus = true,
                Parent = parent,
                X = parent != null ? parent.X + Space : StartX
            };

            foreach (KeyValuePair<string, JSONNode> pair in obj.AsObject)
            {
                if (pair.Value.Count == 0)
                {
                    Field field = new Field();
                    string value;

                    if (pair.Value.IsBool)
                        value = "<color=" + (EditorGUIUtility.isProSkin ? ProBoolColor : BoolColor) + ">" + pair.Value +
                                "</color>";
                    else if (pair.Value.IsInt || pair.Value.IsFloat || pair.Value.IsDouble)
                        value = "<color=" + (EditorGUIUtility.isProSkin ? ProIntColor : IntColor) + ">" + pair.Value +
                                "</color>";
                    else
                        value = "<color=" + (EditorGUIUtility.isProSkin ? ProStringColor : StringColor) + ">" +
                                pair.Value + "</color>";

                    field.Value = pair.Key + ": " + value;
                    field.HasPlus = false;
                    field.Parent = returnField;
                    field.X = field.Parent.X + Space;
                    returnField.Childs.Add(field);
                }
                else
                {
                    Field field = TryParse(pair.Value.ToString(), pair.Key, returnField);
                    field.X = field.Parent.X + Space;
                    returnField.Childs.Add(field);
                }
            }

            return returnField;
        }

        private class Field
        {
            public string Value = "";
            public int X;
            public bool HasPlus;
            public bool IsOpened;
            public Field Parent;
            public readonly List<Field> Childs = new List<Field>();
        }
    }
}
