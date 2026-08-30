namespace GridBasedStrategyGame.Grid
{
    public enum RuntimeGridStatus
    {
        Uninitialised,
        Initialising,
        Ready,
        Failed
    }

    public enum GridInitializationFailure
    {
        None,
        AlreadyInitialised,
        NullProfile,
        NullGridRoot,
        InvalidProfileIdentity,
        UnsupportedSchemaVersion,
        InvalidLayoutChecksum,
        InvalidGeometry,
        BackingCellCountOverflow,
        LayoutLengthMismatch,
        InvalidCellZone,
        EmptyPlayableLayout,
        ActiveCellCountMismatch,
        ConstructionFailed
    }

    public readonly struct GridInitializationResult
    {
        public bool Succeeded { get; }
        public GridInitializationFailure Failure { get; }
        public string ProfileId { get; }
        public string Message { get; }

        private GridInitializationResult(
            bool succeeded,
            GridInitializationFailure failure,
            string profileId,
            string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            ProfileId = profileId;
            Message = message;
        }

        internal static GridInitializationResult Success(string profileId) =>
            new GridInitializationResult(true, GridInitializationFailure.None, profileId, string.Empty);

        internal static GridInitializationResult Failed(
            GridInitializationFailure failure,
            string profileId,
            string message) =>
            new GridInitializationResult(false, failure, profileId ?? string.Empty, message);
    }

    public readonly struct RuntimeGridSourceMetadata
    {
        public string ProfileId { get; }
        public int SchemaVersion { get; }
        public string LayoutChecksum { get; }

        internal RuntimeGridSourceMetadata(string profileId, int schemaVersion, string layoutChecksum)
        {
            ProfileId = profileId;
            SchemaVersion = schemaVersion;
            LayoutChecksum = layoutChecksum;
        }
    }
}
