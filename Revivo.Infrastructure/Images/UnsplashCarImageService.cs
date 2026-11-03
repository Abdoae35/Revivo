using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Revivo.Application.Vehicles;

namespace Revivo.Infrastructure.Images;

public sealed class UnsplashCarImageService(
    ICarCatalogService catalog,
    IHttpClientFactory clientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<UnsplashCarImageService> logger) : ICarImageService
{
    public async Task<CarImageResult> GetImageAsync(int modelId, int year, CancellationToken cancellationToken)
    {
        if (modelId <= 0) return new(CarImageStatus.ModelNotFound);
        if (year < 1886 || year > DateTime.UtcNow.Year + 1) return new(CarImageStatus.InvalidYear);

        var model = await catalog.GetModelForImageAsync(modelId, cancellationToken);
        if (model is null) return new(CarImageStatus.ModelNotFound);
        if (year < model.YearStart || year > model.YearEnd) return new(CarImageStatus.InvalidYear);

        var cacheKey = $"car-image:{modelId}:{year}:{model.Make}:{model.Model}";
        if (cache.TryGetValue(cacheKey, out CarImageResult? cached) && cached is not null) return cached;

        var accessKey = configuration["Unsplash:AccessKey"];
        if (string.IsNullOrWhiteSpace(accessKey)) return new(CarImageStatus.NotConfigured);

        var query = Uri.EscapeDataString($"{model.Make} {model.Model} {year} car");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"search/photos?query={query}&orientation=landscape&order_by=relevant&content_filter=high&per_page=5");
        request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", accessKey);
        request.Headers.Add("Accept-Version", "v1");

        try
        {
            using var response = await clientFactory.CreateClient("Unsplash").SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var limited = new CarImageResult(CarImageStatus.RateLimited);
                cache.Set(cacheKey, limited, TimeSpan.FromMinutes(1));
                return limited;
            }
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Unsplash search returned HTTP {StatusCode}.", (int)response.StatusCode);
                return new(CarImageStatus.Unavailable);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var result = MapSearchResponse(document.RootElement);
            if (result.Status is CarImageStatus.Found or CarImageStatus.NoImage)
                cache.Set(cacheKey, result, result.Status == CarImageStatus.Found
                    ? TimeSpan.FromHours(6) : TimeSpan.FromMinutes(15));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Unsplash search timed out.");
            return new(CarImageStatus.Unavailable);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Unsplash search could not be completed.");
            return new(CarImageStatus.Unavailable);
        }
        catch (JsonException)
        {
            logger.LogWarning("Unsplash returned an invalid search response.");
            return new(CarImageStatus.Unavailable);
        }
    }

    internal static CarImageResult MapSearchResponse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return new(CarImageStatus.Unavailable);

        foreach (var photo in results.EnumerateArray())
        {
            if (photo.ValueKind != JsonValueKind.Object ||
                !photo.TryGetProperty("urls", out var urls) || !photo.TryGetProperty("user", out var user) ||
                !photo.TryGetProperty("links", out var links) || user.ValueKind != JsonValueKind.Object) continue;
            var imageUrl = GetString(urls, "regular");
            var photographer = GetString(user, "name");
            var photographerUrl = user.TryGetProperty("links", out var userLinks)
                ? AttributionUrl(GetString(userLinks, "html")) : null;
            var photoUrl = AttributionUrl(GetString(links, "html"));
            if (!IsImageUrl(imageUrl) || string.IsNullOrWhiteSpace(photographer) ||
                photographerUrl is null || photoUrl is null) continue;

            return new(CarImageStatus.Found, imageUrl, photographer, photographerUrl, photoUrl,
                "https://unsplash.com/?utm_source=revivo&utm_medium=referral");
        }
        return new(CarImageStatus.NoImage);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool IsImageUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals("images.unsplash.com", StringComparison.OrdinalIgnoreCase);

    private static string? AttributionUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !uri.Host.Equals("unsplash.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Equals("www.unsplash.com", StringComparison.OrdinalIgnoreCase)) return null;
        var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
        builder.Query = string.IsNullOrEmpty(uri.Query)
            ? "utm_source=revivo&utm_medium=referral"
            : $"{uri.Query.TrimStart('?')}&utm_source=revivo&utm_medium=referral";
        return builder.Uri.ToString();
    }
}
