namespace SignKz.Models;

// Which fingers are up. It is built from the 21 hand points (landmarks) sent by the camera script.
public class HandPose
{
    public bool Thumb { get; set; }
    public bool Index { get; set; }
    public bool Middle { get; set; }
    public bool Ring { get; set; }
    public bool Pinky { get; set; }

    // The raw points, used by LetterClassifier (letters need the full hand shape, not only the fingers).
    // Image = 21 x (x, y, z) in picture units. World = 21 x (x, y, z) in real 3D units (meters). World can be null.
    public double[]? Image { get; set; }
    public double[]? World { get; set; }

    // Example: thumb + index up -> "11000"
    public string ToPattern()
    {
        string result = "";
        result += Thumb ? "1" : "0";
        result += Index ? "1" : "0";
        result += Middle ? "1" : "0";
        result += Ring ? "1" : "0";
        result += Pinky ? "1" : "0";
        return result;
    }

    // data = 21 points * 3 numbers (x, y, z) = 63 numbers. The camera script can add 63 more numbers
    // (world points) after them. Returns null if the data is wrong.
    public static HandPose? FromLandmarks(double[]? data)
    {
        if (data == null || data.Length < 63)
        {
            return null;
        }

        HandPose pose = new HandPose();

        pose.Image = new double[63];
        Array.Copy(data, pose.Image, 63);
        if (data.Length >= 126)
        {
            pose.World = new double[63];
            Array.Copy(data, 63, pose.World, 0, 63);
        }

        // Four fingers: finger is "up" when the tip is farther from the wrist than the middle joint.
        pose.Index = IsFingerUp(data, 8, 6);
        pose.Middle = IsFingerUp(data, 12, 10);
        pose.Ring = IsFingerUp(data, 16, 14);
        pose.Pinky = IsFingerUp(data, 20, 18);

        // Thumb: it moves sideways, so it needs a different check.
        pose.Thumb = IsThumbUp(data);

        return pose;
    }

    private static bool IsFingerUp(double[] data, int tip, int joint)
    {
        double tipDistance = Distance(data, 0, tip);
        double jointDistance = Distance(data, 0, joint);
        return tipDistance > jointDistance * RecognizerSettings.FingerMargin;
    }

    private static bool IsThumbUp(double[] data)
    {
        double palmSize = Distance(data, 0, 9);
        if (palmSize <= 0)
        {
            return false;
        }

        // The thumb tip must be far from the pinky side and far from the index base.
        bool awayFromPinky = Distance(data, 4, 17) > Distance(data, 3, 17);
        bool awayFromIndex = Distance(data, 4, 5) > palmSize * RecognizerSettings.ThumbSpread;
        return awayFromPinky && awayFromIndex;
    }

    // Distance between two points on the screen (we ignore z, because it is not very exact).
    private static double Distance(double[] data, int a, int b)
    {
        double dx = data[a * 3] - data[b * 3];
        double dy = data[a * 3 + 1] - data[b * 3 + 1];
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
