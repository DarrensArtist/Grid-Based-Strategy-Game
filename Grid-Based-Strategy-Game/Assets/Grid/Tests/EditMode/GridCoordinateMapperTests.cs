using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class GridCoordinateMapperTests
    {
        private GameObject rootObject;

        [TearDown]
        public void TearDown()
        {
            if (rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void OddGrid_CentreCoordinateMapsToRoot()
        {
            var mapper = CreateMapper(9, 9, 1f);

            Assert.That(mapper.TryGridToWorld(new GridCoordinate(4, 4), out var world, out var failure), Is.True);
            Assert.That(failure, Is.EqualTo(GridMappingFailure.None));
            Assert.That(world, Is.EqualTo(rootObject.transform.position).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void Constructor_RejectsUnvalidatedGeometry()
        {
            rootObject = new GameObject("Grid Root (Test)");

            Assert.Throws<ArgumentException>(() => new GridCoordinateMapper(default, rootObject.transform));
        }

        [Test]
        public void EvenGrid_CentralFourMapToHalfCellOffsets()
        {
            var mapper = CreateMapper(10, 10, 1f);
            var expected = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            var coordinates = new[]
            {
                new GridCoordinate(4, 4),
                new GridCoordinate(5, 4),
                new GridCoordinate(4, 5),
                new GridCoordinate(5, 5)
            };

            for (var index = 0; index < coordinates.Length; index++)
            {
                Assert.That(mapper.TryGridToWorld(coordinates[index], out var world, out _), Is.True);
                Assert.That(world, Is.EqualTo(expected[index]).Using(Vector3ComparerWithEqualsOperator.Instance));
            }
        }

        [TestCase(9, 9, 1f)]
        [TestCase(10, 6, 2.5f)]
        public void EveryCellCentre_RoundTrips(int width, int height, float cellSize)
        {
            var mapper = CreateMapper(width, height, cellSize);

            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var expected = new GridCoordinate(x, z);
                    Assert.That(mapper.TryGridToWorld(expected, out var world, out _), Is.True);
                    Assert.That(mapper.TryWorldToGrid(world, out var actual, out _), Is.True);
                    Assert.That(actual, Is.EqualTo(expected));
                }
            }
        }

        [Test]
        public void InvalidCoordinate_ReturnsFailureAndDefaultPosition()
        {
            var mapper = CreateMapper(3, 3, 1f);

            var success = mapper.TryGridToWorld(new GridCoordinate(-1, 0), out var world, out var failure);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.EqualTo(GridMappingFailure.CoordinateOutsideBackingGrid));
            Assert.That(world, Is.EqualTo(default(Vector3)));
        }

        [Test]
        public void OutsideWorldPoint_ReturnsFailureAndDefaultCoordinate()
        {
            var mapper = CreateMapper(3, 3, 1f);

            var success = mapper.TryWorldToGrid(new Vector3(1.5001f, 50f, 0f), out var coordinate, out var failure);

            Assert.That(success, Is.False);
            Assert.That(failure, Is.EqualTo(GridMappingFailure.WorldPointOutsideFootprint));
            Assert.That(coordinate, Is.EqualTo(default(GridCoordinate)));
        }

        [Test]
        public void ExactInternalBoundary_ResolvesTowardPositiveAxes()
        {
            var mapper = CreateMapper(4, 4, 1f);

            Assert.That(mapper.TryWorldToGrid(Vector3.zero, out var coordinate, out _), Is.True);
            Assert.That(coordinate, Is.EqualTo(new GridCoordinate(2, 2)));
        }

        [Test]
        public void ExactOuterEdges_AreInclusiveAndResolveToEdgeCells()
        {
            var mapper = CreateMapper(4, 2, 1f);

            Assert.That(mapper.TryWorldToGrid(new Vector3(-2f, 0f, -1f), out var minimum, out _), Is.True);
            Assert.That(minimum, Is.EqualTo(new GridCoordinate(0, 0)));
            Assert.That(mapper.TryWorldToGrid(new Vector3(2f, 0f, 1f), out var maximum, out _), Is.True);
            Assert.That(maximum, Is.EqualTo(new GridCoordinate(3, 1)));
        }

        [Test]
        public void HeightAboveFloor_IsIgnored()
        {
            var mapper = CreateMapper(3, 3, 1f);

            Assert.That(mapper.TryWorldToGrid(new Vector3(0f, 500f, 0f), out var coordinate, out _), Is.True);
            Assert.That(coordinate, Is.EqualTo(new GridCoordinate(1, 1)));
        }

        [Test]
        public void TranslatedAndRotatedRoot_PreservesRoundTrip()
        {
            var mapper = CreateMapper(7, 4, 1.25f);
            rootObject.transform.SetPositionAndRotation(new Vector3(12f, 3f, -8f), Quaternion.Euler(0f, 37f, 0f));
            var expected = new GridCoordinate(6, 1);

            Assert.That(mapper.TryGridToWorld(expected, out var world, out _), Is.True);
            Assert.That(world, Is.Not.EqualTo(new Vector3(3.75f, 0f, -0.625f)));
            Assert.That(mapper.TryWorldToGrid(world, out var actual, out _), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        private GridCoordinateMapper CreateMapper(int width, int height, float cellSize)
        {
            rootObject = new GameObject("Grid Root (Test)");
            Assert.That(GridGeometry.TryCreate(width, height, cellSize, out var geometry, out _), Is.True);
            return new GridCoordinateMapper(geometry, rootObject.transform);
        }
    }
}
