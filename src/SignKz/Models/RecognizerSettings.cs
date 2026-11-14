namespace SignKz.Models;

// Numbers that can be tuned in front of a real camera.
public static class RecognizerSettings
{
    // Tip distance must be bigger than (joint distance * this number).
    public const double FingerMargin = 1.0;

    // Thumb tip to index base must be bigger than (palm size * this number).
    public const double ThumbSpread = 0.6;

    // How many camera frames in a row the sign must be held (about 10 frames per second).
    public const int HoldFrames = 6;

    // ---- Letters (LetterClassifier). Tune these in front of a real camera. ----

    // Finger bend = PIP angle + DIP angle (degrees). Below Open = straight finger, above Closed = fist.
    public const double LetterOpenBend = 45;
    public const double LetterClosedBend = 120;

    // Two points count as "touching" when their distance is smaller than (palm size * this number).
    public const double LetterTouch = 0.38;

    // Thumb tip to index base must be bigger than (palm size * this number) for the thumb to be "out".
    public const double LetterThumbOut = 0.75;

    // Index and middle tips farther apart than (palm size * this number) = V, closer = U.
    public const double LetterSpread = 0.42;

    // Angle (degrees) between the thumb and the index finger: bigger = L, smaller = G / Q.
    public const double LetterLAngle = 50;

    // Hand direction: within this many degrees of vertical = up or down, otherwise sideways.
    public const double LetterUpAngle = 55;

    // ---- Loose check ("approximately right") used when the person is practicing one chosen letter. ----

    // Bend (degrees) below this = the finger is straight enough.
    public const double LetterLooseStraight = 70;

    // Bend (degrees) above this = the finger is bent enough.
    public const double LetterLooseBent = 70;

    // Bend (degrees) above this = the finger is in a fist.
    public const double LetterLooseFist = 95;
}
