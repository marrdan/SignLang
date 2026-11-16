using SignKz.Models;

namespace SignKz.Services;

public interface ISignRepository
{
    List<Sign> GetAll();
    Sign? GetById(string id);
}

