using System;

namespace GridBasedStrategyGame.Grid
{
    public enum GridGeometryValidationError
    {
        None,
        WidthMustBePositive,
        HeightMustBePositive,
        CellSizeMustBeFiniteAndPositive
    }

    /// <summary>Validated dimensions and cell size for a rectangular backing grid.</summary>
    public readonly struct GridGeometry : IEquatable<GridGeometry>
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public bool IsValid => Width > 0 && Height > 0 &&
                               !float.IsNaN(CellSize) && !float.IsInfinity(CellSize) && CellSize > 0f;

        private GridGeometry(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
        }

        public static bool TryCreate(
            int width,
            int height,
            float cellSize,
            out GridGeometry geometry,
            out GridGeometryValidationError error)
        {
            geometry = default;

            if (width <= 0)
            {
                error = GridGeometryValidationError.WidthMustBePositive;
                return false;
            }

            if (height <= 0)
            {
                error = GridGeometryValidationError.HeightMustBePositive;
                return false;
            }

            if (float.IsNaN(cellSize) || float.IsInfinity(cellSize) || cellSize <= 0f)
            {
                error = GridGeometryValidationError.CellSizeMustBeFiniteAndPositive;
                return false;
            }

            geometry = new GridGeometry(width, height, cellSize);
            error = GridGeometryValidationError.None;
            return true;
        }

        public bool Contains(GridCoordinate coordinate) =>
            coordinate.X >= 0 && coordinate.X < Width &&
            coordinate.Z >= 0 && coordinate.Z < Height;

        public bool Equals(GridGeometry other) =>
            Width == other.Width && Height == other.Height && CellSize.Equals(other.CellSize);

        public override bool Equals(object obj) => obj is GridGeometry other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ CellSize.GetHashCode();
                return hashCode;
            }
        }
    }
}
