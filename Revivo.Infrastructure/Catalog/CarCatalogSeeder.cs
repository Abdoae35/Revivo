using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Infrastructure.Catalog;

public sealed class CarCatalogSeeder(RevivoDbContext db)
{
    public static IReadOnlyList<CatalogRow> ReadRows(string path)
    {
        using var parser = new TextFieldParser(path);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var header = parser.ReadFields();
        if (header is null || !header.SequenceEqual(["make_group", "make", "model", "year_start", "year_end"], StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Expected make_group,make,model,year_start,year_end CSV columns.");
        var rows = new List<CatalogRow>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length != 5) throw new InvalidDataException("Malformed car catalog row.");
            var make = fields[1].Trim();
            var model = fields[2].Trim();
            if (make.Length is 0 or > 100 || model.Length is 0 or > 100)
                throw new InvalidDataException("Make and model names must contain 1-100 characters.");
            var start = ParseYear(fields[3]);
            var end = ParseYear(fields[4]);
            if (start is not null && end is not null && end < start)
                throw new InvalidDataException("Model end year precedes start year.");
            rows.Add(new CatalogRow(make, model, start, end));
        }
        return rows;
    }

    public async Task<(int Makes, int Models)> SeedAsync(IReadOnlyList<CatalogRow> rows, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var makes = (await db.CarMakes.ToListAsync(cancellationToken))
            .ToDictionary(make => make.Name, StringComparer.OrdinalIgnoreCase);
        var newMakes = 0;
        foreach (var name in rows.Select(row => row.Make).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (makes.ContainsKey(name)) continue;
            var make = new CarMake { Name = name };
            db.CarMakes.Add(make);
            makes.Add(name, make);
            newMakes++;
        }
        await db.SaveChangesAsync(cancellationToken);

        var existing = await db.CarModels.ToListAsync(cancellationToken);
        var models = existing.ToDictionary(model => (model.CarMakeId, model.Name), new ModelKeyComparer());
        var newModels = 0;
        foreach (var row in rows)
        {
            var makeId = makes[row.Make].Id;
            var key = (makeId, row.Model);
            if (models.TryGetValue(key, out var model))
            {
                model.YearStart ??= row.YearStart;
                model.YearEnd ??= row.YearEnd;
                continue;
            }
            model = new CarModel { CarMakeId = makeId, Name = row.Model, YearStart = row.YearStart, YearEnd = row.YearEnd };
            db.CarModels.Add(model);
            models.Add(key, model);
            newModels++;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (newMakes, newModels);
    }

    private static int? ParseYear(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!int.TryParse(value, out var year) || year < 1886 || year > 3000)
            throw new InvalidDataException("Invalid model year in catalog row.");
        return year;
    }

    private sealed class ModelKeyComparer : IEqualityComparer<(int MakeId, string Name)>
    {
        public bool Equals((int MakeId, string Name) x, (int MakeId, string Name) y) =>
            x.MakeId == y.MakeId && StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
        public int GetHashCode((int MakeId, string Name) value) => HashCode.Combine(value.MakeId, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Name));
    }
}

public sealed record CatalogRow(string Make, string Model, int? YearStart, int? YearEnd);
