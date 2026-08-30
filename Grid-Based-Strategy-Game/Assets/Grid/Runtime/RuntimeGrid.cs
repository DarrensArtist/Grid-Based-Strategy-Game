using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Owns the atomically published logical battlefield for one runtime Grid.</summary>
    public sealed partial class RuntimeGrid
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
        public int OccupiedCellCount => IsReady ? state.CellOccupants.Count : 0;

        public event Action<GridInitializationResult> InitializationCompleted;
        public event Action<GridOccupancyResult> OccupancyChanged;

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

        public bool TryGetOccupant(GridCoordinate coordinate, out GridOccupantId occupant)
        {
            if (IsReady && state.CellOccupants.TryGetValue(coordinate, out occupant))
            {
                return true;
            }

            occupant = default;
            return false;
        }

        public bool TryGetOccupantLocation(GridOccupantId occupant, out GridCoordinate coordinate)
        {
            if (IsReady && occupant.IsValid && state.OccupantLocations.TryGetValue(occupant, out coordinate))
            {
                return true;
            }

            coordinate = default;
            return false;
        }

        public GridOccupancyResult Place(GridOccupantId occupant, GridCoordinate destination)
        {
            var operation = GridOccupancyOperation.Place;
            var commonFailure = ValidateCommon(operation, occupant);
            if (commonFailure.HasValue)
            {
                return commonFailure.Value;
            }

            if (state.OccupantLocations.TryGetValue(occupant, out var existing))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.OccupantAlreadyRegistered,
                    occupant, $"Occupant '{occupant}' is already registered at {existing}.", true, existing,
                    true, destination);
            }

            var destinationFailure = ValidateDestination(operation, occupant, destination);
            if (destinationFailure.HasValue)
            {
                return destinationFailure.Value;
            }

            state.CellOccupants.Add(destination, occupant);
            state.OccupantLocations.Add(occupant, destination);
            return Publish(GridOccupancyResult.Success(operation, occupant, false, default, true, destination));
        }

        /// <summary>Moves an occupant only when its registered location still matches expectedSource.</summary>
        public GridOccupancyResult Move(
            GridOccupantId occupant,
            GridCoordinate expectedSource,
            GridCoordinate destination)
        {
            var operation = GridOccupancyOperation.Move;
            var commonFailure = ValidateCommon(operation, occupant);
            if (commonFailure.HasValue)
            {
                return commonFailure.Value;
            }

            if (!state.OccupantLocations.TryGetValue(occupant, out var registeredSource))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.OccupantNotRegistered,
                    occupant, $"Occupant '{occupant}' is not registered in this Grid.", true, expectedSource,
                    true, destination);
            }

            if (registeredSource != expectedSource)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.SourceMismatch,
                    occupant, $"Expected source {expectedSource} is stale; occupant is at {registeredSource}.",
                    true, expectedSource, true, destination);
            }

            if (destination == registeredSource)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.SameCellMove,
                    occupant, "Moving to the current cell is rejected as a no-op.", true, registeredSource,
                    true, destination);
            }

            var destinationFailure = ValidateDestination(operation, occupant, destination);
            if (destinationFailure.HasValue)
            {
                return destinationFailure.Value;
            }

            state.CellOccupants.Remove(registeredSource);
            state.CellOccupants.Add(destination, occupant);
            state.OccupantLocations[occupant] = destination;
            return Publish(GridOccupancyResult.Success(
                operation, occupant, true, registeredSource, true, destination));
        }

        public GridOccupancyResult Remove(GridOccupantId occupant, GridCoordinate expectedCoordinate)
        {
            var operation = GridOccupancyOperation.Remove;
            var commonFailure = ValidateCommon(operation, occupant);
            if (commonFailure.HasValue)
            {
                return commonFailure.Value;
            }

            if (state.CellOccupants.TryGetValue(expectedCoordinate, out var occupantAtExpectedCell) &&
                occupantAtExpectedCell != occupant)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.OccupantMismatch,
                    occupant, $"Cell {expectedCoordinate} is occupied by a different identity.", true,
                    expectedCoordinate);
            }

            if (!state.OccupantLocations.TryGetValue(occupant, out var registeredCoordinate))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.OccupantNotRegistered,
                    occupant, $"Occupant '{occupant}' is not registered in this Grid.", true,
                    expectedCoordinate);
            }

            if (registeredCoordinate != expectedCoordinate)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.SourceMismatch,
                    occupant, $"Expected cell {expectedCoordinate} is stale; occupant is at {registeredCoordinate}.",
                    true, expectedCoordinate);
            }

            if (!state.CellOccupants.TryGetValue(expectedCoordinate, out var current) || current != occupant)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.OccupantMismatch,
                    occupant, $"Cell {expectedCoordinate} does not contain occupant '{occupant}'.", true,
                    expectedCoordinate);
            }

            state.CellOccupants.Remove(expectedCoordinate);
            state.OccupantLocations.Remove(occupant);
            return Publish(GridOccupancyResult.Success(
                operation, occupant, true, expectedCoordinate, false, default));
        }

        public GridOccupancyConsistencyReport ScanOccupancyConsistency()
        {
            var errors = new List<string>();
            if (!IsReady)
            {
                errors.Add("The Grid is not ready; no occupancy state is available to scan.");
                return new GridOccupancyConsistencyReport(0, 0, errors);
            }

            foreach (var pair in state.CellOccupants)
            {
                if (!state.OccupantLocations.TryGetValue(pair.Value, out var reverse) || reverse != pair.Key)
                {
                    errors.Add($"Cell {pair.Key} points to '{pair.Value}' without a matching reverse entry.");
                }

                if (!TryGetPlayableCell(pair.Key, out _))
                {
                    errors.Add($"Occupied coordinate {pair.Key} is not an active playable cell.");
                }
            }

            foreach (var pair in state.OccupantLocations)
            {
                if (!state.CellOccupants.TryGetValue(pair.Value, out var forward) || forward != pair.Key)
                {
                    errors.Add($"Occupant '{pair.Key}' points to {pair.Value} without a matching cell entry.");
                }
            }

            return new GridOccupancyConsistencyReport(
                state.CellOccupants.Count, state.OccupantLocations.Count, errors);
        }

        private GridOccupancyResult? ValidateCommon(GridOccupancyOperation operation, GridOccupantId occupant)
        {
            if (!IsReady)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.GridNotReady,
                    occupant, "The Grid must be ready before occupancy can change.");
            }

            if (!occupant.IsValid)
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.InvalidOccupant,
                    occupant, "A non-empty stable occupant identity is required.");
            }

            return null;
        }

        private GridOccupancyResult? ValidateDestination(
            GridOccupancyOperation operation,
            GridOccupantId occupant,
            GridCoordinate destination)
        {
            if (!state.Geometry.Contains(destination))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.DestinationOutsideGrid,
                    occupant, $"Destination {destination} is outside the Grid.", false, default, true,
                    destination);
            }

            if (!TryGetPlayableCell(destination, out _))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.DestinationInactive,
                    occupant, $"Destination {destination} is inactive.", false, default, true, destination);
            }

            if (state.CellOccupants.ContainsKey(destination))
            {
                return GridOccupancyResult.Failed(operation, GridOccupancyFailure.DestinationOccupied,
                    occupant, $"Destination {destination} is already occupied.", false, default, true,
                    destination);
            }

            return null;
        }

        private GridOccupancyResult Publish(GridOccupancyResult result)
        {
            OccupancyChanged?.Invoke(result);
            return result;
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
                        profile.LayoutChecksum),
                    new Dictionary<GridCoordinate, GridOccupantId>(),
                    new Dictionary<GridOccupantId, GridCoordinate>());

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
            public Dictionary<GridCoordinate, GridOccupantId> CellOccupants { get; }
            public Dictionary<GridOccupantId, GridCoordinate> OccupantLocations { get; }

            public PublishedState(
                GridGeometry geometry,
                GridCoordinateMapper mapper,
                RuntimeGridCell[] cells,
                int activeCellCount,
                RuntimeGridSourceMetadata sourceMetadata,
                Dictionary<GridCoordinate, GridOccupantId> cellOccupants,
                Dictionary<GridOccupantId, GridCoordinate> occupantLocations)
            {
                Geometry = geometry;
                Mapper = mapper;
                Cells = cells;
                ActiveCellCount = activeCellCount;
                SourceMetadata = sourceMetadata;
                CellOccupants = cellOccupants;
                OccupantLocations = occupantLocations;
            }
        }
    }
}
