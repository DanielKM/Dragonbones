using UnityEditor;
using UnityEngine;

using RTSEngine.Utilities;

namespace RTSEngine.EditorOnly
{
    public abstract class TabsEditorBase<T> : Editor where T : MonoBehaviour
    {
        protected SerializedObject SO { private set; get; }
        protected T comp { private set; get; }
        protected abstract Int2D tabID { get; set; }

        public void OnEnable()
        {
            comp = (T)target;
            SO = new SerializedObject(comp);
        }

        public virtual void OnInspectorGUI(string[][] toolbars)
        {
            SO.Update();

            GUIStyle buttonStyle = new GUIStyle (GUI.skin.button);
            buttonStyle.margin.right = 0;
            buttonStyle.margin.left = 0;

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical();

            for (int x = 0; x < toolbars.Length; x++)
            {
                GUILayout.BeginHorizontal();

                float buttonWidth = (EditorGUIUtility.currentViewWidth - 20.0f) / toolbars[x].Length - (toolbars[x].Length) * (buttonStyle.margin.left);
                for (int y = 0; y < toolbars[x].Length; y++)
                {
                    GUI.enabled = !(tabID.x == x && tabID.y == y);
                    if (GUILayout.Button(toolbars[x][y], buttonStyle, GUILayout.Width(buttonWidth)))
                        tabID = new Int2D { x = x, y = y };
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                SO.ApplyModifiedProperties();
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space();

            if (!tabID.x.IsValidIndex(toolbars))
                tabID = new Int2D { x = 0, y = tabID.y };
            if (!tabID.y.IsValidIndex(toolbars[tabID.x]))
                tabID = new Int2D { x = tabID.y, y = 0 };

            OnTabSwitch(toolbars[tabID.x][tabID.y]);

            SO.ApplyModifiedProperties();
        }

        protected virtual void OnTabSwitch(string tabName) { }
    }
}
