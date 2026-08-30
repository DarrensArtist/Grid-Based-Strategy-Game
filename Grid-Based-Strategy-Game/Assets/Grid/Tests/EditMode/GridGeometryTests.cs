using NUnit.Framework;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class GridGeometryTests
    {
        [TestCase(0, 2, 1f, GridGeometryValidationError.WidthMustBePositive)]
        [TestCase(2, 0, 1f, GridGeometryValidationError.HeightMustBePositive)]
        [TestCase(2, 2, 0f, GridGeometryValidationError.CellSizeMustBeFiniteAndPositive)]
        [TestCase(2, 2, float.NaN, GridGeometryValidationError.CellSizeMustBeFiniteAndPositive)]
        public void TryCreate_InvalidInput_ReturnsSpecificFailure(
            int width,
            int height,
            float cellSize,
            GridGeometryValidationError expected)
        {
            var success = GridGeometry.TryCreate(width, height, cellSize, out _, out var failure);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.EqualTo(expected));
        }

        [Test]
        public void Contains_UsesSingleBackingGridBoundsRule()
        {
            Assert.That(GridGeometry.TryCreate(3, 2, 1f, out var geometry, out _), Is.True);

            Assert.That(geometry.Contains(new GridCoordinate(0, 0)), Is.True);
            Assert.That(geometry.Contains(new GridCoordinate(2, 1)), Is.True);
            Assert.That(geometry.Contains(new GridCoordinate(-1, 0)), Is.False);
            Assert.That(geometry.Contains(new GridCoordinate(3, 1)), Is.False);
            Assert.That(geometry.Contains(new GridCoordinate(2, 2)), Is.False);
        }

        [Test]
        public void DefaultGeometry_IsNotValid()
        {
            Assert.That(default(GridGeometry).IsValid, Is.False);
        }
    }
}
