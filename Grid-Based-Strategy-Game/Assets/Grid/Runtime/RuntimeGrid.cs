using System;
using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Owns the atomically published logical battlefield for one runtime Grid.</summary>
    public sealed class RuntimeGrid
    {
        private PublishedState state;

        public RuntimeGridStatus Status { get; private set; } = RuntimeGridStatus.Uninitialised;
        public GridInitializationResult LastInitializationResult { get; private set; }
        public bool IsReady => Status == RuntimeGridStatus.Ready && state != null;
        public int BackingCellCount => IsReady ? state.Cells.Length : 0;
        public int ActiveCellCount => IsReady ? state.ActiveCellCount : 0;
        public GridGeometry Geometry => IsReady ? state.Geometry : default;
        public RuntimeGridSourceMetadata SourceMetadata => IsReady ? state.SourceMetadata : default;
        public Transform GridRoot => IsReady ? state.Mapper.GridRoot : null;

        public event Action<GridInitializationResult> InitializationCompleted;

        public GridInitializationResult Initialize(ArenaGridProfile profile, Transform gridRoot)
        {
            if (Status == RuntimeGridStatus.Ready || Status == RuntimeGridStatus.Initialising)
            {
                return CompleteWithoutStateChange(GridInitializationResult.Failed(
                    GridInitializationFailure.AlreadyInitialised,
                    profile != null ? profile.ProfileId : string.Empty,
                    "The Grid is already initialised. Use Reload to deliberately replace its layout."));
            }

            return Load(profile, gridRoot);
        }

        public GridInitializationResult Reload(ArenaGridProfile profile, Transform gridRoot)
        {
            return Load(profile, gridRoot);
        }

        public bool TryGetBackingCell(GridCoordinate coordinate, out RuntimeGridCell cell)
        {
            if (!IsReady || !state.Geometry.Contains(coordinate))
            {
                cell = default;
                return false;
            }

            cell = state.Cells[ToIndex(coordinate, state.Geometry.Width)];
            return true;
        }

        public bool TryGetPlayableCell(GridCoordinate coordinate, out RuntimeGridCell cell)
        {
            if (!TryGetBackingCell(coordinate, out cell) || !cell.IsActive)
            {
                cell = default;
                return false;
            }

            return true;
        }

        public bool TryGetCellCentre(GridCoordinate coordinate, out Vector3 worldPosition)
        {
            if (!TryGetBackingCell(coordinate, out _))
            {
                worldPosition = default;
                return false;
            }

            return state.Mapper.TryGridToWorld(coordinate, out worldPosition, out _);
        }

        private GridInitializationResult Load(ArenaGridProfile profile, Transform gridRoot)
        {
            Status = RuntimeGridStatus.Initialising;
            state = null;

            var validation = Validate(profile, gridRoot, out var geometry, out var expectedCellCount);
            if (!validation.Succeeded)
            {
                return Fail(validation);
            }

            try
            {
                var cells = new RuntimeGridCell[expectedCellCount];
                var activeCellCount = 0;

                for (var z = 0; z < geometry.Height; z++)
                {
                    for (var x = 0; x < geometry.Width; x++)
                    {
                        var coordinate = new GridCoordinate(x, z);
                        profile.TryGetCellDefinition(coordinate, out var definition);
                        if (definition.IsActive)
                        {
                            activeCellCount++;
                        }

                        cells[ToIndex(coordinate, geometry.Width)] = new RuntimeGridCell(
                            coordinate,
                            CreateStableCellIdentity(profile.ProfileId, coordinate),
                            profile.ProfileId,
                            definition.IsActive,
                            definition.Zone);
                    }
                }

                if (profile.ExpectedActiveCellCount >= 0 &&
                    profile.ExpectedActiveCellCount != activeCellCount)
                {
                    return Fail(GridInitializationResult.Failed(
                        GridInitializationFailure.ActiveCellCountMismatch,
                        profile.ProfileId,
                        $"Expected {profile.ExpectedActiveCellCount} active cells but constructed {activeCellCount}."));
                }

                var candidate = new PublishedState(
                    geometry,
                    new GridCoordinateMapper(geometry, gridRoot),
                    cells,
                    activeCellCount,
                    new RuntimeGridSourceMetadata(
                        profile.ProfileId,
                        profile.SchemaVersion,
                        profile.LayoutChecksum));

                state = candidate;
                Status = RuntimeGridStatus.Ready;
                return Complete(GridInitializationResult.Success(profile.ProfileId));
            }
            catch (Exception exception)
            {
                return Fail(GridInitializationResult.Failed(
                    GridInitializationFailure.ConstructionFailed,
                    profile.ProfileId,
                    $"Runtime Grid construction failed: {exception.Message}"));
            }
        }

        private static GridInitializationResult Validate(
            ArenaGridProfile profile,
            Transform gridRoot,
            out GridGeometry geometry,
            out int expectedCellCount)
        {
            geometry = default;
            expectedCellCount = 0;

            if (profile == null)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.NullProfile,
                    string.Empty,
                    "An Arena Grid Profile is required.");
            }

            if (gridRoot == null)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.NullGridRoot,
                    profile.ProfileId,
                    "A Grid root Transform is required.");
            }

            if (string.IsNullOrWhiteSpace(profile.ProfileId))
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.InvalidProfileIdentity,
                    profile.ProfileId,
                    "The profile must have a non-empty stable identity.");
            }

            if (profile.SchemaVersion != ArenaGridProfile.CurrentSchemaVersion)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.UnsupportedSchemaVersion,
                    profile.ProfileId,
                    $"Schema version {profile.SchemaVersion} is unsupported; expected {ArenaGridProfile.CurrentSchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(profile.LayoutChecksum))
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.InvalidLayoutChecksum,
                    profile.ProfileId,
                    "The profile must contain a layout checksum.");
            }

            if (!GridGeometry.TryCreate(
                    profile.Width,
                    profile.Height,
                    profile.CellSize,
                    out geometry,
                    out var geometryError))
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.InvalidGeometry,
                    profile.ProfileId,
                    $"Invalid Grid geometry: {geometryError}.");
            }

            try
            {
                expectedCellCount = checked(profile.Width * profile.Height);
            }
            catch (OverflowException)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.BackingCellCountOverflow,
                    profile.ProfileId,
                    "Grid dimensions exceed the supported backing-cell count.");
            }

            if (profile.CellDefinitionCount != expectedCellCount)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.LayoutLengthMismatch,
                    profile.ProfileId,
                    $"Layout contains {profile.CellDefinitionCount} definitions; expected {expectedCellCount}.");
            }

            var activeCount = 0;
            for (var z = 0; z < profile.Height; z++)
            {
                for (var x = 0; x < profile.Width; x++)
                {
                    var coordinate = new GridCoordinate(x, z);
                    profile.TryGetCellDefinition(coordinate, out var definition);
                    var zoneValue = (int)definition.Zone;
                    var zoneIsKnown = zoneValue >= (int)ArenaCellZone.None &&
                                      zoneValue <= (int)ArenaCellZone.TeamBDeployment;
                    var zoneIsValid = zoneIsKnown &&
                                      (definition.IsActive
                                          ? definition.Zone != ArenaCellZone.None
                                          : definition.Zone == ArenaCellZone.None);

                    if (!zoneIsValid)
                    {
                        return GridInitializationResult.Failed(
                            GridInitializationFailure.InvalidCellZone,
                            profile.ProfileId,
                            $"Cell {coordinate} has an invalid active/zone combination.");
                    }

                    if (definition.IsActive)
                    {
                        activeCount++;
                    }
                }
            }

            if (activeCount == 0)
            {
                return GridInitializationResult.Failed(
                    GridInitializationFailure.EmptyPlayableLayout,
                    profile.ProfileId,
                    "Runtime profiles must contain at least one active cell.");
            }

            return GridInitializationResult.Success(profile.ProfileId);
        }

        private GridInitializationResult Fail(GridInitializationResult result)
        {
            state = null;
            Status = RuntimeGridStatus.Failed;
            return Complete(result);
        }

        private GridInitializationResult Complete(GridInitializationResult result)
        {
            LastInitializationResult = result;
            InitializationCompleted?.Invoke(result);
            return result;
        }

        private GridInitializationResult CompleteWithoutStateChange(GridInitializationResult result)
        {
            LastInitializationResult = result;
            InitializationCompleted?.Invoke(result);
            return result;
        }

        private static int ToIndex(GridCoordinate coordinate, int width) =>
            (coordinate.Z * width) + coordinate.X;

        private static string CreateStableCellIdentity(string profileId, GridCoordinate coordinate) =>
            $"{profileId}:{coordinate.X}:{coordinate.Z}";

        private sealed class PublishedState
        {
            public GridGeometry Geometry { get; }
            public GridCoordinateMapper Mapper { get; }
            public RuntimeGridCell[] Cells { get; }
            public int ActiveCellCount { get; }
            public RuntimeGridSourceMetadata SourceMetadata { get; }

            public PublishedState(
                GridGeometry geometry,
                GridCoordinateMapper mapper,
                RuntimeGridCell[] cells,
                int activeCellCount,
                RuntimeGridSourceMetadata sourceMetadata)
            {
                Geometry = geometry;
                Mapper = mapper;
                Cells = cells;
                ActiveCellCount = activeCellCount;
                SourceMetadata = sourceMetadata;
            }
        }
    }
}
