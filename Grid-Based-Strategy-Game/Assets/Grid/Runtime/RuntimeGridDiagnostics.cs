using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    [DisallowMultipleComponent]
    public sealed class RuntimeGridDiagnostics : MonoBehaviour
    {
        [SerializeField] private RuntimeGridHost gridHost;

        [Header("Runtime Diagnostics (Off By Default)")]
        [SerializeField] private bool showRoot;
        [SerializeField] private bool showBoundary;
        [SerializeField] private bool showActiveFootprint;
        [SerializeField] private bool showCentres;
        [SerializeField] private bool showZones;
        [SerializeField] private bool showCoordinates;
        [SerializeField] private bool showStableIdentities;

        [Header("Colours")]
        [SerializeField] private Color rootColour = Color.white;
        [SerializeField] private Color boundaryColour = Color.cyan;
        [SerializeField] private Color centreColour = Color.yellow;
        [SerializeField] private Color teamAColour = new Color(0.2f, 0.45f, 1f, 0.35f);
        [SerializeField] private Color neutralColour = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        [SerializeField] private Color teamBColour = new Color(1f, 0.3f, 0.25f, 0.35f);

        public RuntimeGrid Grid => gridHost != null ? gridHost.Grid : null;
        public bool ShowRoot => showRoot;
        public bool ShowBoundary => showBoundary;
        public bool ShowActiveFootprint => showActiveFootprint;
        public bool ShowCentres => showCentres;
        public bool ShowZones => showZones;
        public bool ShowCoordinates => showCoordinates;
        public bool ShowStableIdentities => showStableIdentities;
        public bool AnyLayerVisible => showRoot || showBoundary || showActiveFootprint || showCentres ||
                                       showZones || showCoordinates || showStableIdentities;

        public void Bind(RuntimeGridHost host) => gridHost = host;

        public void SetLayers(
            bool root,
            bool boundary,
            bool activeFootprint,
            bool centres,
            bool zones,
            bool coordinates,
            bool stableIdentities)
        {
            showRoot = root;
            showBoundary = boundary;
            showActiveFootprint = activeFootprint;
            showCentres = centres;
            showZones = zones;
            showCoordinates = coordinates;
            showStableIdentities = stableIdentities;
        }

        private void OnDrawGizmos()
        {
            var grid = Grid;
            if (grid == null || !grid.IsReady || !AnyLayerVisible)
            {
                return;
            }

            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = grid.GridRoot.localToWorldMatrix;
            var geometry = grid.Geometry;

            if (showRoot)
            {
                Gizmos.color = rootColour;
                Gizmos.DrawSphere(Vector3.zero, geometry.CellSize * 0.12f);
            }

            if (showBoundary)
            {
                Gizmos.color = boundaryColour;
                Gizmos.DrawWireCube(
                    Vector3.zero,
                    new Vector3(geometry.Width * geometry.CellSize, 0f, geometry.Height * geometry.CellSize));
            }

            for (var z = 0; z < geometry.Height; z++)
            {
                for (var x = 0; x < geometry.Width; x++)
                {
                    var coordinate = new GridCoordinate(x, z);
                    if (!grid.TryGetPlayableCell(coordinate, out var cell) ||
                        !grid.TryGetCellCentre(coordinate, out var worldCentre))
                    {
                        continue;
                    }

                    var localCentre = grid.GridRoot.InverseTransformPoint(worldCentre);
                    if (showActiveFootprint || showZones)
                    {
                        Gizmos.color = showZones ? GetZoneColour(cell.Zone) : new Color(1f, 1f, 1f, 0.2f);
                        Gizmos.DrawCube(localCentre, new Vector3(geometry.CellSize * 0.9f, 0.01f, geometry.CellSize * 0.9f));
                    }

                    if (showCentres)
                    {
                        Gizmos.color = centreColour;
                        Gizmos.DrawSphere(localCentre, geometry.CellSize * 0.05f);
                    }
                }
            }

            Gizmos.matrix = previousMatrix;
        }

        private Color GetZoneColour(ArenaCellZone zone) =>
            zone == ArenaCellZone.TeamADeployment
                ? teamAColour
                : zone == ArenaCellZone.TeamBDeployment
                    ? teamBColour
                    : neutralColour;
    }
}
