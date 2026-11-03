namespace Revivo.Application.Vehicles;

public enum CarImageStatus { Found, NoImage, Unavailable, RateLimited, NotConfigured, ModelNotFound, InvalidYear }

public sealed record CarImageResult(
    CarImageStatus Status,
    string? ImageUrl = null,
    string? PhotographerName = null,
    string? PhotographerUrl = null,
    string? PhotoUrl = null,
    string? UnsplashUrl = null,
    bool IsExactVehicle = false);

public sealed record CarImageResponse(
    string Status,
    string? ImageUrl,
    string? PhotographerName,
    string? PhotographerUrl,
    string? PhotoUrl,
    string? UnsplashUrl,
    bool IsExactVehicle,
    string Message);

public interface ICarImageService
{
    Task<CarImageResult> GetImageAsync(int modelId, int year, CancellationToken cancellationToken);
}
