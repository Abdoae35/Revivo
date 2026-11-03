using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Revivo.Api.Vehicles;
using Revivo.Application.Vehicles;
using Revivo.Infrastructure.Images;

namespace Revivo.Api.Tests;

public class CarImageTests
{
    private const string PhotoJson = """
        {"results":[{"urls":{"regular":"https://images.unsplash.com/photo-test?ixid=123"},
        "user":{"name":"Test Photographer","links":{"html":"https://unsplash.com/@tester"}},
        "links":{"html":"https://unsplash.com/photos/test"}}]}
        """;

    [Fact]
    public async Task FoundPhotoMapsAttributionAndCachesSearch()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(PhotoJson) });
        var service = CreateService(handler);

        var first = await service.GetImageAsync(7, 2020, CancellationToken.None);
        var second = await service.GetImageAsync(7, 2020, CancellationToken.None);

        Assert.Equal(CarImageStatus.Found, first.Status);
        Assert.Same(first, second);
        Assert.Equal(1, handler.Calls);
        Assert.Contains("Toyota", handler.LastUri!.Query);
        Assert.Contains("Corolla", handler.LastUri.Query);
        Assert.Contains("2020", handler.LastUri!.Query);
        Assert.Contains("car", handler.LastUri.Query);
        Assert.Contains("orientation=landscape", handler.LastUri.ToString());
        Assert.Equal("Client-ID", handler.LastAuthScheme);
        Assert.Equal("https://images.unsplash.com/photo-test?ixid=123", first.ImageUrl);
        Assert.Equal("Test Photographer", first.PhotographerName);
        Assert.Contains("utm_source=revivo&utm_medium=referral", first.PhotographerUrl);
        Assert.Contains("utm_source=revivo&utm_medium=referral", first.PhotoUrl);
        Assert.Contains("utm_source=revivo&utm_medium=referral", first.UnsplashUrl);
        Assert.False(first.IsExactVehicle);
    }

    [Fact]
    public async Task EmptyResultsUseCachedFallback()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"results\":[]}") });
        var service = CreateService(handler);
        Assert.Equal(CarImageStatus.NoImage, (await service.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        Assert.Equal(CarImageStatus.NoImage, (await service.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, CarImageStatus.RateLimited)]
    [InlineData(HttpStatusCode.InternalServerError, CarImageStatus.Unavailable)]
    public async Task ApiFailuresReturnFallback(HttpStatusCode code, CarImageStatus expected)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(code));
        var service = CreateService(handler);
        Assert.Equal(expected, (await service.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        await service.GetImageAsync(7, 2020, CancellationToken.None);
        Assert.Equal(code == HttpStatusCode.TooManyRequests ? 1 : 2, handler.Calls);
    }

    [Fact]
    public async Task NetworkFailureAndMalformedJsonReturnUnavailable()
    {
        var failed = CreateService(new StubHandler(_ => throw new HttpRequestException()));
        Assert.Equal(CarImageStatus.Unavailable, (await failed.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        var malformed = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("not json") }));
        Assert.Equal(CarImageStatus.Unavailable, (await malformed.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        var timedOut = CreateService(new StubHandler(_ => throw new TaskCanceledException()));
        Assert.Equal(CarImageStatus.Unavailable, (await timedOut.GetImageAsync(7, 2020, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task InvalidInputAndMissingKeyAvoidUnsplash()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(handler);
        Assert.Equal(CarImageStatus.InvalidYear, (await service.GetImageAsync(7, 1800, CancellationToken.None)).Status);
        Assert.Equal(CarImageStatus.ModelNotFound, (await service.GetImageAsync(99, 2020, CancellationToken.None)).Status);
        Assert.Equal(CarImageStatus.InvalidYear, (await service.GetImageAsync(7, 2000, CancellationToken.None)).Status);
        Assert.Equal(0, handler.Calls);
        service = CreateService(handler, configured: false);
        Assert.Equal(CarImageStatus.NotConfigured, (await service.GetImageAsync(7, 2020, CancellationToken.None)).Status);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(0, 2020, 400)]
    [InlineData(7, 1800, 400)]
    [InlineData(99, 2020, 404)]
    public async Task EndpointMapsInvalidInputs(int modelId, int year, int expectedStatus)
    {
        var method = typeof(CarCatalogEndpoints).GetMethod("GetCarImageAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var response = await (Task<IResult>)method.Invoke(null, [modelId, year, service, CancellationToken.None])!;
        Assert.Equal(expectedStatus, ((IStatusCodeHttpResult)response).StatusCode);
    }

    [Theory]
    [InlineData(CarImageStatus.Found, "found")]
    [InlineData(CarImageStatus.NoImage, "noImage")]
    [InlineData(CarImageStatus.RateLimited, "rateLimited")]
    public async Task EndpointReturnsClearImageOrFallback(CarImageStatus status, string expected)
    {
        var method = typeof(CarCatalogEndpoints).GetMethod("GetCarImageAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var response = await (Task<IResult>)method.Invoke(null,
            [7, 2020, new StubImages(new CarImageResult(status)), CancellationToken.None])!;
        Assert.Equal(200, ((IStatusCodeHttpResult)response).StatusCode);
        var body = Assert.IsType<CarImageResponse>(((IValueHttpResult)response).Value);
        Assert.Equal(expected, body.Status);
        Assert.False(body.IsExactVehicle);
        Assert.NotEmpty(body.Message);
    }

    private static UnsplashCarImageService CreateService(StubHandler handler, bool configured = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Unsplash:AccessKey"] = configured ? new string('x', 32) : null }).Build();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.unsplash.com/") };
        return new UnsplashCarImageService(new StubCatalog(), new StubFactory(client), config,
            new MemoryCache(new MemoryCacheOptions()), NullLogger<UnsplashCarImageService>.Instance);
    }

    private sealed class StubCatalog : ICarCatalogService
    {
        public Task<CarImageModelDto?> GetModelForImageAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<CarImageModelDto?>(id == 7 ? new("Toyota", "Corolla", 2010, 2025) : null);
        public Task<IReadOnlyList<CarMakeDto>> GetMakesAsync(string? search, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CarMakeDto>>([]);
        public Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId, string? search, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CarModelDto>>([]);
        public Task<IReadOnlyList<int>> GetYearsAsync(int modelId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<int>>([]);
    }

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubImages(CarImageResult result) : ICarImageService
    {
        public Task<CarImageResult> GetImageAsync(int modelId, int year, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }
        public string? LastAuthScheme { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUri = request.RequestUri;
            LastAuthScheme = request.Headers.Authorization?.Scheme;
            return Task.FromResult(respond(request));
        }
    }
}
