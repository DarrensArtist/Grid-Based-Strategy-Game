using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>
    /// Replaceable development harness for manually exercising Grid Slices 1-3 from the Inspector.
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

        public RuntimeGridHost GridHost => gridHost;
        public BattlefieldPresenter Presenter => presenter;
        public RuntimeGridDiagnostics Diagnostics => diagnostics;

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
        }

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
