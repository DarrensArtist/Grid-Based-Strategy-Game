using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class BattlefieldPresenterTests
    {
        private readonly List<ArenaGridProfile> profiles = new List<ArenaGridProfile>();
        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in objects)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
            objects.Clear();

            foreach (var profile in profiles)
            {
                if (profile != null)
                {
                    UnityEngine.Object.DestroyImmediate(profile);
                }
            }
            profiles.Clear();
        }

        [Test]
        public void RebuildBeforeReady_ReturnsUsefulFailureAndCreatesNothing()
        {
            var presenter = CreatePresenter();
            presenter.Bind(new RuntimeGrid());

            var result = presenter.Rebuild();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BattlefieldPresentationFailure.GridNotReady));
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(presenter.SurfaceCount, Is.Zero);
        }

        [Test]
        public void ReadyNotification_RendersOnlyActiveCellsWithAuthoritativeZones()
        {
            var definitions = new[]
            {
                new ArenaCellDefinition(true, ArenaCellZone.TeamADeployment),
                new ArenaCellDefinition(false, ArenaCellZone.None),
                new ArenaCellDefinition(true, ArenaCellZone.Neutral),
                new ArenaCellDefinition(true, ArenaCellZone.TeamBDeployment)
            };
            var profile = CreateProfile("cut-corner", 2, 2, definitions, 3);
            var root = CreateObject("Grid Root");
            var grid = new RuntimeGrid();
            var presenter = CreatePresenter();
            presenter.Bind(grid);

            Assert.That(grid.Initialize(profile, root.transform).Succeeded, Is.True);

            Assert.That(presenter.SurfaceCount, Is.EqualTo(3));
            AssertPresentation(presenter, "cut-corner:0:0", ArenaCellZone.TeamADeployment);
            Assert.That(presenter.TryGetPresentation("cut-corner:1:0", out _), Is.False);
            AssertPresentation(presenter, "cut-corner:0:1", ArenaCellZone.Neutral);
            AssertPresentation(presenter, "cut-corner:1:1", ArenaCellZone.TeamBDeployment);
        }

        [Test]
        public void SurfacePositionComesFromGridCentreAndFollowsTransformedRoot()
        {
            var profile = CreateProfile(
                "transformed",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var root = CreateObject("Grid Root");
            root.transform.SetPositionAndRotation(new Vector3(7f, 2f, -4f), Quaternion.Euler(0f, 45f, 0f));
            var grid = new RuntimeGrid();
            Assert.That(grid.Initialize(profile, root.transform).Succeeded, Is.True);
            var presenter = CreatePresenter();
            presenter.Bind(grid);

            Assert.That(presenter.TryGetPresentation("transformed:0:0", out var presented), Is.True);
            Assert.That(grid.TryGetCellCentre(new GridCoordinate(0, 0), out var expected), Is.True);
            Assert.That(presented.Transform.position,
                Is.EqualTo(expected).Using(Vector3ComparerWithEqualsOperator.Instance));

            root.transform.SetPositionAndRotation(new Vector3(-3f, 1f, 8f), Quaternion.Euler(0f, 90f, 0f));
            Assert.That(grid.TryGetCellCentre(new GridCoordinate(0, 0), out var movedExpected), Is.True);
            Assert.That(presented.Transform.position,
                Is.EqualTo(movedExpected).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ManualVisualDrift_DoesNotChangeLogicalCell()
        {
            var profile = CreateProfile(
                "drift",
                1,
                1,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var root = CreateObject("Grid Root");
            var grid = new RuntimeGrid();
            grid.Initialize(profile, root.transform);
            var presenter = CreatePresenter();
            presenter.Bind(grid);
            presenter.TryGetPresentation("drift:0:0", out var presented);

            presented.Transform.position = new Vector3(100f, 100f, 100f);

            Assert.That(grid.TryGetPlayableCell(new GridCoordinate(0, 0), out var logical), Is.True);
            Assert.That(logical.Coordinate, Is.EqualTo(new GridCoordinate(0, 0)));
            Assert.That(logical.StableIdentity, Is.EqualTo("drift:0:0"));
        }

        [Test]
        public void Rebuild_RecreatesSameFootprintAndIdentities()
        {
            var profile = CreateAllActiveProfile("rebuild", 3, 3);
            var root = CreateObject("Grid Root");
            var grid = new RuntimeGrid();
            grid.Initialize(profile, root.transform);
            var presenter = CreatePresenter();
            presenter.Bind(grid);
            presenter.TryGetPresentation("rebuild:2:2", out var before);

            var result = presenter.Rebuild();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.SurfaceCount, Is.EqualTo(9));
            Assert.That(presenter.TryGetPresentation("rebuild:2:2", out var after), Is.True);
            Assert.That(after.StableIdentity, Is.EqualTo(before.StableIdentity));
            Assert.That(after.Transform, Is.Not.SameAs(before.Transform));
            Assert.That(grid.ActiveCellCount, Is.EqualTo(9));
        }

        [Test]
        public void FailedReload_ClearsPresenterOwnedVisualsOnly()
        {
            var valid = CreateAllActiveProfile("valid", 2, 2);
            var invalid = CreateProfile(
                "invalid",
                2,
                2,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                1);
            var root = CreateObject("Grid Root");
            var unrelated = CreateObject("Unrelated Scene Object");
            var grid = new RuntimeGrid();
            grid.Initialize(valid, root.transform);
            var presenter = CreatePresenter();
            presenter.Bind(grid);

            grid.Reload(invalid, root.transform);

            Assert.That(presenter.SurfaceCount, Is.Zero);
            Assert.That(unrelated, Is.Not.Null);
        }

        [Test]
        public void DiagnosticLayers_AreIndependentAndOffByDefault()
        {
            var diagnosticsObject = CreateObject("Diagnostics");
            var diagnostics = diagnosticsObject.AddComponent<RuntimeGridDiagnostics>();

            Assert.That(diagnostics.AnyLayerVisible, Is.False);
            Assert.That(diagnostics.ShowCoordinates, Is.False);
            Assert.That(diagnostics.ShowStableIdentities, Is.False);

            diagnostics.SetLayers(false, false, false, false, false, true, false);
            Assert.That(diagnostics.ShowCoordinates, Is.True);
            Assert.That(diagnostics.ShowStableIdentities, Is.False);
        }

        [Test]
        public void RepresentativeLargeGrid_BuildsWithLabelsDisabled()
        {
            const int size = 32;
            var profile = CreateAllActiveProfile("large", size, size);
            var root = CreateObject("Grid Root");
            var grid = new RuntimeGrid();
            grid.Initialize(profile, root.transform);
            var presenter = CreatePresenter();
            var stopwatch = Stopwatch.StartNew();

            presenter.Bind(grid);
            stopwatch.Stop();

            Assert.That(presenter.SurfaceCount, Is.EqualTo(size * size));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(10d));
        }

        private BattlefieldPresenter CreatePresenter()
        {
            var gameObject = CreateObject("Battlefield Presenter");
            return gameObject.AddComponent<BattlefieldPresenter>();
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            return gameObject;
        }

        private ArenaGridProfile CreateAllActiveProfile(string id, int width, int height)
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
            return CreateProfile(id, width, height, definitions, definitions.Length);
        }

        private ArenaGridProfile CreateProfile(
            string id,
            int width,
            int height,
            ArenaCellDefinition[] definitions,
            int activeCount)
        {
            var profile = ArenaGridProfile.CreateTransient(
                id,
                ArenaGridProfile.CurrentSchemaVersion,
                width,
                height,
                1f,
                definitions,
                $"{id}-checksum",
                activeCount);
            profiles.Add(profile);
            return profile;
        }

        private static void AssertPresentation(
            BattlefieldPresenter presenter,
            string identity,
            ArenaCellZone expectedZone)
        {
            Assert.That(presenter.TryGetPresentation(identity, out var presentation), Is.True);
            Assert.That(presentation.Zone, Is.EqualTo(expectedZone));
            Assert.That(presentation.Transform.GetComponent<Collider>(), Is.Null);
            Assert.That(presentation.Transform.GetComponent<MeshRenderer>(), Is.Not.Null);
        }
    }
}
