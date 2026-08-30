using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class RuntimeGridSpatialQueryTests
    {
        private readonly List<ArenaGridProfile> profiles = new List<ArenaGridProfile>();
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            foreach (var profile in profiles) UnityEngine.Object.DestroyImmediate(profile);
            profiles.Clear();
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void CentreNeighbours_ReturnCardinalThenDiagonalInDocumentedOrder()
        {
            var grid = CreateGrid(Filled(3, 3), 3, 3);

            var result = grid.QueryNeighbours(new GridCoordinate(1, 1));

            AssertSuccess(result,
                C(1, 2), C(2, 1), C(1, 0), C(0, 1),
                C(2, 2), C(2, 0), C(0, 0), C(0, 2));
        }

        [Test]
        public void EdgeCornerAndCutCornerNeighbours_ExcludeOutsideAndInactiveCells()
        {
            var active = Filled(3, 3);
            active[1] = false;
            var grid = CreateGrid(active, 3, 3);

            AssertSuccess(grid.QueryNeighbours(C(0, 0), GridNeighbourMode.Cardinal), C(0, 1));
            AssertSuccess(grid.QueryNeighbours(C(0, 1), GridNeighbourMode.Diagonal), C(1, 2));
        }

        [Test]
        public void DirectionalLine_StopsAtInactiveGapAndNeverReenters()
        {
            var active = Filled(5, 1);
            active[2] = false;
            var grid = CreateGrid(active, 5, 1);

            var result = grid.QueryDirectionalLine(C(0, 0), GridDirection.East, 4);

            AssertSuccess(result, C(1, 0));
            Assert.That(result.Termination, Is.EqualTo(GridQueryTermination.InactiveCell));
            Assert.That(result.TerminationCoordinate, Is.EqualTo(C(2, 0)));
        }

        [Test]
        public void DirectionalLine_ReportsOutsideAndZeroLengthDeterministically()
        {
            var grid = CreateGrid(Filled(3, 1), 3, 1);

            var outside = grid.QueryDirectionalLine(C(1, 0), GridDirection.East, 5);
            AssertSuccess(outside, C(2, 0));
            Assert.That(outside.Termination, Is.EqualTo(GridQueryTermination.OutsideGrid));
            Assert.That(outside.TerminationCoordinate, Is.EqualTo(C(3, 0)));

            var zero = grid.QueryDirectionalLine(C(1, 0), GridDirection.East, 0);
            AssertSuccess(zero);
            Assert.That(zero.Termination, Is.EqualTo(GridQueryTermination.RequestedLengthReached));
        }

        [Test]
        public void Rectangle_NormalisesBoundsClipsAndReturnsActiveRowMajorOrder()
        {
            var active = Filled(4, 3);
            active[(1 * 4) + 1] = false;
            var grid = CreateGrid(active, 4, 3);

            var first = grid.QueryRectangle(C(3, 2), C(-1, 1));
            var repeated = grid.QueryRectangle(C(-1, 1), C(3, 2));

            AssertSuccess(first, C(0, 1), C(2, 1), C(3, 1), C(0, 2), C(1, 2), C(2, 2), C(3, 2));
            CollectionAssert.AreEqual(first.Coordinates, repeated.Coordinates);
        }

        [Test]
        public void Area_DistinguishesManhattanAndChebyshevAndControlsOrigin()
        {
            var grid = CreateGrid(Filled(5, 5), 5, 5);
            var origin = C(2, 2);

            var manhattan = grid.QueryArea(origin, 1, GridDistanceMode.Manhattan, false);
            AssertSuccess(manhattan, C(2, 1), C(1, 2), C(3, 2), C(2, 3));

            var chebyshev = grid.QueryArea(origin, 1, GridDistanceMode.Chebyshev, true);
            AssertSuccess(chebyshev,
                C(2, 2), C(1, 1), C(2, 1), C(3, 1),
                C(1, 2), C(3, 2), C(1, 3), C(2, 3), C(3, 3));

            AssertSuccess(grid.QueryArea(origin, 0, GridDistanceMode.Manhattan, false));
            AssertSuccess(grid.QueryArea(origin, 0, GridDistanceMode.Manhattan, true), origin);
        }

        [Test]
        public void OccupancyFilters_AreExplicitAndNeverMutateOccupancy()
        {
            var grid = CreateGrid(Filled(4, 1), 4, 1);
            var occupant = new GridOccupantId("blocker");
            Assert.That(grid.Place(occupant, C(2, 0)).Succeeded, Is.True);

            AssertSuccess(grid.QueryDirectionalLine(C(0, 0), GridDirection.East, 3),
                C(1, 0), C(2, 0), C(3, 0));
            AssertSuccess(grid.QueryDirectionalLine(C(0, 0), GridDirection.East, 3,
                GridOccupancyFilter.UnoccupiedOnly), C(1, 0), C(3, 0));
            AssertSuccess(grid.QueryDirectionalLine(C(0, 0), GridDirection.East, 3,
                GridOccupancyFilter.OccupiedOnly), C(2, 0));

            var stopped = grid.QueryDirectionalLine(C(0, 0), GridDirection.East, 3,
                GridOccupancyFilter.StopBeforeOccupied);
            AssertSuccess(stopped, C(1, 0));
            Assert.That(stopped.Termination, Is.EqualTo(GridQueryTermination.OccupiedCell));
            Assert.That(grid.TryGetOccupantLocation(occupant, out var location), Is.True);
            Assert.That(location, Is.EqualTo(C(2, 0)));
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
        }

        [Test]
        public void PlayableEdges_ReportOutsideAndConcaveBoundariesWithoutLegalityClaims()
        {
            var active = Filled(3, 3);
            active[4] = false;
            var grid = CreateGrid(active, 3, 3);

            var result = grid.QueryPlayableEdges();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Edges.Count, Is.EqualTo(8));
            var southWest = result.Edges[0];
            Assert.That(southWest.Coordinate, Is.EqualTo(C(0, 0)));
            Assert.That(southWest.Edges, Is.EqualTo(GridPlayableEdge.South | GridPlayableEdge.West));
            var southCentre = result.Edges[1];
            Assert.That(southCentre.Edges,
                Is.EqualTo(GridPlayableEdge.North | GridPlayableEdge.South));
            Assert.That(result.Description, Does.Not.Contain("legal").IgnoreCase);
            Assert.That(result.Description, Does.Not.Contain("attack").IgnoreCase);
        }

        [Test]
        public void InvalidRequests_FailExplicitlyWithoutFabricatingResults()
        {
            var active = Filled(2, 2);
            active[1] = false;
            var grid = CreateGrid(active, 2, 2);

            AssertFailure(grid.QueryNeighbours(C(-1, 0)), GridQueryFailure.OriginOutsideGrid);
            AssertFailure(grid.QueryNeighbours(C(1, 0)), GridQueryFailure.OriginInactive);
            AssertFailure(grid.QueryDirectionalLine(C(0, 0), GridDirection.East, -1),
                GridQueryFailure.InvalidLength);
            AssertFailure(grid.QueryArea(C(0, 0), -1, GridDistanceMode.Manhattan, true),
                GridQueryFailure.InvalidRadius);
            AssertFailure(grid.QueryDirectionalLine(C(0, 0), (GridDirection)999, 1),
                GridQueryFailure.InvalidDirection);
            AssertFailure(grid.QueryNeighbours(C(0, 0), GridNeighbourMode.All,
                GridOccupancyFilter.StopBeforeOccupied), GridQueryFailure.UnsupportedOccupancyFilter);
        }

        [Test]
        public void ResultsAreReadOnlyAndLastQueryDiagnosticsMatchReturnedResult()
        {
            var grid = CreateGrid(Filled(2, 2), 2, 2);
            var result = grid.QueryRectangle(C(0, 0), C(1, 1));

            Assert.That(grid.LastQueryResult, Is.SameAs(result));
            var list = (IList<GridCoordinate>)result.Coordinates;
            Assert.Throws<NotSupportedException>(() => list[0] = C(9, 9));
            Assert.That(grid.ActiveCellCount, Is.EqualTo(4));
        }

        [Test]
        public void QueryBeforeInitialization_FailsClearly()
        {
            var grid = new RuntimeGrid();
            AssertFailure(grid.QueryRectangle(C(0, 0), C(1, 1)), GridQueryFailure.GridNotReady);
        }

        private RuntimeGrid CreateGrid(bool[] active, int width, int height)
        {
            root = new GameObject("Spatial Query Test Grid");
            var definitions = new ArenaCellDefinition[active.Length];
            for (var index = 0; index < active.Length; index++)
                definitions[index] = new ArenaCellDefinition(active[index],
                    active[index] ? ArenaCellZone.Neutral : ArenaCellZone.None);
            var profile = ArenaGridProfile.CreateTransient("query-fixture",
                ArenaGridProfile.CurrentSchemaVersion, width, height, 1f, definitions, string.Empty);
            profiles.Add(profile);
            var grid = new RuntimeGrid();
            Assert.That(grid.Initialize(profile, root.transform).Succeeded, Is.True);
            return grid;
        }

        private static bool[] Filled(int width, int height)
        {
            var active = new bool[width * height];
            for (var index = 0; index < active.Length; index++) active[index] = true;
            return active;
        }

        private static GridCoordinate C(int x, int z) => new GridCoordinate(x, z);

        private static void AssertSuccess(GridQueryResult result, params GridCoordinate[] expected)
        {
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Failure, Is.EqualTo(GridQueryFailure.None));
            CollectionAssert.AreEqual(expected, result.Coordinates);
        }

        private static void AssertFailure(GridQueryResult result, GridQueryFailure failure)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(failure));
            Assert.That(result.Coordinates, Is.Empty);
            Assert.That(result.Message, Is.Not.Empty);
        }
    }
}
