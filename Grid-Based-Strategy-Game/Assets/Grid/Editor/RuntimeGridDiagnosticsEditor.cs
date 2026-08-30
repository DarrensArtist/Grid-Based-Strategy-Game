using UnityEditor;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Editor
{
    [CustomEditor(typeof(RuntimeGridDiagnostics))]
    public sealed class RuntimeGridDiagnosticsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var diagnostics = (RuntimeGridDiagnostics)target;
            var grid = diagnostics.Grid;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authoritative Grid", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status", grid != null ? grid.Status.ToString() : "No host bound");
            if (grid != null && grid.IsReady)
            {
                EditorGUILayout.LabelField("Profile", grid.SourceMetadata.ProfileId);
                EditorGUILayout.LabelField("Schema", grid.SourceMetadata.SchemaVersion.ToString());
                EditorGUILayout.LabelField("Checksum", grid.SourceMetadata.LayoutChecksum);
                EditorGUILayout.LabelField("Active Cells", grid.ActiveCellCount.ToString());
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawLabels(RuntimeGridDiagnostics diagnostics, GizmoType _)
        {
            var grid = diagnostics.Grid;
            if (grid == null || !grid.IsReady ||
                (!diagnostics.ShowCoordinates && !diagnostics.ShowStableIdentities))
            {
                return;
            }

            var geometry = grid.Geometry;
            for (var z = 0; z < geometry.Height; z++)
            {
                for (var x = 0; x < geometry.Width; x++)
                {
                    var coordinate = new GridCoordinate(x, z);
                    if (!grid.TryGetPlayableCell(coordinate, out var cell) ||
                        !grid.TryGetCellCentre(coordinate, out var centre))
                    {
                        continue;
                    }

                    var label = diagnostics.ShowCoordinates ? coordinate.ToString() : string.Empty;
                    if (diagnostics.ShowStableIdentities)
                    {
                        label = string.IsNullOrEmpty(label) ? cell.StableIdentity : $"{label}\n{cell.StableIdentity}";
                    }

                    Handles.Label(centre, label);
                }
            }
        }
    }
}
