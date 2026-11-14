using SignKz.Models;

namespace SignKz.Services;

public class GestureRecognizer : IGestureRecognizer
{
    // Looks only at the signs in the list. Pass signs of ONE category (numbers or letters),
    // because a number and a letter can have the same finger state.
    public Sign? Recognize(HandPose? pose, List<Sign> signs)
    {
        if (pose == null) return null;

        // 1. Signs with a finger pattern (the numbers).
        string pattern = pose.ToPattern();
        foreach (Sign sign in signs)
        {
            if (sign.HasPattern && sign.Pattern == pattern) return sign;
        }

        // 2. Signs checked by the full hand shape (the letters).
        string? letter = LetterClassifier.Classify(pose);
        if (letter != null)
        {
            foreach (Sign sign in signs)
            {
                if (sign.ShapeCheck && sign.Id == letter) return sign;
            }
        }
        return null;
    }

    public bool IsMatch(HandPose? pose, Sign target, List<Sign> signs)
    {
        if (pose == null) return false;

        Sign? seen = Recognize(pose, signs);
        if (seen != null && seen.Id == target.Id) return true;

        // Not an exact match: for letters, accept a close shape of the chosen letter.
        return target.ShapeCheck && LetterClassifier.Matches(pose, target.Id);
    }
}
