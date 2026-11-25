namespace WorldAtlas.Observations.Parsing;

public sealed class ObservationValidationException : FormatException
{
    public ObservationValidationException(string code, string message) : base(message) => Code = code;

    public ObservationValidationException(string code, string message, Exception innerException)
        : base(message, innerException) => Code = code;

    public string Code { get; }
}
