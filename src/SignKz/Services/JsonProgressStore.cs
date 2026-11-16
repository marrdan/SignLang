using System.Text.Json;
using SignKz.Models;

namespace SignKz.Services;

// Saves all users to one JSON file. Can be replaced by a database (EF Core) later,
// because the rest of the code uses only the IProgressStore interface.
public class JsonProgressStore : IProgressStore
{
    private readonly string _filePath;

    public JsonProgressStore(string filePath)
    {
        _filePath = filePath;
    }

    public List<UserProgress> LoadAll()
    {
        if (!File.Exists(_filePath))
        {
            return new List<UserProgress>();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            List<UserProgress>? users = JsonSerializer.Deserialize<List<UserProgress>>(json); // turns json into a list of UserProgress objects
            return users ?? new List<UserProgress>();
        }
        catch (JsonException)
        {
            File.Move(_filePath, _filePath + ".bad", true);
            return new List<UserProgress>();
        }
    }

    public void SaveAll(IEnumerable<UserProgress> users)
    {
        string? folder = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.WriteIndented = true;
        string json = JsonSerializer.Serialize(users, options); // turn list of UserProgress objects into json text

        string tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, true);
    }
}
