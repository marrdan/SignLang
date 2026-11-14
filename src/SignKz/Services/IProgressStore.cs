using SignKz.Models;

namespace SignKz.Services;

public interface IProgressStore
{
    List<UserProgress> LoadAll();
    void SaveAll(IEnumerable<UserProgress> users);
}
