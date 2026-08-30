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
                EditorGUILayout.LabelField("Occupied Cells", grid.OccupiedCellCount.ToString());
                var consistency = grid.ScanOccupancyConsistency();
                EditorGUILayout.LabelField("Occupancy Indexes", consistency.IsConsistent ? "Consistent" : "Mismatch");
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawLabels(RuntimeGridDiagnostics diagnostics, GizmoType _)
        {
            var grid = diagnostics.Grid;
            if (grid == null || !grid.IsReady)
            {
                return;
            }

            if (diagnostics.ShowCoordinates || diagnostics.ShowStableIdentities || diagnostics.ShowOccupants)
            {
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

                        if (diagnostics.ShowOccupants && grid.TryGetOccupant(coordinate, out var occupant))
                        {
                            label = string.IsNullOrEmpty(label) ? $"Occupant: {occupant}" : $"{label}\nOccupant: {occupant}";
                        }

                        if (!string.IsNullOrEmpty(label)) Handles.Label(centre, label);
                    }
                }
            }

            if (diagnostics.ShowLastQuery) DrawLastQuery(grid);
        }

        private static void DrawLastQuery(RuntimeGrid grid)
        {
            var result = grid.LastQueryResult;
            if (result == null) return;

            Handles.color = Color.magenta;
            for (var index = 0; index < result.Coordinates.Count; index++)
            {
                if (grid.TryGetCellCentre(result.Coordinates[index], out var centre))
                {
                    Handles.DrawWireDisc(centre, grid.GridRoot.up, grid.Geometry.CellSize * 0.3f);
                    Handles.Label(centre + grid.GridRoot.up * 0.05f, $"#{index}");
                }
            }

            var context = grid.LastQueryDiagnostics;
            if (context.HasDirection && context.HasOrigin && grid.TryGetCellCentre(context.Origin, out var origin))
            {
                var offset = DirectionOffset(context.Direction);
                var worldDirection = grid.GridRoot.TransformDirection(new Vector3(offset.x, 0f, offset.y));
                Handles.DrawLine(origin, origin + worldDirection * grid.Geometry.CellSize);
            }

            if (context.HasBounds)
            {
                var minimum = context.MinimumBounds;
                var maximum = context.MaximumBounds;
                var localCentre = new Vector3(
                    ((minimum.X + maximum.X + 1) * 0.5f - grid.Geometry.Width * 0.5f) * grid.Geometry.CellSize,
                    0f,
                    ((minimum.Z + maximum.Z + 1) * 0.5f - grid.Geometry.Height * 0.5f) * grid.Geometry.CellSize);
                var size = new Vector3(
                    (maximum.X - minimum.X + 1) * grid.Geometry.CellSize,
                    0f,
                    (maximum.Z - minimum.Z + 1) * grid.Geometry.CellSize);
                var previous = Handles.matrix;
                Handles.matrix = grid.GridRoot.localToWorldMatrix;
                Handles.DrawWireCube(localCentre, size);
                Handles.matrix = previous;
            }

            if (result.HasTerminationCoordinate)
            {
                var coordinate = result.TerminationCoordinate;
                var geometry = grid.Geometry;
                var local = new Vector3(
                    (coordinate.X + 0.5f - geometry.Width * 0.5f) * geometry.CellSize,
                    0f,
                    (coordinate.Z + 0.5f - geometry.Height * 0.5f) * geometry.CellSize);
                var world = grid.GridRoot.TransformPoint(local);
                Handles.color = Color.red;
                Handles.Label(world, result.Termination.ToString());
                Handles.DrawWireDisc(world, grid.GridRoot.up, geometry.CellSize * 0.4f);
            }
        }

        private static Vector2Int DirectionOffset(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.North: return new Vector2Int(0, 1);
                case GridDirection.East: return new Vector2Int(1, 0);
                case GridDirection.South: return new Vector2Int(0, -1);
                case GridDirection.West: return new Vector2Int(-1, 0);
                case GridDirection.NorthEast: return new Vector2Int(1, 1);
                case GridDirection.SouthEast: return new Vector2Int(1, -1);
                case GridDirection.SouthWest: return new Vector2Int(-1, -1);
                default: return new Vector2Int(-1, 1);
            }
        }
    }
}
