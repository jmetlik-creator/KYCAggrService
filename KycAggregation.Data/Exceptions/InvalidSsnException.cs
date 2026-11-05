namespace KycAggregation.Data;

public sealed class InvalidSsnException : Exception
{
    public InvalidSsnException(string message) : base(message)
    {
    }
}
