using System;
using UnityEngine;

namespace GridBasedStrategyGame.Grid
{
    public enum GridMappingFailure
    {
        None,
        CoordinateOutsideBackingGrid,
        WorldPointOutsideFootprint
    }

    /// <summary>
    /// Authoritative conversion between logical coordinates and cell-centred world positions.
    /// Exact internal boundaries resolve toward the positive axis. Outer edges are inclusive.
    /// </summary>
    public sealed class GridCoordinateMapper
    {
        private readonly GridGeometry geometry;
        private readonly Transform gridRoot;

        public GridGeometry Geometry => geometry;
        public Transform GridRoot => gridRoot;

        public GridCoordinateMapper(GridGeometry geometry, Transform gridRoot)
        {
            if (!geometry.IsValid)
            {
                throw new ArgumentException("Grid geometry must be created successfully through GridGeometry.TryCreate.", nameof(geometry));
            }

            this.geometry = geometry;
            this.gridRoot = gridRoot != null
                ? gridRoot
                : throw new ArgumentNullException(nameof(gridRoot));
        }

        public bool Contains(GridCoordinate coordinate) => geometry.Contains(coordinate);

        public bool TryGridToWorld(
            GridCoordinate coordinate,
            out Vector3 worldPosition,
            out GridMappingFailure failure)
        {
            if (!geometry.Contains(coordinate))
            {
                worldPosition = default;
                failure = GridMappingFailure.CoordinateOutsideBackingGrid;
                return false;
            }

            var localPosition = new Vector3(
                (coordinate.X - ((geometry.Width - 1) * 0.5f)) * geometry.CellSize,
                0f,
                (coordinate.Z - ((geometry.Height - 1) * 0.5f)) * geometry.CellSize);

            worldPosition = gridRoot.TransformPoint(localPosition);
            failure = GridMappingFailure.None;
            return true;
        }

        public bool TryWorldToGrid(
            Vector3 worldPosition,
            out GridCoordinate coordinate,
            out GridMappingFailure failure)
        {
            var localPosition = gridRoot.InverseTransformPoint(worldPosition);
            var halfWidth = geometry.Width * geometry.CellSize * 0.5f;
            var halfHeight = geometry.Height * geometry.CellSize * 0.5f;

            if (localPosition.x < -halfWidth || localPosition.x > halfWidth ||
                localPosition.z < -halfHeight || localPosition.z > halfHeight)
            {
                coordinate = default;
                failure = GridMappingFailure.WorldPointOutsideFootprint;
                return false;
            }

            var x = ResolveAxis(localPosition.x, halfWidth, geometry.CellSize, geometry.Width);
            var z = ResolveAxis(localPosition.z, halfHeight, geometry.CellSize, geometry.Height);
            coordinate = new GridCoordinate(x, z);
            failure = GridMappingFailure.None;
            return true;
        }

        private static int ResolveAxis(float localValue, float halfExtent, float cellSize, int count)
        {
            var resolved = Mathf.FloorToInt((localValue + halfExtent) / cellSize);
            return Mathf.Min(resolved, count - 1);
        }
    }
}
