namespace SignKz.Services;

// Counts how many frames in a row the user shows the right sign.
// This avoids false success when the hand just passes through the pose.
public class HoldTracker
{
    private readonly int _framesNeeded;
    private int _count;

    public HoldTracker(int framesNeeded)
    {
        if (framesNeeded < 1) throw new ArgumentException("framesNeeded must be at least 1.");
        _framesNeeded = framesNeeded;
    }

    public int Percent
    {
        get
        {
            int value = _count * 100 / _framesNeeded;
            return value > 100 ? 100 : value;
        }
    }

    // Call it for every camera frame. Returns true when the sign was held long enough.
    public bool Update(bool isMatch)
    {
        if (isMatch) _count++;
        else _count = 0;

        return _count >= _framesNeeded;
    }

    public void Reset()
    {
        _count = 0;
    }
}
