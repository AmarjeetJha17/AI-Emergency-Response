using System.Collections.Concurrent;

namespace EmergencyResponse.Application.Reliability;

public interface IDependencyHealthRegistry
{
    void Set(string dependency, DependencyHealthState state, string? category = null);
    (DependencyHealthState State, string? Category) Get(string dependency);
}

public sealed class DependencyHealthRegistry : IDependencyHealthRegistry
{
    private readonly ConcurrentDictionary<string, (DependencyHealthState State, string? Category)> _states =
        new(StringComparer.OrdinalIgnoreCase);

    public void Set(string dependency, DependencyHealthState state, string? category = null)
        => _states[dependency] = (state, category);

    public (DependencyHealthState State, string? Category) Get(string dependency)
        => _states.TryGetValue(dependency, out var result)
            ? result
            : (DependencyHealthState.Healthy, null);
}
