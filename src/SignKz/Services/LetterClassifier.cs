using SignKz.Models;

namespace SignKz.Services;

// Recognizes the letters of the fingerspelling alphabet from the 21 hand points.
public static class LetterClassifier
{
    private enum Finger { Open, Curved, Closed }
    private enum Direction { Up, Side, Down }


    private class Features
    {
        public double[] World = new double[63];
        public double Palm;

        // Bend of each finger in degrees: about 0 = straight, about 170 = fist.
        public double BendIndex;
        public double BendMiddle;
        public double BendRing;
        public double BendPinky;

//distance between thumb tip and other fingers
        public double ThumbFromIndexBase;
        public double ThumbToIndexTip;
        public double ThumbToMiddleTip;
        public double ThumbToMiddlePip;
        public double ThumbToRingPip;
        public double Spread;          // index tip to middle tip
        public double ThumbIndexAngle; // angle between the thumb and the index finger
        public Direction Direction;

        // Thumb tip position in the hand's own axes: U across the palm (0 = index side), H along the palm (up).
        public double U;
        public double H;
    }

    

   
    public static string? Classify(HandPose? pose)
    {
        Features? f = Measure(pose);
        if (f == null) return null;
        return ClassifyStrict(f);
    }

    // True when the pose is the given letter. Strict check first, then a loose one for the hard letters.
    public static bool Matches(HandPose? pose, string letter)
    {
        Features? f = Measure(pose);
        if (f == null) return false;

        if (ClassifyStrict(f) == letter) return true;
        return MatchesLoose(f, letter);
    }


    public static bool HasMotionShape(HandPose? pose, string letter)
    {
        Features? f = Measure(pose);
        if (f == null) return false;

        double straight = RecognizerSettings.LetterLooseStraight;
        double bent = RecognizerSettings.LetterLooseBent;

        if (letter == "j")
        {
            return f.BendPinky < straight && f.BendIndex > bent && f.BendMiddle > bent && f.BendRing > bent;
        }
        if (letter == "z")
        {
            return f.BendIndex < straight && f.BendMiddle > bent && f.BendRing > bent && f.BendPinky > bent;
        }
        return false;
    }


//measure

    private static Features? Measure(HandPose? pose)
    {
        if (pose == null) return null;

        double[]? w = pose.World;
        double[]? img = pose.Image;
        if (w == null || img == null || w.Length < 63 || img.Length < 63) return null;

        double s = Dist(w, 0, 9); // palm size
        if (s <= 0) return null;

        Features f = new Features();
        f.World = w;
        f.Palm = s;

        f.BendIndex = Bend(w, 5);
        f.BendMiddle = Bend(w, 9);
        f.BendRing = Bend(w, 13);
        f.BendPinky = Bend(w, 17);

        f.ThumbFromIndexBase = Dist(w, 4, 5) / s;
        f.ThumbToIndexTip = Dist(w, 4, 8) / s;
        f.ThumbToMiddleTip = Dist(w, 4, 12) / s;
        f.ThumbToMiddlePip = Dist(w, 4, 10) / s;
        f.ThumbToRingPip = Dist(w, 4, 14) / s;
        f.Spread = Dist(w, 8, 12) / s;
        f.ThumbIndexAngle = AngleBetween(w, 2, 4, 5, 8);
        f.Direction = GetDirection(img);

        double[] across = Normalize(Sub(Point(w, 17), Point(w, 5)));
        double[] along = Normalize(Sub(Point(w, 9), Point(w, 0)));// orientation of the hand
        double[] thumb = Sub(Point(w, 4), Point(w, 5));
        f.U = Dot(thumb, across) / s;
        f.H = Dot(thumb, along) / s;

        return f;
    }

    // Bend of the two upper joints of one finger (mcp = the first point of the finger).
    private static double Bend(double[] w, int mcp)
    {
        double pip = 180.0 - Angle(w, mcp, mcp + 1, mcp + 2);
        double dip = 180.0 - Angle(w, mcp + 1, mcp + 2, mcp + 3);
        return pip + dip;
    }

    private static Finger ToFinger(double bend)
    {
        if (bend < RecognizerSettings.LetterOpenBend) return Finger.Open;
        if (bend < RecognizerSettings.LetterClosedBend) return Finger.Curved;
        return Finger.Closed;
    }

