using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

public class HoldTrackerTests
{
    [Fact]
    public void Update_ReturnsTrue_AfterEnoughFramesInARow()
    {
        HoldTracker tracker = new HoldTracker(3);

        Assert.False(tracker.Update(true));
        Assert.False(tracker.Update(true));
        Assert.True(tracker.Update(true));
    }

    [Fact]
    public void Update_ResetsCounter_WhenSignIsLost()
    {
        HoldTracker tracker = new HoldTracker(3);

        tracker.Update(true);
        tracker.Update(true);
        tracker.Update(false);

        Assert.False(tracker.Update(true));
        Assert.Equal(33, tracker.Percent);
    }

    [Fact]
    public void Constructor_Throws_WhenFramesNeededIsZero()
    {
        Assert.Throws<ArgumentException>(() => new HoldTracker(0));
    }
}
