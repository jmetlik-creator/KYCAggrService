using System.Net;
using System.Text;

namespace KycAggregation.Tests.Mock;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses;

    public StubHttpMessageHandler(Dictionary<string, string> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = request.RequestUri!.AbsolutePath;

        var responseBody = _responses.TryGetValue(key, out var payload)
            ? payload
            : _responses.FirstOrDefault(x => key.StartsWith(x.Key, StringComparison.Ordinal)).Value;

        if (string.IsNullOrEmpty(responseBody))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
    }
}
