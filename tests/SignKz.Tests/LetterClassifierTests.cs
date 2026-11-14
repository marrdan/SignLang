using SignKz.Models;
using SignKz.Services;
using Xunit;

namespace SignKz.Tests;

public class LetterClassifierTests
{
    [Fact]
    public void Classify_ReturnsNull_WhenNoPose()
    {
        Assert.Null(LetterClassifier.Classify(null));
    }

    [Fact]
    public void Classify_ReturnsNull_WhenThereAreNoWorldPoints()
    {
        // 63 numbers only = no world points, so letters cannot be checked.
        HandPose? pose = HandPose.FromLandmarks(TestHelpers.MakeHand("01000"));

        Assert.Null(LetterClassifier.Classify(pose));
    }

    [Fact]
    public void FromLandmarks_SplitsImageAndWorldPoints()
    {
        double[] data = new double[126];
        for (int i = 0; i < 126; i++) data[i] = i;

        HandPose pose = HandPose.FromLandmarks(data)!;

        Assert.Equal(63, pose.Image!.Length);
        Assert.Equal(63, pose.World!.Length);
        Assert.Equal(63.0, pose.World[0]);
    }
}

public class LetterRecognizerTests
{
    [Fact]
    public void Recognize_IgnoresLetters_WhenPoseHasNoWorldPoints()
    {
        GestureRecognizer recognizer = new GestureRecognizer();
        List<Sign> letters = new List<Sign> { new Sign { Id = "b", Category = "Alphabet", ShapeCheck = true } };
        HandPose? pose = HandPose.FromLandmarks(TestHelpers.MakeHand("01111"));

        Assert.Null(recognizer.Recognize(pose, letters));
    }

    [Fact]
    public void Sign_WithShapeCheck_HasCameraCheck()
    {
        Assert.True(new Sign { ShapeCheck = true }.HasCameraCheck);
        Assert.True(new Sign { Pattern = "01000" }.HasCameraCheck);
        Assert.False(new Sign().HasCameraCheck);
    }
}

public class MotionTrackerTests
{
    // Feeds a list of (x, y) points with a palm size of 0.2.
    private static MotionTracker Feed(params double[][] points)
    {
        MotionTracker tracker = new MotionTracker();
        foreach (double[] p in points)
        {
            tracker.AddPoint(p[0], p[1], 0.2);
        }
        return tracker;
    }

    // A straight line from (x1, y1) to (x2, y2) in small steps.
    private static List<double[]> Line(double x1, double y1, double x2, double y2, int steps)
    {
        List<double[]> list = new List<double[]>();
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            list.Add(new double[] { x1 + (x2 - x1) * t, y1 + (y2 - y1) * t });
        }
        return list;
    }

    [Theory]
    [InlineData(1.0)]   // drawn to the right
    [InlineData(-1.0)]  // mirrored
    public void Detect_Z_ForThreeStrokes(double side)
    {
        List<double[]> path = new List<double[]>();
        path.Add(new double[] { 0.0, 0.0 });
        path.AddRange(Line(0.0, 0.0, side * 0.6, 0.0, 6));               // across
        path.AddRange(Line(side * 0.6, 0.0, 0.0, 0.6, 6));               // diagonal back and down
        path.AddRange(Line(0.0, 0.6, side * 0.6, 0.6, 6));               // across again

        MotionTracker tracker = Feed(path.ToArray());

        Assert.True(tracker.Detect("z"));
    }

    [Fact]
    public void Detect_Z_IsFalse_ForAStraightLine()
    {
        MotionTracker tracker = Feed(Line(0, 0, 1.0, 0, 18).ToArray());

        Assert.False(tracker.Detect("z"));
    }

    [Fact]
    public void Detect_J_ForDownThenHook()
    {
        List<double[]> path = new List<double[]>();
        path.Add(new double[] { 0.5, 0.0 });
        path.AddRange(Line(0.5, 0.0, 0.5, 0.5, 8));      // down
        path.AddRange(Line(0.5, 0.5, 0.2, 0.4, 4));      // curve to the side and up

        MotionTracker tracker = Feed(path.ToArray());

        Assert.True(tracker.Detect("j"));
    }

    [Fact]
    public void Detect_J_IsFalse_ForStillHand()
    {
        MotionTracker tracker = new MotionTracker();
        for (int i = 0; i < 20; i++) tracker.AddPoint(0.5, 0.5, 0.2);

        Assert.False(tracker.Detect("j"));
        Assert.Equal(0, tracker.Percent("j"));
    }

    [Fact]
    public void Add_ReturnsFalse_ForNoPose()
    {
        Assert.False(new MotionTracker().Add(null, "z"));
    }
}
