using SignKz.Models;
using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

public class GestureRecognizerTests
{
    private readonly GestureRecognizer _recognizer = new GestureRecognizer();

    [Fact]
    public void Recognize_ReturnsMatchingSign()
    {
        HandPose? pose = HandPose.FromLandmarks(TestHelpers.MakeHand("11111"));

        Sign? result = _recognizer.Recognize(pose, TestHelpers.MakeSigns());

        Assert.NotNull(result);
        Assert.Equal("n5", result!.Id);
    }

    [Fact]
    public void Recognize_ReturnsNull_ForUnknownPose()
    {
        HandPose? pose = HandPose.FromLandmarks(TestHelpers.MakeHand("00100"));

        Assert.Null(_recognizer.Recognize(pose, TestHelpers.MakeSigns()));
    }

    [Fact]
    public void Recognize_ReturnsNull_WhenNoHand()
    {
        Assert.Null(_recognizer.Recognize(null, TestHelpers.MakeSigns()));
    }
}
