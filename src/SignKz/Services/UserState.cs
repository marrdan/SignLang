namespace SignKz.Services;

// Who is logged in right now. One instance per browser tab (scoped).
public class UserState
{
    public string Name { get; private set; } = "";
    public bool IsLoggedIn => Name.Length > 0;

    public event Action? Changed;

    public void SetName(string name)
    {
        Name = name;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Name = "";
        Changed?.Invoke();
    }
}

