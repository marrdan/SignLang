using SignKz.Models;

namespace SignKz.Services;

public class GestureRecognizer : IGestureRecognizer
{

    public Sign? Recognize(HandPose? pose, List<Sign> signs)
    {
        if (pose == null) return null;

        string pattern = pose.ToPattern(); // we can take like 01010 (each finger up or down) and check if it matches a sign that has a pattern.
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

        
        return target.ShapeCheck && LetterClassifier.Matches(pose, target.Id);
    }
}
