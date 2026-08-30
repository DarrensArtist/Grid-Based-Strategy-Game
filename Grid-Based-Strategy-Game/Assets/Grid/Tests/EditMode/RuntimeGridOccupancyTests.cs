using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class RuntimeGridOccupancyTests
    {
        private readonly List<ArenaGridProfile> profiles = new List<ArenaGridProfile>();
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            foreach (var profile in profiles)
            {
                Object.DestroyImmediate(profile);
            }

            profiles.Clear();
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlaceMoveRemove_UpdatesBothIndexesAndPublishesAfterCommit()
        {
            var grid = CreateReadyGrid();
            var occupant = new GridOccupantId("unit-1");
            var source = new GridCoordinate(0, 0);
            var destination = new GridCoordinate(2, 0);
            var notifications = 0;
            grid.OccupancyChanged += result =>
            {
                notifications++;
                Assert.That(result.Succeeded, Is.True);
                Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
            };

            Assert.That(grid.Place(occupant, source).Succeeded, Is.True);
            Assert.That(grid.TryGetOccupant(source, out var found), Is.True);
            Assert.That(found, Is.EqualTo(occupant));
            Assert.That(grid.TryGetOccupantLocation(occupant, out var location), Is.True);
            Assert.That(location, Is.EqualTo(source));

            Assert.That(grid.Move(occupant, source, destination).Succeeded, Is.True);
            Assert.That(grid.TryGetOccupant(source, out _), Is.False);
            Assert.That(grid.TryGetOccupant(destination, out found), Is.True);
            Assert.That(found, Is.EqualTo(occupant));

            Assert.That(grid.Remove(occupant, destination).Succeeded, Is.True);
            Assert.That(grid.TryGetOccupant(destination, out _), Is.False);
            Assert.That(grid.TryGetOccupantLocation(occupant, out _), Is.False);
            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(notifications, Is.EqualTo(3));
        }

        [Test]
        public void InvalidPlacements_ChangeNothingAndPublishNothing()
        {
            var grid = CreateReadyGrid();
            var notifications = 0;
            grid.OccupancyChanged += _ => notifications++;

            Assert.That(grid.Place(default, new GridCoordinate(0, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.InvalidOccupant));
            Assert.That(grid.Place(new GridOccupantId("outside"), new GridCoordinate(-1, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.DestinationOutsideGrid));
            Assert.That(grid.Place(new GridOccupantId("inactive"), new GridCoordinate(1, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.DestinationInactive));

            var first = new GridOccupantId("first");
            Assert.That(grid.Place(first, new GridCoordinate(0, 0)).Succeeded, Is.True);
            Assert.That(grid.Place(first, new GridCoordinate(2, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.OccupantAlreadyRegistered));
            Assert.That(grid.Place(new GridOccupantId("second"), new GridCoordinate(0, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.DestinationOccupied));

            Assert.That(grid.OccupiedCellCount, Is.EqualTo(1));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
        }

        [Test]
        public void FailedAndSameCellMoves_PreserveSourceAndPublishNothing()
        {
            var grid = CreateReadyGrid();
            var first = new GridOccupantId("first");
            var blocker = new GridOccupantId("blocker");
            var firstCell = new GridCoordinate(0, 0);
            var blockedCell = new GridCoordinate(2, 0);
            grid.Place(first, firstCell);
            grid.Place(blocker, blockedCell);
            var notifications = 0;
            grid.OccupancyChanged += _ => notifications++;

            Assert.That(grid.Move(first, firstCell, blockedCell).Failure,
                Is.EqualTo(GridOccupancyFailure.DestinationOccupied));
            Assert.That(grid.Move(first, new GridCoordinate(2, 1), new GridCoordinate(0, 1)).Failure,
                Is.EqualTo(GridOccupancyFailure.SourceMismatch));
            Assert.That(grid.Move(first, firstCell, firstCell).Failure,
                Is.EqualTo(GridOccupancyFailure.SameCellMove));

            Assert.That(grid.TryGetOccupantLocation(first, out var location), Is.True);
            Assert.That(location, Is.EqualTo(firstCell));
            Assert.That(notifications, Is.Zero);
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
        }

        [Test]
        public void WrongOrStaleRemoval_DoesNotClearCurrentOccupant()
        {
            var grid = CreateReadyGrid();
            var occupant = new GridOccupantId("unit");
            var coordinate = new GridCoordinate(0, 0);
            grid.Place(occupant, coordinate);

            Assert.That(grid.Remove(new GridOccupantId("other"), coordinate).Failure,
                Is.EqualTo(GridOccupancyFailure.OccupantMismatch));
            Assert.That(grid.Remove(occupant, new GridCoordinate(2, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.SourceMismatch));
            Assert.That(grid.TryGetOccupant(coordinate, out var remaining), Is.True);
            Assert.That(remaining, Is.EqualTo(occupant));
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
        }

        [Test]
        public void Reload_ClearsOccupancyAndRejectsStaleRequests()
        {
            var grid = CreateReadyGrid();
            var occupant = new GridOccupantId("unit");
            var coordinate = new GridCoordinate(0, 0);
            grid.Place(occupant, coordinate);

            Assert.That(grid.Reload(CreateProfile("replacement"), root.transform).Succeeded, Is.True);

            Assert.That(grid.OccupiedCellCount, Is.Zero);
            Assert.That(grid.TryGetOccupantLocation(occupant, out _), Is.False);
            Assert.That(grid.Move(occupant, coordinate, new GridCoordinate(2, 0)).Failure,
                Is.EqualTo(GridOccupancyFailure.OccupantNotRegistered));
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
        }

        [Test]
        public void LongMixedSequence_RemainsConsistent()
        {
            var grid = CreateReadyGrid();
            var coordinates = new[]
            {
                new GridCoordinate(0, 0), new GridCoordinate(2, 0),
                new GridCoordinate(0, 1), new GridCoordinate(2, 1)
            };
            var occupants = new[]
            {
                new GridOccupantId("a"), new GridOccupantId("b"), new GridOccupantId("c")
            };

            for (var index = 0; index < occupants.Length; index++)
            {
                Assert.That(grid.Place(occupants[index], coordinates[index]).Succeeded, Is.True);
                Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.True);
            }

            Assert.That(grid.Remove(occupants[1], coordinates[1]).Succeeded, Is.True);
            Assert.That(grid.Move(occupants[0], coordinates[0], coordinates[1]).Succeeded, Is.True);
            Assert.That(grid.Move(occupants[2], coordinates[2], coordinates[3]).Succeeded, Is.True);
            var report = grid.ScanOccupancyConsistency();
            Assert.That(report.IsConsistent, Is.True);
            Assert.That(report.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(report.RegisteredOccupantCount, Is.EqualTo(2));
        }

        [Test]
        public void MutationBeforeInitialization_ReturnsExplicitFailure()
        {
            var grid = new RuntimeGrid();
            var result = grid.Place(new GridOccupantId("unit"), new GridCoordinate(0, 0));

            Assert.That(result.Failure, Is.EqualTo(GridOccupancyFailure.GridNotReady));
            Assert.That(grid.ScanOccupancyConsistency().IsConsistent, Is.False);
        }

        private RuntimeGrid CreateReadyGrid()
        {
            root = new GameObject("Occupancy Test Grid");
            var grid = new RuntimeGrid();
            Assert.That(grid.Initialize(CreateProfile("occupancy"), root.transform).Succeeded, Is.True);
            return grid;
        }

        private ArenaGridProfile CreateProfile(string id)
        {
            var definitions = new[]
            {
                new ArenaCellDefinition(true, ArenaCellZone.TeamADeployment),
                new ArenaCellDefinition(false, ArenaCellZone.None),
                new ArenaCellDefinition(true, ArenaCellZone.TeamADeployment),
                new ArenaCellDefinition(true, ArenaCellZone.TeamBDeployment),
                new ArenaCellDefinition(false, ArenaCellZone.None),
                new ArenaCellDefinition(true, ArenaCellZone.TeamBDeployment)
            };
            var profile = ArenaGridProfile.CreateTransient(
                id, ArenaGridProfile.CurrentSchemaVersion, 3, 2, 1f, definitions, string.Empty);
            profiles.Add(profile);
            return profile;
        }
    }
}
