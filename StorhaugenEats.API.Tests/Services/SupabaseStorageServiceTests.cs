using FluentAssertions;
using Microsoft.Extensions.Options;
using Supabase;
using StorhaugenEats.API.Services;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace StorhaugenEats.API.Tests.Services;

public class SupabaseStorageServiceTests
{
    private static SupabaseStorageService CreateService(HttpMessageHandler handler, StorageOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        var supabaseClient = new Client("http://localhost", "test-key", new SupabaseOptions { AutoConnectRealtime = false });
        var storageOptions = Options.Create(options ?? new StorageOptions
        {
            AllowedImageHosts = new List<string> { "example.com" },
            MaxDownloadBytes = 1024
        });

        return new SupabaseStorageService(supabaseClient, httpClient, storageOptions);
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldRejectBlockedHost()
    {
        var handler = new TestHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var service = CreateService(handler, new StorageOptions
        {
            AllowedImageHosts = new List<string> { "allowed.com" }
        });

        var act = () => service.UploadImageFromUrlAsync("https://blocked.com/image.png", "image.png");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Image host is not allowed.");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldRejectOversizedContent()
    {
        var handler = new TestHttpMessageHandler(_ =>
        {
            var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            content.Headers.ContentLength = 2048;
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var service = CreateService(handler, new StorageOptions
        {
            AllowedImageHosts = new List<string> { "example.com" },
            MaxDownloadBytes = 1024
        });

        var act = () => service.UploadImageFromUrlAsync("https://example.com/image.png", "image.png");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Image exceeds maximum allowed size of 1024 bytes.");
    }

    [Fact]
    public async Task UploadImageFromUrlAsync_ShouldRejectNonImageContentType()
    {
        var handler = new TestHttpMessageHandler(_ =>
        {
            var content = new StringContent("not an image");
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        var service = CreateService(handler);

        var act = () => service.UploadImageFromUrlAsync("https://example.com/image.png", "image.png");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Remote content is not an image.");
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
