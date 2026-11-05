using System.Net.Http.Json;
using Logging.Contracts;

namespace KycAggregation.Data;

public sealed class HttpLogSinkClient : ILogSink
{
    private readonly HttpClient _httpClient;

    public HttpLogSinkClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        if (logEvent is null)
        {
            throw new ArgumentNullException(nameof(logEvent));
        }

        using var response = await _httpClient.PostAsJsonAsync("/api/logs", logEvent, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
