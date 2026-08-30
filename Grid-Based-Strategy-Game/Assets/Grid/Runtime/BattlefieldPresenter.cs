using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    public enum BattlefieldPresentationFailure
    {
        None,
        NoGridBound,
        GridNotReady,
        MissingCellCentre
    }

    public readonly struct BattlefieldPresentationResult
    {
        public bool Succeeded { get; }
        public BattlefieldPresentationFailure Failure { get; }
        public string Message { get; }
        public int SurfaceCount { get; }

        internal BattlefieldPresentationResult(
            bool succeeded,
            BattlefieldPresentationFailure failure,
            string message,
            int surfaceCount)
        {
            Succeeded = succeeded;
            Failure = failure;
            Message = message;
            SurfaceCount = surfaceCount;
        }
    }

    public readonly struct PresentedGridCell
    {
        public GridCoordinate Coordinate { get; }
        public string StableIdentity { get; }
        public ArenaCellZone Zone { get; }
        public Transform Transform { get; }

        internal PresentedGridCell(RuntimeGridCell cell, Transform transform)
        {
            Coordinate = cell.Coordinate;
            StableIdentity = cell.StableIdentity;
            Zone = cell.Zone;
            Transform = transform;
        }
    }

    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class BattlefieldPresenter : MonoBehaviour
    {
        [Header("Runtime Source")]
        [SerializeField] private RuntimeGridHost gridHost;

        [Header("Replaceable Presentation")]
        [Tooltip("Optional XZ-plane mesh. A generated quad is used when absent.")]
        [SerializeField] private Mesh placeholderMesh;
        [SerializeField] private Material teamAMaterial;
        [SerializeField] private Material neutralMaterial;
        [SerializeField] private Material teamBMaterial;
        [Range(0.1f, 1f)] [SerializeField] private float cellFill = 0.92f;
        [SerializeField] private float surfaceYOffset;

        private readonly Dictionary<string, PresentedGridCell> presentations =
            new Dictionary<string, PresentedGridCell>();
        private RuntimeGrid boundGrid;
        private Transform ownedRoot;
        private Mesh generatedMesh;
        private readonly Dictionary<ArenaCellZone, Material> fallbackMaterials =
            new Dictionary<ArenaCellZone, Material>();

        public int SurfaceCount => presentations.Count;
        public RuntimeGrid BoundGrid => boundGrid;
        public BattlefieldPresentationResult LastResult { get; private set; }

        private void Awake()
        {
            if (gridHost != null)
            {
                Bind(gridHost.Grid);
            }
        }

        private void OnDestroy()
        {
            Unbind();
            DestroyOwnedObject(generatedMesh);
            foreach (var material in fallbackMaterials.Values)
            {
                DestroyOwnedObject(material);
            }
            fallbackMaterials.Clear();
        }

        public void Bind(RuntimeGrid grid)
        {
            if (boundGrid == grid)
            {
                return;
            }

            Unbind();
            boundGrid = grid;
            if (boundGrid != null)
            {
                boundGrid.InitializationCompleted += HandleGridInitialisation;
                if (boundGrid.IsReady)
                {
                    Rebuild();
                }
            }
        }

        public void Unbind()
        {
            if (boundGrid != null)
            {
                boundGrid.InitializationCompleted -= HandleGridInitialisation;
            }

            boundGrid = null;
            ClearPresentation();
        }

        public BattlefieldPresentationResult Rebuild()
        {
            ClearPresentation();

            if (boundGrid == null)
            {
                return SetResult(false, BattlefieldPresentationFailure.NoGridBound, "No Runtime Grid is bound.", 0);
            }

            if (!boundGrid.IsReady)
            {
                return SetResult(false, BattlefieldPresentationFailure.GridNotReady, "The Runtime Grid is not ready.", 0);
            }

            ownedRoot = new GameObject("Battlefield Presentation (Transient)").transform;
            ownedRoot.SetParent(boundGrid.GridRoot, false);

            var geometry = boundGrid.Geometry;
            for (var z = 0; z < geometry.Height; z++)
            {
                for (var x = 0; x < geometry.Width; x++)
                {
                    var coordinate = new GridCoordinate(x, z);
                    if (!boundGrid.TryGetPlayableCell(coordinate, out var cell))
                    {
                        continue;
                    }

                    if (!boundGrid.TryGetCellCentre(coordinate, out var centre))
                    {
                        ClearPresentation();
                        return SetResult(
                            false,
                            BattlefieldPresentationFailure.MissingCellCentre,
                            $"No authoritative centre was available for {coordinate}.",
                            0);
                    }

                    var surface = CreateSurface(cell, centre, geometry.CellSize);
                    presentations.Add(cell.StableIdentity, new PresentedGridCell(cell, surface.transform));
                }
            }

            return SetResult(true, BattlefieldPresentationFailure.None, string.Empty, presentations.Count);
        }

        public void ClearPresentation()
        {
            presentations.Clear();
            if (ownedRoot != null)
            {
                DestroyOwnedObject(ownedRoot.gameObject);
                ownedRoot = null;
            }
        }

        public bool TryGetPresentation(string stableIdentity, out PresentedGridCell presentation) =>
            presentations.TryGetValue(stableIdentity, out presentation);

        private GameObject CreateSurface(RuntimeGridCell cell, Vector3 worldCentre, float cellSize)
        {
            var surface = new GameObject($"Cell {cell.Coordinate} [{cell.Zone}]");
            surface.transform.SetParent(ownedRoot, false);
            surface.transform.localPosition = ownedRoot.InverseTransformPoint(worldCentre) +
                                              new Vector3(0f, surfaceYOffset, 0f);
            surface.transform.localRotation = Quaternion.identity;
            surface.transform.localScale = new Vector3(cellSize * cellFill, 1f, cellSize * cellFill);

            var meshFilter = surface.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = placeholderMesh != null ? placeholderMesh : GetOrCreateFallbackMesh();
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetZoneMaterial(cell.Zone);
            return surface;
        }

        private Mesh GetOrCreateFallbackMesh()
        {
            if (generatedMesh != null)
            {
                return generatedMesh;
            }

            generatedMesh = new Mesh { name = "Generated Grid Cell Quad" };
            generatedMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            generatedMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();
            return generatedMesh;
        }

        private Material GetZoneMaterial(ArenaCellZone zone)
        {
            var configured = zone == ArenaCellZone.TeamADeployment
                ? teamAMaterial
                : zone == ArenaCellZone.TeamBDeployment
                    ? teamBMaterial
                    : neutralMaterial;
            if (configured != null)
            {
                return configured;
            }

            if (fallbackMaterials.TryGetValue(zone, out var fallback))
            {
                return fallback;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            fallback = new Material(shader) { name = $"Grid {zone} Fallback" };
            fallback.color = zone == ArenaCellZone.TeamADeployment
                ? new Color(0.2f, 0.45f, 1f, 1f)
                : zone == ArenaCellZone.TeamBDeployment
                    ? new Color(1f, 0.3f, 0.25f, 1f)
                    : new Color(0.55f, 0.55f, 0.55f, 1f);
            fallbackMaterials.Add(zone, fallback);
            return fallback;
        }

        private void HandleGridInitialisation(GridInitializationResult _)
        {
            if (boundGrid != null && boundGrid.IsReady)
            {
                Rebuild();
            }
            else
            {
                ClearPresentation();
            }
        }

        private BattlefieldPresentationResult SetResult(
            bool succeeded,
            BattlefieldPresentationFailure failure,
            string message,
            int count)
        {
            LastResult = new BattlefieldPresentationResult(succeeded, failure, message, count);
            return LastResult;
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
    }
}
