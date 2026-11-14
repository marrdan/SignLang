using SignKz.Models;
using Xunit;

namespace SignKz.Tests;

public class HandPoseTests
{
    [Theory]
    [InlineData("00000")]
    [InlineData("10000")]
    [InlineData("01000")]
    [InlineData("11000")]
    [InlineData("01100")]
    [InlineData("01110")]
    [InlineData("01111")]
    [InlineData("11111")]
    [InlineData("10001")]
    [InlineData("00001")]
    public void FromLandmarks_DetectsFingerPattern(string pattern)
    {
        HandPose? pose = HandPose.FromLandmarks(TestHelpers.MakeHand(pattern));

        Assert.NotNull(pose);
        Assert.Equal(pattern, pose!.ToPattern());
    }

    [Fact]
    public void FromLandmarks_ReturnsNull_WhenDataIsNull()
    {
        Assert.Null(HandPose.FromLandmarks(null));
    }

    [Fact]
    public void FromLandmarks_ReturnsNull_WhenDataIsTooShort()
    {
        Assert.Null(HandPose.FromLandmarks(new double[10]));
    }
}
