using System;

namespace GridBasedStrategyGame.Grid
{
    /// <summary>Immutable logical address within a two-dimensional backing grid.</summary>
    [Serializable]
    public readonly struct GridCoordinate : IEquatable<GridCoordinate>
    {
        public int X { get; }
        public int Z { get; }

        public GridCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(GridCoordinate other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is GridCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public override string ToString() => $"({X}, {Z})";

        public static bool operator ==(GridCoordinate left, GridCoordinate right) => left.Equals(right);

        public static bool operator !=(GridCoordinate left, GridCoordinate right) => !left.Equals(right);
    }
}
