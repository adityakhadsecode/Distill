namespace Distill.Tests.Mocks;

/// <summary>
/// Mock HttpMessageHandler for testing HttpClient-based services without real network requests.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (Handler != null)
        {
            return Task.FromResult(Handler(request));
        }

        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
