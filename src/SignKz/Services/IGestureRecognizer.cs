using SignKz.Models;

namespace SignKz.Services;

public interface IGestureRecognizer
{
    // Returns the sign that matches the hand pose, or null if nothing matches.
    Sign? Recognize(HandPose? pose, List<Sign> signs);

    // True when the pose is the target sign. Letters are checked "approximately": a letter that is
    // hard to show exactly is accepted when it is close enough.
    bool IsMatch(HandPose? pose, Sign target, List<Sign> signs);
}
