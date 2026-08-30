using System;
using System.Collections.Generic;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Opaque stable identity supplied by the runtime module that owns the entity.</summary>
    public readonly struct GridOccupantId : IEquatable<GridOccupantId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public GridOccupantId(string value) => Value = value;

        public bool Equals(GridOccupantId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is GridOccupantId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(GridOccupantId left, GridOccupantId right) => left.Equals(right);
        public static bool operator !=(GridOccupantId left, GridOccupantId right) => !left.Equals(right);
    }

    public enum GridOccupancyOperation
    {
        Place,
        Move,
        Remove
    }

    public enum GridOccupancyFailure
    {
        None,
        GridNotReady,
        InvalidOccupant,
        DestinationOutsideGrid,
        DestinationInactive,
        DestinationOccupied,
        OccupantAlreadyRegistered,
        OccupantNotRegistered,
        SourceMismatch,
        SameCellMove,
        OccupantMismatch
    }

    /// <summary>Result of one occupancy request. Successful results are also published post-commit.</summary>
    public readonly struct GridOccupancyResult
    {
        public bool Succeeded { get; }
        public GridOccupancyOperation Operation { get; }
        public GridOccupancyFailure Failure { get; }
        public GridOccupantId Occupant { get; }
        public bool HasSource { get; }
        public GridCoordinate Source { get; }
        public bool HasDestination { get; }
        public GridCoordinate Destination { get; }
        public string Message { get; }

        private GridOccupancyResult(
            bool succeeded,
            GridOccupancyOperation operation,
            GridOccupancyFailure failure,
            GridOccupantId occupant,
            bool hasSource,
            GridCoordinate source,
            bool hasDestination,
            GridCoordinate destination,
            string message)
        {
            Succeeded = succeeded;
            Operation = operation;
            Failure = failure;
            Occupant = occupant;
            HasSource = hasSource;
            Source = source;
            HasDestination = hasDestination;
            Destination = destination;
            Message = message ?? string.Empty;
        }

        internal static GridOccupancyResult Success(
            GridOccupancyOperation operation,
            GridOccupantId occupant,
            bool hasSource,
            GridCoordinate source,
            bool hasDestination,
            GridCoordinate destination) =>
            new GridOccupancyResult(true, operation, GridOccupancyFailure.None, occupant,
                hasSource, source, hasDestination, destination, string.Empty);

        internal static GridOccupancyResult Failed(
            GridOccupancyOperation operation,
            GridOccupancyFailure failure,
            GridOccupantId occupant,
            string message,
            bool hasSource = false,
            GridCoordinate source = default,
            bool hasDestination = false,
            GridCoordinate destination = default) =>
            new GridOccupancyResult(false, operation, failure, occupant,
                hasSource, source, hasDestination, destination, message);
    }

    /// <summary>Immutable result of checking both occupancy indexes for agreement.</summary>
    public sealed class GridOccupancyConsistencyReport
    {
        private readonly string[] errors;

        public bool IsConsistent => errors.Length == 0;
        public int OccupiedCellCount { get; }
        public int RegisteredOccupantCount { get; }
        public IReadOnlyList<string> Errors => errors;

        internal GridOccupancyConsistencyReport(
            int occupiedCellCount,
            int registeredOccupantCount,
            List<string> errors)
        {
            OccupiedCellCount = occupiedCellCount;
            RegisteredOccupantCount = registeredOccupantCount;
            this.errors = errors.ToArray();
        }
    }
}
