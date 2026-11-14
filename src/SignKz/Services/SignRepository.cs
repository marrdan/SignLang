using System.Text.Json;
using SignKz.Models;

namespace SignKz.Services;

// Reads the list of signs from Data/signs.json. To add a new sign, edit that file.
// Photos and videos can be added by dropping files named like the sign id into the media folder.
public class SignRepository : ISignRepository
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm" };

    private readonly List<Sign> _signs;
    private readonly string _mediaFolder;

    public SignRepository(string filePath, string mediaFolder)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Signs file was not found.", filePath);
        }

        _mediaFolder = mediaFolder;

        string json = File.ReadAllText(filePath);
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.PropertyNameCaseInsensitive = true;

        List<Sign>? loaded = JsonSerializer.Deserialize<List<Sign>>(json, options);
        _signs = loaded ?? new List<Sign>();

        // Clean the links once: only safe links are allowed.
        foreach (Sign sign in _signs)
        {
            sign.ImageUrl = CleanLink(sign.ImageUrl);
            sign.VideoUrl = CleanLink(sign.VideoUrl);
        }
    }

    public List<Sign> GetAll()
    {
        FindMediaFiles();
        return new List<Sign>(_signs);
    }

    public Sign? GetById(string id)
    {
        FindMediaFiles();
        foreach (Sign sign in _signs)
        {
            if (sign.Id == id) return sign;
        }
        return null;
    }

    // If there is a file like media/1.jpg for sign "1", use it. Files are checked on every call,
    // so a new file appears after a page refresh (no restart needed).
    private void FindMediaFiles()
    {
        foreach (Sign sign in _signs)
        {
            if (sign.ImageUrl.Length == 0)
            {
                foreach (string ext in ImageExtensions)
                {
                    if (File.Exists(Path.Combine(_mediaFolder, sign.Id + ext)))
                    {
                        sign.ImageUrl = "media/" + sign.Id + ext;
                        break;
                    }
                }
            }

            if (sign.VideoUrl.Length == 0)
            {
                foreach (string ext in VideoExtensions)
                {
                    if (File.Exists(Path.Combine(_mediaFolder, sign.Id + ext)))
                    {
                        sign.VideoUrl = "media/" + sign.Id + ext;
                        break;
                    }
                }
            }
        }
    }

    // Allows only https://, http:// and local "media/..." links (blocks things like javascript:).
    private static string CleanLink(string? link)
    {
        string value = (link ?? "").Trim();
        if (value.StartsWith("https://") || value.StartsWith("http://") || value.StartsWith("media/"))
        {
            return value;
        }
        return "";
    }
}
