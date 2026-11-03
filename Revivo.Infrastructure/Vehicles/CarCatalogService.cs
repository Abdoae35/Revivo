using Microsoft.EntityFrameworkCore;
using Revivo.Application.Vehicles;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Vehicles;

public sealed class CarCatalogService(RevivoDbContext db) : ICarCatalogService
{
    public async Task<IReadOnlyList<CarMakeDto>> GetMakesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.CarMakes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(make => make.Name.ToLower().Contains(term));
        }
        return await query.OrderBy(make => make.Name)
            .Select(make => new CarMakeDto(make.Id, make.Name)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId, string? search, CancellationToken cancellationToken)
    {
        var query = db.CarModels.AsNoTracking().Where(model => model.CarMakeId == makeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(model => model.Name.ToLower().Contains(term));
        }
        return await query.OrderBy(model => model.Name)
            .Select(model => new CarModelDto(model.Id, model.CarMakeId, model.Name, model.YearStart, model.YearEnd))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(int modelId, CancellationToken cancellationToken)
    {
        var bounds = await db.CarModels.AsNoTracking().Where(model => model.Id == modelId)
            .Select(model => new { model.YearStart, model.YearEnd }).SingleOrDefaultAsync(cancellationToken);
        if (bounds?.YearStart is not int start || start < 1886 || start > DateTime.UtcNow.Year + 1)
            return [];
        var end = Math.Min(bounds.YearEnd ?? DateTime.UtcNow.Year + 1, DateTime.UtcNow.Year + 1);
        return end < start ? [] : Enumerable.Range(start, end - start + 1).Reverse().ToArray();
    }

    public Task<CarImageModelDto?> GetModelForImageAsync(int modelId, CancellationToken cancellationToken) =>
        db.CarModels.AsNoTracking().Where(model => model.Id == modelId)
            .Select(model => new CarImageModelDto(model.CarMake.Name, model.Name, model.YearStart, model.YearEnd))
            .SingleOrDefaultAsync(cancellationToken);
}
