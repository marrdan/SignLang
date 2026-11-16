using SignKz.Models;

namespace SignKz.Services;

public interface IGestureRecognizer
{
    Sign? Recognize(HandPose? pose, List<Sign> signs);

    bool IsMatch(HandPose? pose, Sign target, List<Sign> signs);
}
