using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class SimulationConfigurationValidationResult
{
    private readonly IReadOnlyList<string> errors;

    public bool IsValid => errors.Count == 0;
    public IReadOnlyList<string> Errors => errors;

    internal SimulationConfigurationValidationResult(IEnumerable<string> errors)
    {
        List<string> snapshot = errors == null
            ? new List<string>()
            : new List<string>(errors);
        this.errors = new ReadOnlyCollection<string>(snapshot);
    }
}
