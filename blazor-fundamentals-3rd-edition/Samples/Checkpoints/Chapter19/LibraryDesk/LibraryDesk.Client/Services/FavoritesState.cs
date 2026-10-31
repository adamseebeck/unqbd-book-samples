namespace LibraryDesk.Client.Services;

public sealed class FavoritesState
{
    private readonly HashSet<int> ids = [];
    public IReadOnlyCollection<int> Ids => ids.ToArray();
    public event Action? Changed;

    public void Add(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        if (ids.Add(id)) Changed?.Invoke();
    }

    public void Remove(int id)
    {
        if (ids.Remove(id)) Changed?.Invoke();
    }

    public void Clear()
    {
        if (ids.Count == 0) return;
        ids.Clear();
        Changed?.Invoke();
    }
}
