using SkyGlance.Core.Places;

namespace SkyGlance.Core.Tests;

public class PlaceTests
{
    [Theory]
    [InlineData(-1, 2, 1, 0)]
    [InlineData(0, 2, 1, 1)]
    [InlineData(1, 2, 1, -1)]
    [InlineData(-1, 2, -1, 1)]
    [InlineData(-1, 0, 1, -1)]
    [InlineData(5, 2, 1, 0)]
    [InlineData(0, 3, 7, -1)]
    [InlineData(0, 3, 6, 2)]
    public void Steps_through_my_location_and_cities(int current, int count, int steps, int expected) =>
        Assert.Equal(expected, CityCycle.Step(current, count, steps));

    [Theory]
    [InlineData(1, 1, -1)]
    [InlineData(2, 0, 1)]
    [InlineData(0, 2, 0)]
    [InlineData(-1, 0, -1)]
    public void Removing_a_city_keeps_the_right_one_active(int current, int removed, int expected) =>
        Assert.Equal(expected, CityCycle.AfterRemove(current, removed));

    [Fact]
    public void Key_ignores_small_differences_and_culture()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("52.52,-13.41", Place.KeyOf(52.5201, -13.4099));
            Assert.Equal(new Place("A", 52.5201, 13.41).Key, new Place("B", 52.519, 13.4149).Key);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }
}