    // strcit rules

    private static string? ClassifyStrict(Features f)
    {
        double[] w = f.World;
        Finger index = ToFinger(f.BendIndex);
        Finger middle = ToFinger(f.BendMiddle);
        Finger ring = ToFinger(f.BendRing);
        Finger pinky = ToFinger(f.BendPinky);

        double touch = RecognizerSettings.LetterTouch;
        bool thumbOut = f.ThumbFromIndexBase > RecognizerSettings.LetterThumbOut;

        // F: thumb and index touch, the other three fingers are straight.
        if (middle == Finger.Open && ring == Finger.Open && pinky == Finger.Open && f.ThumbToIndexTip < touch)
        {
            return "f";
        }

        // B: four straight fingers, thumb across the palm. (Thumb out would be the number 5.)
        if (index == Finger.Open && middle == Finger.Open && ring == Finger.Open && pinky == Finger.Open)
        {
            return thumbOut ? null : "b";
        }

        // W: three straight fingers, the thumb holds the pinky.
        if (index == Finger.Open && middle == Finger.Open && ring == Finger.Open)
        {
            return "w";
        }

        // R, K, P, H, V, U: index and middle straight, ring and pinky folded.
        if (index == Finger.Open && middle == Finger.Open && ring != Finger.Open && pinky != Finger.Open)
        {
            if (f.Spread < RecognizerSettings.LetterSpread && IsCrossed(w)) return "r";

            // K / P: the thumb touches the middle finger. V / U: the thumb is folded over the ring finger.
            bool thumbOnMiddle = f.ThumbToMiddlePip < 0.5 && f.ThumbToMiddlePip < f.ThumbToRingPip;
            if (thumbOnMiddle) return f.Direction == Direction.Down ? "p" : "k";

            if (f.Direction == Direction.Side) return "h";
            return f.Spread > RecognizerSettings.LetterSpread ? "v" : "u";
        }

        // D, L, G, Q: only the index finger is straight.
        if (index == Finger.Open && middle != Finger.Open && ring != Finger.Open && pinky != Finger.Open)
        {
            if (f.ThumbToMiddleTip < touch) return "d";

            if (thumbOut && f.ThumbIndexAngle > RecognizerSettings.LetterLAngle) return "l";

            if (f.ThumbFromIndexBase > 0.4 && f.ThumbIndexAngle <= RecognizerSettings.LetterLAngle)
            {
                if (f.Direction == Direction.Down) return "q";
                if (f.Direction == Direction.Side) return "g";
            }
            return null;
        }

        // Y / I: only the pinky is straight.
        if (pinky == Finger.Open && index != Finger.Open && middle != Finger.Open && ring != Finger.Open)
        {
            return thumbOut ? "y" : "i";
        }

        // X: the index finger is a hook, the others are in a fist.
        if (index == Finger.Curved && middle == Finger.Closed && ring == Finger.Closed && pinky == Finger.Closed)
        {
            return "x";
        }

        // C / O: all fingers are curved, not straight and not in a fist.
        if (index == Finger.Curved && middle == Finger.Curved && ring != Finger.Open && pinky != Finger.Open)
        {
            if (f.ThumbToIndexTip < touch) return "o";
            if (f.ThumbToIndexTip < 1.6) return "c";
            return null;
        }

        // Fist letters: A, T, S, E, N, M. They differ only by where the thumb is.
        if (index == Finger.Closed && middle == Finger.Closed && ring == Finger.Closed && pinky == Finger.Closed)
        {
            return ClassifyFist(f);
        }

        return null;
    }

    private static string ClassifyFist(Features f)
    {
        if (f.H >= 0.12)
        {
            // Thumb is high, on the front or the side of the fist.
            if (f.U < 0.12) return "a";
            if (f.U < 0.28) return "t";
            return "s";
        }

        // Thumb is low, tucked under the fingers.
        if (f.U >= 0.38) return "m";
        if (f.U >= 0.25) return "n";
        return "e";
    }

    // ---------------------------------------------------------------- loose rules

