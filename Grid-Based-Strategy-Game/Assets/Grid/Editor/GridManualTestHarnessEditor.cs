using UnityEditor;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Editor
{
    [CustomEditor(typeof(GridManualTestHarness))]
    public sealed class GridManualTestHarnessEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var harness = (GridManualTestHarness)target;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Enter Play Mode, then use these controls to verify loading, presentation rebuilds, " +
                "diagnostics, and translated/rotated Grid-root alignment.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.LabelField("Lifecycle", EditorStyles.boldLabel);
                if (GUILayout.Button("Reload Logical Grid")) harness.ReloadLogicalGrid();
                if (GUILayout.Button("Rebuild Presentation")) harness.RebuildPresentation();
                if (GUILayout.Button("Clear Presentation")) harness.ClearPresentation();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Occupancy", EditorStyles.boldLabel);
                if (GUILayout.Button("Place Occupant At Destination")) harness.PlaceOccupant();
                if (GUILayout.Button("Move Occupant Source → Destination")) harness.MoveOccupant();
                if (GUILayout.Button("Remove Occupant At Source")) harness.RemoveOccupant();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Diagnostics and Transform", EditorStyles.boldLabel);
                if (GUILayout.Button("Toggle All Diagnostics")) harness.ToggleAllDiagnostics();
                if (GUILayout.Button("Move Grid Right")) harness.MoveGridRight();
                if (GUILayout.Button("Rotate Grid")) harness.RotateGrid();
                if (GUILayout.Button("Reset Grid Transform")) harness.ResetGridTransform();
            }

            var grid = harness.GridHost != null ? harness.GridHost.Grid : null;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Grid", grid != null ? grid.Status.ToString() : "Missing host");
            EditorGUILayout.LabelField(
                "Surfaces",
                harness.Presenter != null ? harness.Presenter.SurfaceCount.ToString() : "Missing presenter");
            EditorGUILayout.LabelField(
                "Diagnostics",
                harness.Diagnostics != null && harness.Diagnostics.AnyLayerVisible ? "Visible" : "Hidden");
            if (grid != null)
            {
                EditorGUILayout.LabelField("Occupied Cells", grid.OccupiedCellCount.ToString());
                var consistency = harness.ScanOccupancy();
                EditorGUILayout.LabelField("Occupancy Indexes", consistency.IsConsistent ? "Consistent" : "Mismatch");
            }

            var last = harness.LastOccupancyResult;
            if (last.Succeeded || last.Failure != GridOccupancyFailure.None)
            {
                EditorGUILayout.LabelField("Last Occupancy Request", last.Succeeded ? "Succeeded" : last.Failure.ToString());
                if (!string.IsNullOrEmpty(last.Message))
                {
                    EditorGUILayout.HelpBox(last.Message, last.Succeeded ? MessageType.Info : MessageType.Warning);
                }
            }
        }
    }
}
