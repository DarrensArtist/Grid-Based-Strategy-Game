using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class RuntimeGridTests
    {
        private readonly List<ArenaGridProfile> profiles = new List<ArenaGridProfile>();
        private GameObject rootObject;

        [TearDown]
        public void TearDown()
        {
            foreach (var profile in profiles)
            {
                if (profile != null)
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                }
            }

            profiles.Clear();
            if (rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void ValidOddProfile_PublishesAllBackingCellsAndCorrectZones()
        {
            var profile = CreateThreeRegionProfile("odd", 3, 3);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(grid.Status, Is.EqualTo(RuntimeGridStatus.Ready));
            Assert.That(grid.BackingCellCount, Is.EqualTo(9));
            Assert.That(grid.ActiveCellCount, Is.EqualTo(9));
            AssertZone(grid, 1, 0, ArenaCellZone.TeamADeployment);
            AssertZone(grid, 1, 1, ArenaCellZone.Neutral);
            AssertZone(grid, 1, 2, ArenaCellZone.TeamBDeployment);
        }

        [Test]
        public void ValidEvenProfile_LoadsMinimumEvenFixture()
        {
            var profile = CreateThreeRegionProfile("even", 2, 2);
            var grid = CreateGrid();

            Assert.That(grid.Initialize(profile, rootObject.transform).Succeeded, Is.True);
            Assert.That(grid.BackingCellCount, Is.EqualTo(4));
            Assert.That(grid.ActiveCellCount, Is.EqualTo(4));
        }

        [Test]
        public void InactiveCoordinate_IsAvailableAsBackingButRejectedAsPlayable()
        {
            var definitions = new[]
            {
                new ArenaCellDefinition(true, ArenaCellZone.TeamADeployment),
                new ArenaCellDefinition(false, ArenaCellZone.None),
                new ArenaCellDefinition(true, ArenaCellZone.TeamBDeployment)
            };
            var profile = CreateProfile("inactive", 3, 1, definitions, 2);
            var grid = CreateGrid();
            grid.Initialize(profile, rootObject.transform);

            Assert.That(grid.TryGetBackingCell(new GridCoordinate(1, 0), out var backing), Is.True);
            Assert.That(backing.IsActive, Is.False);
            Assert.That(backing.Zone, Is.EqualTo(ArenaCellZone.None));
            Assert.That(grid.TryGetPlayableCell(new GridCoordinate(1, 0), out _), Is.False);
        }

        [Test]
        public void SourceMetadataAndStableCellIdentities_AreDeterministicAndInspectable()
        {
            var profile = CreateThreeRegionProfile("stable-profile", 3, 3, "layout-123");
            var firstGrid = CreateGrid();
            Assert.That(firstGrid.Initialize(profile, rootObject.transform).Succeeded, Is.True);
            Assert.That(firstGrid.TryGetBackingCell(new GridCoordinate(2, 1), out var firstCell), Is.True);

            var secondGrid = new RuntimeGrid();
            Assert.That(secondGrid.Initialize(profile, rootObject.transform).Succeeded, Is.True);
            Assert.That(secondGrid.TryGetBackingCell(new GridCoordinate(2, 1), out var secondCell), Is.True);

            Assert.That(firstCell.StableIdentity, Is.EqualTo("stable-profile:2:1"));
            Assert.That(secondCell.StableIdentity, Is.EqualTo(firstCell.StableIdentity));
            Assert.That(firstCell.SourceProfileId, Is.EqualTo("stable-profile"));
            Assert.That(firstGrid.SourceMetadata.ProfileId, Is.EqualTo("stable-profile"));
            Assert.That(firstGrid.SourceMetadata.SchemaVersion, Is.EqualTo(ArenaGridProfile.CurrentSchemaVersion));
            Assert.That(firstGrid.SourceMetadata.LayoutChecksum, Is.EqualTo(profile.LayoutChecksum));
        }

        [Test]
        public void Initialization_DoesNotModifySourceProfile()
        {
            var profile = CreateThreeRegionProfile("immutable-source", 3, 3);
            var before = ReadDefinitions(profile);
            var grid = CreateGrid();

            Assert.That(grid.Initialize(profile, rootObject.transform).Succeeded, Is.True);

            var after = ReadDefinitions(profile);
            Assert.That(after.Length, Is.EqualTo(before.Length));
            for (var index = 0; index < before.Length; index++)
            {
                Assert.That(after[index].IsActive, Is.EqualTo(before[index].IsActive));
                Assert.That(after[index].Zone, Is.EqualTo(before[index].Zone));
            }
        }

        [Test]
        public void LayoutLengthMismatch_FailsWithoutPublishingCells()
        {
            var profile = CreateProfile(
                "short-layout",
                2,
                2,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.LayoutLengthMismatch);
        }

        [TestCase(true, ArenaCellZone.None)]
        [TestCase(false, ArenaCellZone.TeamADeployment)]
        [TestCase(true, (ArenaCellZone)999)]
        public void InvalidActiveZoneCombination_FailsAtomically(bool active, ArenaCellZone zone)
        {
            var profile = CreateProfile(
                "invalid-zone",
                1,
                1,
                new[] { new ArenaCellDefinition(active, zone) },
                active ? 1 : 0);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.InvalidCellZone);
        }

        [Test]
        public void EmptyPlayableLayout_IsRejectedByRuntimeLoader()
        {
            var profile = CreateProfile(
                "empty",
                1,
                1,
                new[] { new ArenaCellDefinition(false, ArenaCellZone.None) },
                0);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.EmptyPlayableLayout);
        }

        [Test]
        public void UnsupportedSchemaVersion_IsRejectedBeforePublication()
        {
            var profile = CreateProfile(
                "future",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1,
                ArenaGridProfile.CurrentSchemaVersion + 1);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.UnsupportedSchemaVersion);
        }

        [Test]
        public void StaleAuthoredActiveCount_IsReplacedByDerivedCount()
        {
            var profile = CreateProfile(
                "wrong-summary",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                7);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(profile.ExpectedActiveCellCount, Is.EqualTo(1));
            Assert.That(grid.ActiveCellCount, Is.EqualTo(1));
        }

        [Test]
        public void BlankIdentity_IsRejectedBeforePublication()
        {
            var profile = CreateProfile(
                " ",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.InvalidProfileIdentity);
        }

        [Test]
        public void BlankStoredChecksum_IsRebuiltAndDoesNotBlockPublication()
        {
            var profile = CreateProfile(
                "checksum-required",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1,
                checksum: "");
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(profile.LayoutChecksum, Has.Length.EqualTo(64));
            Assert.That(grid.SourceMetadata.LayoutChecksum, Is.EqualTo(profile.LayoutChecksum));
        }

        [Test]
        public void InvalidGeometry_IsRejectedBeforeLayoutInspection()
        {
            var profile = CreateProfile(
                "invalid-geometry",
                0,
                1,
                new ArenaCellDefinition[0],
                0);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.InvalidGeometry);
        }

        [Test]
        public void BackingCellCountOverflow_ReturnsStructuredFailure()
        {
            var profile = CreateProfile(
                "overflow",
                int.MaxValue,
                2,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var grid = CreateGrid();

            var result = grid.Initialize(profile, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.BackingCellCountOverflow);
        }

        [Test]
        public void SecondInitialize_IsRejectedAndExplicitReloadReplacesLayout()
        {
            var first = CreateThreeRegionProfile("first", 3, 3);
            var second = CreateThreeRegionProfile("second", 2, 2);
            var grid = CreateGrid();
            Assert.That(grid.Initialize(first, rootObject.transform).Succeeded, Is.True);

            var accidental = grid.Initialize(second, rootObject.transform);
            Assert.That(accidental.Failure, Is.EqualTo(GridInitializationFailure.AlreadyInitialised));
            Assert.That(grid.IsReady, Is.True);
            Assert.That(grid.SourceMetadata.ProfileId, Is.EqualTo("first"));

            var deliberate = grid.Reload(second, rootObject.transform);
            Assert.That(deliberate.Succeeded, Is.True);
            Assert.That(grid.SourceMetadata.ProfileId, Is.EqualTo("second"));
            Assert.That(grid.BackingCellCount, Is.EqualTo(4));
        }

        [Test]
        public void FailedReload_PublishesNeitherPreviousNorCandidateState()
        {
            var valid = CreateThreeRegionProfile("valid", 3, 3);
            var invalid = CreateProfile(
                "invalid",
                2,
                2,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var grid = CreateGrid();
            Assert.That(grid.Initialize(valid, rootObject.transform).Succeeded, Is.True);

            var result = grid.Reload(invalid, rootObject.transform);

            AssertFailedWithoutState(grid, result, GridInitializationFailure.LayoutLengthMismatch);
        }

        [Test]
        public void ActiveCornerCentre_UsesSliceOneMappingUnderTransformedRoot()
        {
            var profile = CreateThreeRegionProfile("centres", 3, 3);
            var grid = CreateGrid();
            rootObject.transform.SetPositionAndRotation(new Vector3(4f, 2f, -3f), Quaternion.Euler(0f, 90f, 0f));
            Assert.That(grid.Initialize(profile, rootObject.transform).Succeeded, Is.True);

            Assert.That(grid.TryGetCellCentre(new GridCoordinate(0, 0), out var centre), Is.True);
            var expected = rootObject.transform.TransformPoint(new Vector3(-1f, 0f, -1f));
            Assert.That(centre, Is.EqualTo(expected).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grid.TryGetPlayableCell(new GridCoordinate(0, 0), out _), Is.True);
        }

        [Test]
        public void NullProfileAndNullRoot_ReturnStructuredFailures()
        {
            var grid = CreateGrid();
            var nullProfile = grid.Initialize(null, rootObject.transform);
            Assert.That(nullProfile.Failure, Is.EqualTo(GridInitializationFailure.NullProfile));

            var profile = CreateThreeRegionProfile("root-required", 3, 3);
            var nullRoot = grid.Initialize(profile, null);
            Assert.That(nullRoot.Failure, Is.EqualTo(GridInitializationFailure.NullGridRoot));
            Assert.That(grid.IsReady, Is.False);
        }

        private RuntimeGrid CreateGrid()
        {
            rootObject = new GameObject("Runtime Grid Root (Test)");
            return new RuntimeGrid();
        }

        private ArenaGridProfile CreateThreeRegionProfile(
            string profileId,
            int width,
            int height,
            string checksum = "fixture-checksum")
        {
            var definitions = new ArenaCellDefinition[width * height];
            for (var z = 0; z < height; z++)
            {
                var zone = z == 0
                    ? ArenaCellZone.TeamADeployment
                    : z == height - 1
                        ? ArenaCellZone.TeamBDeployment
                        : ArenaCellZone.Neutral;
                for (var x = 0; x < width; x++)
                {
                    definitions[(z * width) + x] = new ArenaCellDefinition(true, zone);
                }
            }

            return CreateProfile(profileId, width, height, definitions, definitions.Length, checksum: checksum);
        }

        private ArenaGridProfile CreateProfile(
            string profileId,
            int width,
            int height,
            ArenaCellDefinition[] definitions,
            int expectedActiveCount,
            int schemaVersion = ArenaGridProfile.CurrentSchemaVersion,
            string checksum = "fixture-checksum")
        {
            var profile = ArenaGridProfile.CreateTransient(
                profileId,
                schemaVersion,
                width,
                height,
                1f,
                definitions,
                checksum,
                expectedActiveCount);
            profiles.Add(profile);
            return profile;
        }

        private static ArenaCellDefinition[] ReadDefinitions(ArenaGridProfile profile)
        {
            var definitions = new ArenaCellDefinition[profile.CellDefinitionCount];
            for (var z = 0; z < profile.Height; z++)
            {
                for (var x = 0; x < profile.Width; x++)
                {
                    var coordinate = new GridCoordinate(x, z);
                    profile.TryGetCellDefinition(coordinate, out definitions[(z * profile.Width) + x]);
                }
            }

            return definitions;
        }

        private static void AssertZone(RuntimeGrid grid, int x, int z, ArenaCellZone expected)
        {
            Assert.That(grid.TryGetPlayableCell(new GridCoordinate(x, z), out var cell), Is.True);
            Assert.That(cell.Zone, Is.EqualTo(expected));
        }

        private static void AssertFailedWithoutState(
            RuntimeGrid grid,
            GridInitializationResult result,
            GridInitializationFailure expected)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(expected));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(grid.Status, Is.EqualTo(RuntimeGridStatus.Failed));
            Assert.That(grid.IsReady, Is.False);
            Assert.That(grid.BackingCellCount, Is.Zero);
            Assert.That(grid.ActiveCellCount, Is.Zero);
            Assert.That(grid.TryGetBackingCell(new GridCoordinate(0, 0), out _), Is.False);
        }
    }
}