    // "Approximately right" for EVERY letter A-Y (J and Z are drawn in the air, see MotionTracker).
    // Each rule uses wide limits and looks only at the main idea of the letter:
    // which fingers are straight or bent, where the thumb is, which way the hand points.
    private static bool MatchesLoose(Features f, string letter)
    {
        double straight = RecognizerSettings.LetterLooseStraight;
        double bent = RecognizerSettings.LetterLooseBent;
        double fist = RecognizerSettings.LetterLooseFist;

        bool indexStraight = f.BendIndex < straight;
        bool middleStraight = f.BendMiddle < straight;
        bool ringStraight = f.BendRing < straight;
        bool pinkyStraight = f.BendPinky < straight;
        bool indexBent = f.BendIndex > bent;
        bool middleBent = f.BendMiddle > bent;
        bool ringBent = f.BendRing > bent;
        bool pinkyBent = f.BendPinky > bent;

        bool allFist = f.BendIndex > fist && f.BendMiddle > fist && f.BendRing > fist && f.BendPinky > fist;
        bool allStraight = indexStraight && middleStraight && ringStraight && pinkyStraight;

        // All four fingers curved (not straight, not a tight fist): C and O.
        bool allCurved = InRange(f.BendIndex, 20, 130) && InRange(f.BendMiddle, 20, 130)
            && InRange(f.BendRing, 20, 130) && InRange(f.BendPinky, 20, 130);

        // Index and middle straight, ring and pinky bent: U, V, R, H, K, P.
        bool twoFingers = indexStraight && middleStraight && ringBent && pinkyBent;

        // Only the index finger straight: D, G, Q, L.
        bool onlyIndex = indexStraight && middleBent && ringBent && pinkyBent;

        switch (letter)
        {
            case "a":
                // Fist, thumb on the side of it.
                return allFist && f.U < 0.25;
            case "b":
                // Four straight fingers, thumb not stuck far out.
                return allStraight && f.ThumbFromIndexBase < 0.85;
            case "c":
                // Curved hand with an open gap between the thumb and the fingers.
                return allCurved && f.ThumbToIndexTip > 0.4 && f.ThumbToIndexTip < 1.8;
            case "d":
                // Index straight, thumb touching the other fingers.
                return indexStraight && f.BendMiddle > 40 && ringBent && pinkyBent && f.ThumbToMiddleTip < 0.6;
            case "e":
                // Fingers bent down, thumb low under them.
                return f.BendIndex > 85 && f.BendMiddle > 85 && f.BendRing > 85 && f.BendPinky > 85 && f.H < 0.3;
            case "f":
                // Thumb and index touch, three fingers straight.
                return middleStraight && ringStraight && pinkyStraight && f.ThumbToIndexTip < 0.55;
            case "g":
                // Index and thumb side by side, hand not pointing up.
                return onlyIndex && f.ThumbIndexAngle < 55 && f.ThumbFromIndexBase > 0.3 && f.Direction != Direction.Up;
            case "h":
                // Index and middle straight, hand not pointing up.
                return twoFingers && f.Direction != Direction.Up;
            case "i":
                // Pinky straight, the others bent, thumb not stuck far out.
                return pinkyStraight && indexBent && middleBent && ringBent && f.ThumbFromIndexBase < 0.8;
            case "k":
                // Index and middle straight and apart, thumb touching the middle finger.
                return twoFingers && f.Spread > 0.2 && f.ThumbToMiddlePip < 0.6 && f.Direction != Direction.Down;
            case "l":
                // Index up, thumb away from it (an L), the rest folded.
                return onlyIndex && f.ThumbFromIndexBase > 0.5 && f.ThumbIndexAngle > 35;
            case "m":
                // Fist, thumb far under the fingers.
                return allFist && f.U >= 0.3 && f.H < 0.3;
            case "n":
                // Fist, thumb under the index and middle fingers.
                return allFist && f.U >= 0.12 && f.U < 0.55 && f.H < 0.3;
            case "o":
                // Curved fingers touching the thumb.
                return allCurved && f.ThumbToIndexTip < 0.55;
            case "p":
                // Index straight, thumb touching the middle finger, hand not pointing up.
                return indexStraight && ringBent && pinkyBent
                    && (f.ThumbToMiddlePip < 0.6 || f.ThumbToMiddleTip < 0.6) && f.Direction != Direction.Up;
            case "q":
                // Index and thumb side by side, hand pointing down.
                return onlyIndex && f.ThumbFromIndexBase > 0.3 && f.ThumbIndexAngle < 55 && f.Direction == Direction.Down;
            case "r":
                // Index and middle straight and close together (crossed).
                return twoFingers && f.Spread < 0.4;
            case "s":
                // Fist, thumb across the front of the fingers.
                return allFist && f.U >= 0.12 && f.H >= 0.0;
            case "t":
                // Fist, thumb between the index and middle fingers.
                return allFist && f.U >= 0.02 && f.U < 0.4 && f.H >= 0.0;
            case "u":
                // Index and middle straight and together.
                return twoFingers && f.Spread < 0.5 && f.Direction != Direction.Down;
            case "v":
                // Index and middle straight and a little apart.
                return twoFingers && f.Spread > 0.28 && f.Direction != Direction.Down;
            case "w":
                // Three straight fingers, the pinky folded.
                return indexStraight && middleStraight && ringStraight && f.BendPinky > 50;
            case "x":
                // Index bent like a hook, the others in a fist.
                return InRange(f.BendIndex, 40, 130) && f.BendMiddle > 90 && f.BendRing > 90 && f.BendPinky > 90;
            case "y":
                // Pinky straight, thumb out, the rest folded.
                return pinkyStraight && indexBent && middleBent && ringBent && f.ThumbFromIndexBase > 0.5;
            default:
                return false;
        }
    }

