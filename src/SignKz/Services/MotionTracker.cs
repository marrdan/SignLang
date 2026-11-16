using SignKz.Models;

namespace SignKz.Services;

// j and z
public class MotionTracker
{
    private const int MaxPoints = 40;      
    private const int MinPoints = 6;
    private const int MaxMissedFrames = 5;  

    private readonly List<double[]> _path = new List<double[]>();
    private double _palm;
    private int _missed;

    public void Reset()
    {
        _path.Clear();
        _missed = 0;
    }

    // Call for every camera frame. Returns true when the hand has the right starting shape.
    public bool Add(HandPose? pose, string letter)
    {
        if (pose == null || pose.Image == null || !LetterClassifier.HasMotionShape(pose, letter))
        {
            Miss();
            return false;
        }

        double[] img = pose.Image;
        int tip = letter == "j" ? 20 : 8; // pinky tip for J, index tip for Z
        double dx = img[0] - img[27];
        double dy = img[1] - img[28];
        double palm = Math.Sqrt(dx * dx + dy * dy);

        AddPoint(img[tip * 3], img[tip * 3 + 1], palm);
        return true;
    }

    // Call when there is no hand (or the wrong shape) in a frame.
    public void Miss()
    {
        _missed++;
        if (_missed > MaxMissedFrames)
        {
            _path.Clear();
        }
    }

    public void AddPoint(double x, double y, double palm)
    {
        _missed = 0;
        if (palm <= 0) return;
        _palm = palm;

        if (_path.Count > 0)
        {
            double[] last = _path[_path.Count - 1];
            double dx = x - last[0];
            double dy = y - last[1];
            // A tiny move is camera noise or a still hand: skip it, so a pause does not change the path.
            if (Math.Sqrt(dx * dx + dy * dy) < _palm * 0.04) return;
        }

        _path.Add(new double[] { x, y });
        if (_path.Count > MaxPoints)
        {
            _path.RemoveAt(0);
        }
    }


    public int Percent(string letter)
    {
        if (_palm <= 0 || _path.Count < 2) return 0;

        double value = PathLength(_path, 0) / NeededLength(letter) * 100.0;
        return value > 100 ? 100 : (int)value;
    }


    public bool Detect(string letter)
    {
        if (_palm <= 0 || _path.Count < MinPoints) return false;

        // The path may begin with movement that is not part of the letter (the hand coming into view),
        // so try several start points. The letter must end at the newest point.
        for (int start = 0; start <= _path.Count - MinPoints; start += 2)
        {
            if (LooksLike(letter, start)) return true;
        }
        return false;
    }

    private double NeededLength(string letter)
    {
        return letter == "z" ? 2.0 * _palm : 1.2 * _palm;
    }

    private bool LooksLike(string letter, int start)
    {
        double total = PathLength(_path, start);
        if (total < NeededLength(letter)) return false;

        // Cut the path into three parts of equal length and look at where each part goes.
        double[] p0 = _path[start];
        double[] p1 = PointAt(start, total / 3.0);
        double[] p2 = PointAt(start, total * 2.0 / 3.0);
        double[] p3 = _path[_path.Count - 1];

        double s1x = p1[0] - p0[0], s1y = p1[1] - p0[1];
        double s2x = p2[0] - p1[0], s2y = p2[1] - p1[1];
        double s3x = p3[0] - p2[0], s3y = p3[1] - p2[1];

        if (letter == "z")
        {
            bool firstSideways = Math.Abs(s1x) > Math.Abs(s1y) && Math.Abs(s1x) > 0.4 * _palm;
            bool lastSideways = Math.Abs(s3x) > Math.Abs(s3y) && Math.Abs(s3x) > 0.4 * _palm;
            bool diagonalDown = s2y > 0.3 * Math.Abs(s2x) && s2y > 0.3 * _palm;
            bool comesBack = s2x * s1x < 0;     // the diagonal goes to the other side
            bool sameSideAgain = s3x * s1x > 0; // the last stroke goes like the first one
            return firstSideways && lastSideways && diagonalDown && comesBack && sameSideAgain;
        }

        if (letter == "j")
        {
            bool startsDown = s1y > Math.Abs(s1x) && s1y > 0.5 * _palm;
            bool notStillDown = s3y < 0.5 * s1y;  // the end curves away from "down"
            bool endMoves = Math.Abs(s3x) + Math.Abs(s3y) > 0.3 * _palm;
            return startsDown && notStillDown && endMoves;
        }

        return false;
    }


    private static double PathLength(List<double[]> path, int start)
    {
        double total = 0;
        for (int i = start + 1; i < path.Count; i++)
        {
            total += Distance(path[i - 1], path[i]);
        }
        return total;
    }

    // The point that is "distance" along the path, counted from the start point.
    private double[] PointAt(int start, double distance)
    {
        double walked = 0;
        for (int i = start + 1; i < _path.Count; i++)
        {
            double step = Distance(_path[i - 1], _path[i]);
            if (walked + step >= distance && step > 0)
            {
                double t = (distance - walked) / step;
                return new double[]
                {
                    _path[i - 1][0] + (_path[i][0] - _path[i - 1][0]) * t,
                    _path[i - 1][1] + (_path[i][1] - _path[i - 1][1]) * t
                };
            }
            walked += step;
        }
        return _path[_path.Count - 1];
    }
    

    private static double Distance(double[] a, double[] b)
    {
        double dx = a[0] - b[0];
        double dy = a[1] - b[1];
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
