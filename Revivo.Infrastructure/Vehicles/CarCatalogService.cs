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

    public async Task<IReadOnlyList<CarModelDto>> GetModelsAsync(int makeId, CancellationToken cancellationToken) =>
        await db.CarModels.AsNoTracking().Where(model => model.CarMakeId == makeId)
            .OrderBy(model => model.Name).Select(model => new CarModelDto(model.Id, model.CarMakeId, model.Name))
            .ToListAsync(cancellationToken);
}
