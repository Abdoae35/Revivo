using Revivo.Infrastructure.Catalog;

namespace Revivo.Api.Tests;

public class CarCatalogImportTests
{
    [Fact]
    public void ParsesQuotedNamesAndOptionalYearBounds()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "make_group,make,model,year_start,year_end\ncustom,\"Example, Motors\",Sedan,2000,\n");
            var rows = CarCatalogSeeder.ReadRows(path);
            var row = Assert.Single(rows);
            Assert.Equal("Example, Motors", row.Make);
            Assert.Equal("Sedan", row.Model);
            Assert.Equal(2000, row.YearStart);
            Assert.Null(row.YearEnd);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RejectsReversedYearBounds()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "make_group,make,model,year_start,year_end\ncustom,Example,Sedan,2020,2010\n");
            Assert.Throws<InvalidDataException>(() => CarCatalogSeeder.ReadRows(path));
        }
        finally { File.Delete(path); }
    }
}
