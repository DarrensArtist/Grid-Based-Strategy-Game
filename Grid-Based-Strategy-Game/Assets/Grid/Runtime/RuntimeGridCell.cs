namespace GridBasedStrategyGame.Grid
{
    /// <summary>Immutable layout facts copied from an Arena Grid Profile.</summary>
    public readonly struct RuntimeGridCell
    {
        public GridCoordinate Coordinate { get; }
        public string StableIdentity { get; }
        public string SourceProfileId { get; }
        public bool IsActive { get; }
        public ArenaCellZone Zone { get; }

        internal RuntimeGridCell(
            GridCoordinate coordinate,
            string stableIdentity,
            string sourceProfileId,
            bool isActive,
            ArenaCellZone zone)
        {
            Coordinate = coordinate;
            StableIdentity = stableIdentity;
            SourceProfileId = sourceProfileId;
            IsActive = isActive;
            Zone = zone;
        }
    }
}
