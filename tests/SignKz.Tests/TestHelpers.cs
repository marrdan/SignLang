using SignKz.Models;

namespace SignKz.Tests;

public static class TestHelpers
{
    // Builds 21 fake hand points for an upright hand.
    // pattern = Thumb, Index, Middle, Ring, Pinky ('1' = finger up)
    public static double[] MakeHand(string pattern)
    {
        double[] d = new double[63];

        SetPoint(d, 0, 0.5, 0.9); // wrist

        double[] xs = { 0.4, 0.5, 0.6, 0.7 };
        for (int finger = 0; finger < 4; finger++)
        {
            int baseIndex = 5 + finger * 4;
            bool up = pattern[finger + 1] == '1';
            SetPoint(d, baseIndex, xs[finger], 0.65);
            SetPoint(d, baseIndex + 1, xs[finger], 0.55);
            SetPoint(d, baseIndex + 2, xs[finger], up ? 0.40 : 0.66);
            SetPoint(d, baseIndex + 3, xs[finger], up ? 0.25 : 0.68);
        }

        SetPoint(d, 1, 0.42, 0.85);
        SetPoint(d, 2, 0.38, 0.78);
        if (pattern[0] == '1')
        {
            SetPoint(d, 3, 0.28, 0.70);
            SetPoint(d, 4, 0.15, 0.60);
        }
        else
        {
            SetPoint(d, 3, 0.45, 0.72);
            SetPoint(d, 4, 0.50, 0.68);
        }
        return d;
    }

    private static void SetPoint(double[] data, int index, double x, double y)
    {
        data[index * 3] = x;
        data[index * 3 + 1] = y;
    }

    public static List<Sign> MakeSigns()
    {
        List<Sign> signs = new List<Sign>();
        signs.Add(new Sign { Id = "n1", Title = "1", Pattern = "01000" });
        signs.Add(new Sign { Id = "n2", Title = "2", Pattern = "01100" });
        signs.Add(new Sign { Id = "n5", Title = "5", Pattern = "11111" });
        signs.Add(new Sign { Id = "a", Title = "A", Pattern = "10000" });
        signs.Add(new Sign { Id = "l", Title = "L", Pattern = "11000" });
        return signs;
    }
}
