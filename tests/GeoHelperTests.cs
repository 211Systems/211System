using _211system.Helpers;
using Xunit;

namespace _211system.Tests
{
    public class GeoHelperTests
    {
        [Fact]
        public void CalculateDistance_KnownPoints_ApproxKm()
        {
            double km = GeoHelper.CalculateDistance(
                52.2297, 21.0122,
                50.0647, 19.9450);

            Assert.InRange(km, 248, 256);
        }

        [Fact]
        public void CalculateDistance_SamePoint_ReturnsZero()
        {
            double km = GeoHelper.CalculateDistance(52.0, 21.0, 52.0, 21.0);

            Assert.Equal(0, km);
        }
    }
}