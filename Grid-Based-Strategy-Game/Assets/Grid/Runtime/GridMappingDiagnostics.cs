using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Optional development-only footprint and cell-centre gizmos.</summary>
    [DisallowMultipleComponent]
    public sealed class GridMappingDiagnostics : MonoBehaviour
    {
        [Header("Temporary Slice 1 Geometry")]
        [Tooltip("Backing-grid width used only for mapping diagnostics.")]
        [Min(1)] [SerializeField] private int width = 9;

        [Tooltip("Backing-grid height used only for mapping diagnostics.")]
        [Min(1)] [SerializeField] private int height = 9;

        [Tooltip("World-space size of one square cell before root scaling.")]
        [Min(float.Epsilon)] [SerializeField] private float cellSize = 1f;

        [Header("Gizmos")]
        [SerializeField] private bool showFootprint = true;
        [SerializeField] private bool showCellCentres = true;
        [Min(0.001f)] [SerializeField] private float centreMarkerRadius = 0.04f;

        private void OnDrawGizmosSelected()
        {
            if (!GridGeometry.TryCreate(width, height, cellSize, out var geometry, out _))
            {
                return;
            }

            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (showFootprint)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(width * cellSize, 0f, height * cellSize));
                Gizmos.DrawSphere(Vector3.zero, centreMarkerRadius * 1.5f);
            }

            if (showCellCentres)
            {
                Gizmos.color = Color.yellow;
                for (var z = 0; z < height; z++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var localCentre = new Vector3(
                            (x - ((width - 1) * 0.5f)) * cellSize,
                            0f,
                            (z - ((height - 1) * 0.5f)) * cellSize);
                        Gizmos.DrawSphere(localCentre, centreMarkerRadius);
                    }
                }
            }

            Gizmos.matrix = previousMatrix;
        }
    }
}
