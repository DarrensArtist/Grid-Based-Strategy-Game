using NUnit.Framework;
using UnityEngine;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class ArenaGridProfileTests
    {
        private ArenaGridProfile profile;

        [TearDown]
        public void TearDown()
        {
            if (profile != null)
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CreateTransient_ClonesSourceDefinitions()
        {
            var source = new[]
            {
                new ArenaCellDefinition(true, ArenaCellZone.Neutral)
            };
            profile = ArenaGridProfile.CreateTransient(
                "clone-test",
                ArenaGridProfile.CurrentSchemaVersion,
                1,
                1,
                1f,
                source,
                "checksum",
                1);

            source[0] = new ArenaCellDefinition(false, ArenaCellZone.None);

            Assert.That(profile.TryGetCellDefinition(new GridCoordinate(0, 0), out var stored), Is.True);
            Assert.That(stored.IsActive, Is.True);
            Assert.That(stored.Zone, Is.EqualTo(ArenaCellZone.Neutral));
        }

        [Test]
        public void TryGetCellDefinition_RejectsOutsideCoordinate()
        {
            profile = ArenaGridProfile.CreateTransient(
                "bounds-test",
                ArenaGridProfile.CurrentSchemaVersion,
                1,
                1,
                1f,
                new[] { new ArenaCellDefinition(true, ArenaCellZone.Neutral) },
                "checksum",
                1);

            Assert.That(profile.TryGetCellDefinition(new GridCoordinate(-1, 0), out _), Is.False);
            Assert.That(profile.TryGetCellDefinition(new GridCoordinate(1, 0), out _), Is.False);
        }
    }
}
