using Revivo.Application.Vehicles;

namespace Revivo.Api.Vehicles;

public static class CarCatalogEndpoints
{
    public static IEndpointRouteBuilder MapCarCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/car-catalog");
        group.MapGet("/makes", async (string? search, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            Results.Ok(await catalog.GetMakesAsync(search, cancellationToken)));
        group.MapGet("/models", async (int makeId, string? search, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            makeId <= 0 ? Results.ValidationProblem(new Dictionary<string, string[]> { ["makeId"] = ["A positive makeId is required."] })
                : Results.Ok(await catalog.GetModelsAsync(makeId, search, cancellationToken)));
        group.MapGet("/models/{modelId:int}/years", async (int modelId, ICarCatalogService catalog, CancellationToken cancellationToken) =>
            modelId <= 0 ? Results.ValidationProblem(new Dictionary<string, string[]> { ["modelId"] = ["A positive modelId is required."] })
                : Results.Ok(await catalog.GetYearsAsync(modelId, cancellationToken)));
        return endpoints;
    }
}
