using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>
    /// Replaceable development harness for manually exercising Grid Slices 1-4 from the Inspector.
    /// It owns no logical state and is not used by gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridManualTestHarness : MonoBehaviour
    {
        [Header("Development References")]
        [SerializeField] private RuntimeGridHost gridHost;
        [SerializeField] private BattlefieldPresenter presenter;
        [SerializeField] private RuntimeGridDiagnostics diagnostics;

        [Header("Transform Test Steps")]
        [Min(0.1f)] [SerializeField] private float translationStep = 1f;
        [Range(1f, 180f)] [SerializeField] private float rotationStep = 15f;

        [Header("Occupancy Test Request")]
        [SerializeField] private string occupantId = "test-unit-1";
        [SerializeField] private Vector2Int sourceCoordinate;
        [SerializeField] private Vector2Int destinationCoordinate = new Vector2Int(1, 0);

        public RuntimeGridHost GridHost => gridHost;
        public BattlefieldPresenter Presenter => presenter;
        public RuntimeGridDiagnostics Diagnostics => diagnostics;
        public GridOccupancyResult LastOccupancyResult { get; private set; }

        [ContextMenu("Manual Test/Reload Logical Grid")]
        public void ReloadLogicalGrid()
        {
            if (gridHost != null)
            {
                gridHost.Reload();
            }
        }

        [ContextMenu("Manual Test/Rebuild Presentation")]
        public void RebuildPresentation()
        {
            presenter?.Rebuild();
        }

        [ContextMenu("Manual Test/Clear Presentation")]
        public void ClearPresentation()
        {
            presenter?.ClearPresentation();
        }

        [ContextMenu("Manual Test/Toggle All Diagnostics")]
        public void ToggleAllDiagnostics()
        {
            if (diagnostics == null)
            {
                return;
            }

            var enabled = !diagnostics.AnyLayerVisible;
            diagnostics.SetLayers(enabled, enabled, enabled, enabled, enabled, enabled, enabled);
            diagnostics.SetOccupantsVisible(enabled);
        }

        public void PlaceOccupant()
        {
            LastOccupancyResult = Grid.Place(
                new GridOccupantId(occupantId), ToCoordinate(destinationCoordinate));
        }

        public void MoveOccupant()
        {
            LastOccupancyResult = Grid.Move(
                new GridOccupantId(occupantId),
                ToCoordinate(sourceCoordinate),
                ToCoordinate(destinationCoordinate));
        }

        public void RemoveOccupant()
        {
            LastOccupancyResult = Grid.Remove(
                new GridOccupantId(occupantId), ToCoordinate(sourceCoordinate));
        }

        public GridOccupancyConsistencyReport ScanOccupancy() => Grid.ScanOccupancyConsistency();

        private RuntimeGrid Grid => gridHost != null ? gridHost.Grid : new RuntimeGrid();
        private static GridCoordinate ToCoordinate(Vector2Int value) => new GridCoordinate(value.x, value.y);

        [ContextMenu("Manual Test/Move Grid Right")]
        public void MoveGridRight()
        {
            transform.position += Vector3.right * translationStep;
        }

        [ContextMenu("Manual Test/Rotate Grid")]
        public void RotateGrid()
        {
            transform.Rotate(Vector3.up, rotationStep, Space.World);
        }

        [ContextMenu("Manual Test/Reset Grid Transform")]
        public void ResetGridTransform()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}
