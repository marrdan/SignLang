namespace SignKz.Models;

// One sign (gesture) that the user can learn.
public class Sign
{
    public string Id { get; set; } = "";

    // "Numbers" or "Alphabet"
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";

    // Optional photo and video. Put your own links here, or drop files named like the sign id
    // into wwwroot/media (for example 1.jpg or a.png) and they are found automatically.
    // A file with another name (like first.png) can be set here: "media/first.png".
    public string ImageUrl { get; set; } = "";
    public string VideoUrl { get; set; } = "";

    // Optional. 5 characters: Thumb, Index, Middle, Ring, Pinky ('1' = up, '0' = down).
    // Used for the numbers. If it is empty and ShapeCheck is false, the camera cannot check this sign.
    public string Pattern { get; set; } = "";

    // true = the camera checks this sign with LetterClassifier (the full hand shape, not only the fingers).
    public bool ShapeCheck { get; set; }

    // true = the letter is drawn in the air (J, Z). MotionTracker checks the path of the fingertip.
    public bool Motion { get; set; }

    // Step-by-step guide: how to form the hand shape.
    public List<string> Steps { get; set; } = new List<string>();

    // Set to true only after the gesture was checked with an official source.
    public bool IsVerified { get; set; }

    public bool HasPattern => Pattern.Length == 5;
    public bool HasCameraCheck => HasPattern || ShapeCheck || Motion;
    public bool HasImage => ImageUrl.Length > 0;
    public bool HasVideo => VideoUrl.Length > 0;
    public bool HasSteps => Steps.Count > 0;

    // Can be shown in a quiz: the user must be able to SEE the sign, so a photo is needed.
    public bool HasVisual => HasImage;
}