    private static bool InRange(double value, double min, double max)
    {
        return value >= min && value <= max;
    }

    // ---------------------------------------------------------------- helpers

    // Which way the hand points on the screen (from the wrist to the middle finger base).
    private static Direction GetDirection(double[] img)
    {
        double dx = img[9 * 3] - img[0];
        double dy = img[9 * 3 + 1] - img[1];
        double angle = Math.Abs(Math.Atan2(dx, -dy) * 180.0 / Math.PI); // 0 = up, 180 = down

        if (angle < RecognizerSettings.LetterUpAngle) return Direction.Up;
        if (angle > 180.0 - RecognizerSettings.LetterUpAngle) return Direction.Down;
        return Direction.Side;
    }

    // True when the index and middle fingertips changed sides compared with their bases.
    private static bool IsCrossed(double[] w)
    {
        double[] axis = Sub(Point(w, 17), Point(w, 5));
        double tips = Dot(Point(w, 8), axis) - Dot(Point(w, 12), axis);
        double bases = Dot(Point(w, 5), axis) - Dot(Point(w, 9), axis);
        return tips * bases < 0;
    }

    private static double[] Point(double[] data, int i)
    {
        return new double[] { data[i * 3], data[i * 3 + 1], data[i * 3 + 2] };
    }

    private static double[] Sub(double[] a, double[] b)
    {
        return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
    }

    private static double Dot(double[] a, double[] b)
    {
        return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }

    private static double Length(double[] a)
    {
        return Math.Sqrt(Dot(a, a));
    }

    private static double[] Normalize(double[] a)
    {
        double length = Length(a);
        if (length <= 0) return new double[] { 0, 0, 0 };
        return new double[] { a[0] / length, a[1] / length, a[2] / length };
    }

    private static double Dist(double[] data, int a, int b)
    {
        return Length(Sub(Point(data, a), Point(data, b)));
    }

    // Angle (degrees) at point b, between the lines b->a and b->c.
    private static double Angle(double[] data, int a, int b, int c)
    {
        double[] v1 = Sub(Point(data, a), Point(data, b));
        double[] v2 = Sub(Point(data, c), Point(data, b));
        return AngleOf(v1, v2);
    }

    // Angle (degrees) between the line a1->a2 and the line b1->b2.
    private static double AngleBetween(double[] data, int a1, int a2, int b1, int b2)
    {
        double[] v1 = Sub(Point(data, a2), Point(data, a1));
        double[] v2 = Sub(Point(data, b2), Point(data, b1));
        return AngleOf(v1, v2);
    }

    private static double AngleOf(double[] v1, double[] v2)
    {
        double l1 = Length(v1);
        double l2 = Length(v2);
        if (l1 <= 0 || l2 <= 0) return 0;

        double cos = Dot(v1, v2) / (l1 * l2);
        if (cos > 1) cos = 1;
        if (cos < -1) cos = -1;
        return Math.Acos(cos) * 180.0 / Math.PI;
    }
}
