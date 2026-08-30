using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Eight planar directions. North is positive Z; enum order is query order.</summary>
    public enum GridDirection
    {
        North,
        East,
        South,
        West,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    public enum GridNeighbourMode
    {
        Cardinal,
        Diagonal,
        All
    }

    public enum GridDistanceMode
    {
        Manhattan,
        Chebyshev
    }

    public enum GridOccupancyFilter
    {
        All,
        UnoccupiedOnly,
        OccupiedOnly,
        StopBeforeOccupied
    }

    public enum GridQueryKind
    {
        Neighbours,
        DirectionalLine,
        Rectangle,
        Area,
        PlayableEdges
    }

    public enum GridQueryFailure
    {
        None,
        GridNotReady,
        OriginOutsideGrid,
        OriginInactive,
        InvalidLength,
        InvalidRadius,
        InvalidDirection,
        InvalidNeighbourMode,
        InvalidDistanceMode,
        InvalidOccupancyFilter,
        UnsupportedOccupancyFilter
    }

    public enum GridQueryTermination
    {
        None,
        RequestedLengthReached,
        OutsideGrid,
        InactiveCell,
        OccupiedCell
    }

    [Flags]
    public enum GridPlayableEdge
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    public readonly struct GridPlayableEdgeFact
    {
        public GridCoordinate Coordinate { get; }
        public GridPlayableEdge Edges { get; }

        internal GridPlayableEdgeFact(GridCoordinate coordinate, GridPlayableEdge edges)
        {
            Coordinate = coordinate;
            Edges = edges;
        }
    }

    /// <summary>Structured, transient facts used only to draw the most recent query.</summary>
    public readonly struct GridQueryDiagnosticContext
    {
        public bool HasOrigin { get; }
        public GridCoordinate Origin { get; }
        public bool HasDirection { get; }
        public GridDirection Direction { get; }
        public bool HasBounds { get; }
        public GridCoordinate MinimumBounds { get; }
        public GridCoordinate MaximumBounds { get; }

        internal GridQueryDiagnosticContext(
            bool hasOrigin, GridCoordinate origin,
            bool hasDirection, GridDirection direction,
            bool hasBounds, GridCoordinate minimumBounds, GridCoordinate maximumBounds)
        {
            HasOrigin = hasOrigin;
            Origin = origin;
            HasDirection = hasDirection;
            Direction = direction;
            HasBounds = hasBounds;
            MinimumBounds = minimumBounds;
            MaximumBounds = maximumBounds;
        }
    }

    /// <summary>Immutable, ordered output from one spatial query.</summary>
    public sealed class GridQueryResult
    {
        private readonly ReadOnlyCollection<GridCoordinate> coordinates;
        private readonly ReadOnlyCollection<GridPlayableEdgeFact> edges;

        public bool Succeeded { get; }
        public GridQueryKind Kind { get; }
        public GridQueryFailure Failure { get; }
        public IReadOnlyList<GridCoordinate> Coordinates => coordinates;
        public IReadOnlyList<GridPlayableEdgeFact> Edges => edges;
        public GridQueryTermination Termination { get; }
        public bool HasTerminationCoordinate { get; }
        public GridCoordinate TerminationCoordinate { get; }
        public string Description { get; }
        public string Message { get; }

        private GridQueryResult(
            bool succeeded,
            GridQueryKind kind,
            GridQueryFailure failure,
            List<GridCoordinate> coordinates,
            List<GridPlayableEdgeFact> edges,
            GridQueryTermination termination,
            bool hasTerminationCoordinate,
            GridCoordinate terminationCoordinate,
            string description,
            string message)
        {
            Succeeded = succeeded;
            Kind = kind;
            Failure = failure;
            this.coordinates = Array.AsReadOnly((coordinates ?? new List<GridCoordinate>()).ToArray());
            this.edges = Array.AsReadOnly((edges ?? new List<GridPlayableEdgeFact>()).ToArray());
            Termination = termination;
            HasTerminationCoordinate = hasTerminationCoordinate;
            TerminationCoordinate = terminationCoordinate;
            Description = description ?? string.Empty;
            Message = message ?? string.Empty;
        }

        internal static GridQueryResult Success(
            GridQueryKind kind,
            List<GridCoordinate> coordinates,
            string description,
            GridQueryTermination termination = GridQueryTermination.None,
            bool hasTerminationCoordinate = false,
            GridCoordinate terminationCoordinate = default,
            List<GridPlayableEdgeFact> edges = null) =>
            new GridQueryResult(true, kind, GridQueryFailure.None, coordinates, edges, termination,
                hasTerminationCoordinate, terminationCoordinate, description, string.Empty);

        internal static GridQueryResult Failed(
            GridQueryKind kind,
            GridQueryFailure failure,
            string description,
            string message) =>
            new GridQueryResult(false, kind, failure, null, null, GridQueryTermination.None,
                false, default, description, message);
    }

    public sealed partial class RuntimeGrid
    {
        private static readonly GridDirection[] CardinalDirectionOrder =
        {
            GridDirection.North, GridDirection.East, GridDirection.South, GridDirection.West
        };

        private static readonly GridDirection[] DiagonalDirectionOrder =
        {
            GridDirection.NorthEast, GridDirection.SouthEast,
            GridDirection.SouthWest, GridDirection.NorthWest
        };

        /// <summary>Diagnostic-only record of the most recent query; it is not gameplay state.</summary>
        public GridQueryResult LastQueryResult { get; private set; }
        public GridQueryDiagnosticContext LastQueryDiagnostics { get; private set; }

        public GridQueryResult QueryNeighbours(
            GridCoordinate origin,
            GridNeighbourMode mode = GridNeighbourMode.All,
            GridOccupancyFilter occupancy = GridOccupancyFilter.All)
        {
            const GridQueryKind kind = GridQueryKind.Neighbours;
            BeginQueryDiagnostics(true, origin, false, default, false, default, default);
            var invalid = ValidateOrigin(kind, origin, $"Neighbours from {origin}");
            if (invalid != null) return Record(invalid);
            if (!Enum.IsDefined(typeof(GridNeighbourMode), mode))
                return Record(GridQueryResult.Failed(kind, GridQueryFailure.InvalidNeighbourMode,
                    "Neighbours", $"Neighbour mode '{mode}' is unsupported."));
            if (!IsKnownOccupancyFilter(occupancy)) return Record(InvalidFilter(kind, occupancy));
            if (occupancy == GridOccupancyFilter.StopBeforeOccupied)
                return Record(UnsupportedFilter(kind, "StopBeforeOccupied is only valid for directional lines."));

            var results = new List<GridCoordinate>(8);
            if (mode == GridNeighbourMode.Cardinal || mode == GridNeighbourMode.All)
                AddNeighbours(origin, CardinalDirectionOrder, occupancy, results);
            if (mode == GridNeighbourMode.Diagonal || mode == GridNeighbourMode.All)
                AddNeighbours(origin, DiagonalDirectionOrder, occupancy, results);
            return Record(GridQueryResult.Success(kind, results, $"{mode} neighbours from {origin}"));
        }

        /// <summary>Excludes origin and stops at the first outside/inactive cell or explicit occupied blocker.</summary>
        public GridQueryResult QueryDirectionalLine(
            GridCoordinate origin,
            GridDirection direction,
            int length,
            GridOccupancyFilter occupancy = GridOccupancyFilter.All)
        {
            const GridQueryKind kind = GridQueryKind.DirectionalLine;
            BeginQueryDiagnostics(true, origin, true, direction, false, default, default);
            var description = $"Line from {origin} toward {direction}, length {length}";
            var invalid = ValidateOrigin(kind, origin, description);
            if (invalid != null) return Record(invalid);
            if (!Enum.IsDefined(typeof(GridDirection), direction))
                return Record(GridQueryResult.Failed(kind, GridQueryFailure.InvalidDirection, description,
                    $"Direction '{direction}' is unsupported."));
            if (!IsKnownOccupancyFilter(occupancy)) return Record(InvalidFilter(kind, occupancy));
            if (length < 0)
                return Record(GridQueryResult.Failed(kind, GridQueryFailure.InvalidLength, description,
                    "Line length cannot be negative."));

            var results = new List<GridCoordinate>(length);
            var step = DirectionOffset(direction);
            var current = origin;
            for (var distance = 0; distance < length; distance++)
            {
                current = new GridCoordinate(current.X + step.X, current.Z + step.Z);
                if (!state.Geometry.Contains(current))
                    return Record(GridQueryResult.Success(kind, results, description,
                        GridQueryTermination.OutsideGrid, true, current));
                if (!TryGetPlayableCell(current, out _))
                    return Record(GridQueryResult.Success(kind, results, description,
                        GridQueryTermination.InactiveCell, true, current));
                if (occupancy == GridOccupancyFilter.StopBeforeOccupied && TryGetOccupant(current, out _))
                    return Record(GridQueryResult.Success(kind, results, description,
                        GridQueryTermination.OccupiedCell, true, current));
                if (MatchesOccupancyFilter(current, occupancy)) results.Add(current);
            }

            return Record(GridQueryResult.Success(kind, results, description,
                GridQueryTermination.RequestedLengthReached));
        }

        /// <summary>Normalises inclusive bounds and returns active cells in Z-major, then X order.</summary>
        public GridQueryResult QueryRectangle(
            GridCoordinate first,
            GridCoordinate second,
            GridOccupancyFilter occupancy = GridOccupancyFilter.All)
        {
            const GridQueryKind kind = GridQueryKind.Rectangle;
            var description = $"Rectangle {first} to {second} (normalised)";
            var diagnosticMinimum = new GridCoordinate(Math.Min(first.X, second.X), Math.Min(first.Z, second.Z));
            var diagnosticMaximum = new GridCoordinate(Math.Max(first.X, second.X), Math.Max(first.Z, second.Z));
            BeginQueryDiagnostics(false, default, false, default, true, diagnosticMinimum, diagnosticMaximum);
            var readiness = ValidateReady(kind, description);
            if (readiness != null) return Record(readiness);
            if (!IsKnownOccupancyFilter(occupancy)) return Record(InvalidFilter(kind, occupancy));
            if (occupancy == GridOccupancyFilter.StopBeforeOccupied)
                return Record(UnsupportedFilter(kind, "StopBeforeOccupied is only valid for directional lines."));

            var minimumX = Math.Min(first.X, second.X);
            var maximumX = Math.Max(first.X, second.X);
            var minimumZ = Math.Min(first.Z, second.Z);
            var maximumZ = Math.Max(first.Z, second.Z);
            var results = new List<GridCoordinate>();
            for (var z = minimumZ; z <= maximumZ; z++)
            for (var x = minimumX; x <= maximumX; x++)
            {
                var coordinate = new GridCoordinate(x, z);
                if (TryGetPlayableCell(coordinate, out _) && MatchesOccupancyFilter(coordinate, occupancy))
                    results.Add(coordinate);
            }

            return Record(GridQueryResult.Success(kind, results, description));
        }

        /// <summary>Returns active cells by increasing distance, then Z, then X. Origin inclusion is explicit.</summary>
        public GridQueryResult QueryArea(
            GridCoordinate origin,
            int radius,
            GridDistanceMode distanceMode,
            bool includeOrigin,
            GridOccupancyFilter occupancy = GridOccupancyFilter.All)
        {
            const GridQueryKind kind = GridQueryKind.Area;
            BeginQueryDiagnostics(true, origin, false, default, false, default, default);
            var description = $"{distanceMode} area from {origin}, radius {radius}, origin {includeOrigin}";
            var invalid = ValidateOrigin(kind, origin, description);
            if (invalid != null) return Record(invalid);
            if (!Enum.IsDefined(typeof(GridDistanceMode), distanceMode))
                return Record(GridQueryResult.Failed(kind, GridQueryFailure.InvalidDistanceMode, description,
                    $"Distance mode '{distanceMode}' is unsupported."));
            if (!IsKnownOccupancyFilter(occupancy)) return Record(InvalidFilter(kind, occupancy));
            if (radius < 0)
                return Record(GridQueryResult.Failed(kind, GridQueryFailure.InvalidRadius, description,
                    "Area radius cannot be negative."));
            if (occupancy == GridOccupancyFilter.StopBeforeOccupied)
                return Record(UnsupportedFilter(kind, "StopBeforeOccupied is only valid for directional lines."));

            var candidates = new List<DistanceCandidate>();
            for (var z = origin.Z - radius; z <= origin.Z + radius; z++)
            for (var x = origin.X - radius; x <= origin.X + radius; x++)
            {
                var coordinate = new GridCoordinate(x, z);
                var deltaX = Math.Abs(x - origin.X);
                var deltaZ = Math.Abs(z - origin.Z);
                var distance = distanceMode == GridDistanceMode.Manhattan
                    ? deltaX + deltaZ
                    : Math.Max(deltaX, deltaZ);
                if (distance > radius || (!includeOrigin && coordinate == origin) ||
                    !TryGetPlayableCell(coordinate, out _) || !MatchesOccupancyFilter(coordinate, occupancy))
                    continue;
                candidates.Add(new DistanceCandidate(coordinate, distance));
            }

            candidates.Sort((left, right) =>
            {
                var byDistance = left.Distance.CompareTo(right.Distance);
                if (byDistance != 0) return byDistance;
                var byZ = left.Coordinate.Z.CompareTo(right.Coordinate.Z);
                return byZ != 0 ? byZ : left.Coordinate.X.CompareTo(right.Coordinate.X);
            });
            var results = new List<GridCoordinate>(candidates.Count);
            foreach (var candidate in candidates) results.Add(candidate.Coordinate);
            return Record(GridQueryResult.Success(kind, results, description));
        }

        /// <summary>Reports active cells adjacent to outside/inactive space in row-major order.</summary>
        public GridQueryResult QueryPlayableEdges()
        {
            const GridQueryKind kind = GridQueryKind.PlayableEdges;
            BeginQueryDiagnostics(false, default, false, default, false, default, default);
            const string description = "Playable arena edges";
            var readiness = ValidateReady(kind, description);
            if (readiness != null) return Record(readiness);

            var coordinates = new List<GridCoordinate>();
            var edges = new List<GridPlayableEdgeFact>();
            for (var z = 0; z < state.Geometry.Height; z++)
            for (var x = 0; x < state.Geometry.Width; x++)
            {
                var coordinate = new GridCoordinate(x, z);
                if (!TryGetPlayableCell(coordinate, out _)) continue;
                var flags = GridPlayableEdge.None;
                if (!IsPlayableOffset(coordinate, GridDirection.North)) flags |= GridPlayableEdge.North;
                if (!IsPlayableOffset(coordinate, GridDirection.East)) flags |= GridPlayableEdge.East;
                if (!IsPlayableOffset(coordinate, GridDirection.South)) flags |= GridPlayableEdge.South;
                if (!IsPlayableOffset(coordinate, GridDirection.West)) flags |= GridPlayableEdge.West;
                if (flags == GridPlayableEdge.None) continue;
                coordinates.Add(coordinate);
                edges.Add(new GridPlayableEdgeFact(coordinate, flags));
            }
            return Record(GridQueryResult.Success(kind, coordinates, description, edges: edges));
        }

        private void AddNeighbours(GridCoordinate origin, GridDirection[] directions,
            GridOccupancyFilter occupancy, List<GridCoordinate> results)
        {
            foreach (var direction in directions)
            {
                var step = DirectionOffset(direction);
                var candidate = new GridCoordinate(origin.X + step.X, origin.Z + step.Z);
                if (TryGetPlayableCell(candidate, out _) && MatchesOccupancyFilter(candidate, occupancy))
                    results.Add(candidate);
            }
        }

        private bool IsPlayableOffset(GridCoordinate origin, GridDirection direction)
        {
            var step = DirectionOffset(direction);
            return TryGetPlayableCell(new GridCoordinate(origin.X + step.X, origin.Z + step.Z), out _);
        }

        private bool MatchesOccupancyFilter(GridCoordinate coordinate, GridOccupancyFilter filter)
        {
            var occupied = TryGetOccupant(coordinate, out _);
            return filter == GridOccupancyFilter.All || filter == GridOccupancyFilter.StopBeforeOccupied ||
                   (filter == GridOccupancyFilter.OccupiedOnly && occupied) ||
                   (filter == GridOccupancyFilter.UnoccupiedOnly && !occupied);
        }

        private GridQueryResult ValidateOrigin(GridQueryKind kind, GridCoordinate origin, string description)
        {
            var readiness = ValidateReady(kind, description);
            if (readiness != null) return readiness;
            if (!state.Geometry.Contains(origin))
                return GridQueryResult.Failed(kind, GridQueryFailure.OriginOutsideGrid, description,
                    $"Origin {origin} is outside the Grid.");
            if (!TryGetPlayableCell(origin, out _))
                return GridQueryResult.Failed(kind, GridQueryFailure.OriginInactive, description,
                    $"Origin {origin} is inactive.");
            return null;
        }

        private GridQueryResult ValidateReady(GridQueryKind kind, string description) =>
            IsReady ? null : GridQueryResult.Failed(kind, GridQueryFailure.GridNotReady, description,
                "The Grid must be ready before spatial queries can run.");

        private static GridQueryResult UnsupportedFilter(GridQueryKind kind, string message) =>
            GridQueryResult.Failed(kind, GridQueryFailure.UnsupportedOccupancyFilter, kind.ToString(), message);

        private static GridQueryResult InvalidFilter(GridQueryKind kind, GridOccupancyFilter filter) =>
            GridQueryResult.Failed(kind, GridQueryFailure.InvalidOccupancyFilter, kind.ToString(),
                $"Occupancy filter '{filter}' is unsupported.");

        private static bool IsKnownOccupancyFilter(GridOccupancyFilter filter) =>
            Enum.IsDefined(typeof(GridOccupancyFilter), filter);

        private GridQueryResult Record(GridQueryResult result)
        {
            LastQueryResult = result;
            return result;
        }

        private void BeginQueryDiagnostics(
            bool hasOrigin, GridCoordinate origin,
            bool hasDirection, GridDirection direction,
            bool hasBounds, GridCoordinate minimumBounds, GridCoordinate maximumBounds)
        {
            LastQueryDiagnostics = new GridQueryDiagnosticContext(
                hasOrigin, origin, hasDirection, direction, hasBounds, minimumBounds, maximumBounds);
        }

        private static GridCoordinate DirectionOffset(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.North: return new GridCoordinate(0, 1);
                case GridDirection.East: return new GridCoordinate(1, 0);
                case GridDirection.South: return new GridCoordinate(0, -1);
                case GridDirection.West: return new GridCoordinate(-1, 0);
                case GridDirection.NorthEast: return new GridCoordinate(1, 1);
                case GridDirection.SouthEast: return new GridCoordinate(1, -1);
                case GridDirection.SouthWest: return new GridCoordinate(-1, -1);
                case GridDirection.NorthWest: return new GridCoordinate(-1, 1);
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private readonly struct DistanceCandidate
        {
            public GridCoordinate Coordinate { get; }
            public int Distance { get; }
            public DistanceCandidate(GridCoordinate coordinate, int distance)
            {
                Coordinate = coordinate;
                Distance = distance;
            }
        }
    }
}
