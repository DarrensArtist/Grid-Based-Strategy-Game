using NUnit.Framework;

namespace GridBasedStrategyGame.Grid.Tests
{
    public sealed class GridCoordinateTests
    {
        [Test]
        public void EqualCoordinates_HaveValueEqualityAndMatchingHashes()
        {
            var first = new GridCoordinate(3, 7);
            var second = new GridCoordinate(3, 7);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo("(3, 7)"));
        }
    }
}
