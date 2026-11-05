using System.Net;

namespace KycAggregation.Data;

public sealed class CustomerDataApiException : Exception
{
    public CustomerDataApiException(string message, HttpStatusCode statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
