using Revivo.Application.Vehicles;

namespace Revivo.Api.Vehicles;

public static class CarCatalogEndpoints
{
    public static IEndpointRouteBuilder MapCarCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/car-catalog");
        group.MapGet("/makes", async (string? search, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            Results.Ok(await catalog.GetMakesAsync(search, cancellationToken)));
        group.MapGet("/models", async (int? makeId, string? search, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            makeId is null or <= 0 ? Results.ValidationProblem(new Dictionary<string, string[]> { ["makeId"] = ["A positive makeId is required."] })
                : Results.Ok(await catalog.GetModelsAsync(makeId.Value, search, cancellationToken)));
        group.MapGet("/models/{modelId:int}/years", async (int modelId, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            modelId <= 0 ? Results.ValidationProblem(new Dictionary<string, string[]> { ["modelId"] = ["A positive modelId is required."] })
                : Results.Ok(await catalog.GetYearsAsync(modelId, cancellationToken)));
        group.MapGet("/car-image", GetCarImageAsync);
        return endpoints;
    }

    private static async Task<IResult> GetCarImageAsync(int modelId, int year, ICarImageService images,
        CancellationToken cancellationToken)
    {
        if (modelId <= 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["modelId"] = ["A positive modelId is required."] });
        if (year < 1886 || year > DateTime.UtcNow.Year + 1)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["year"] = ["Vehicle year is outside the valid range."] });

        var result = await images.GetImageAsync(modelId, year, cancellationToken);
        if (result.Status == CarImageStatus.ModelNotFound) return Results.NotFound();
        if (result.Status == CarImageStatus.InvalidYear)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["year"] = ["Year is outside this model's known range."] });

        var (status, message) = result.Status switch
        {
            CarImageStatus.Found => ("found", "Illustrative photo; the exact vehicle is not verified."),
            CarImageStatus.NoImage => ("noImage", "No suitable photo was found; display a local placeholder."),
            CarImageStatus.RateLimited => ("rateLimited", "Image search is temporarily rate limited; display a local placeholder."),
            CarImageStatus.NotConfigured => ("notConfigured", "Car images are not configured; display a local placeholder."),
            _ => ("unavailable", "Image search is temporarily unavailable; display a local placeholder.")
        };
        return Results.Ok(new CarImageResponse(status, result.ImageUrl, result.PhotographerName,
            result.PhotographerUrl, result.PhotoUrl, result.UnsplashUrl, result.IsExactVehicle, message));
    }
}
