using UnityEditor;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Editor
{
    [CustomEditor(typeof(ArenaGridProfile))]
    public sealed class ArenaGridProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var profile = (ArenaGridProfile)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Derived Summary", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Active Cell Count", profile.ExpectedActiveCellCount);
            }

            EditorGUILayout.HelpBox(
                "Active count and layout checksum are computed on demand from geometry and cell definitions. The checksum is intentionally not editable.",
                MessageType.Info);
        }
    }
}
