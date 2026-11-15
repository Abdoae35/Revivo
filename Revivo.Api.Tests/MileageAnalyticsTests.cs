using Revivo.Domain.Statistics;

namespace Revivo.Api.Tests;

public class MileageAnalyticsTests
{
    private static DateTimeOffset At(int year, int month, int day, int hour = 0) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MultipleReadingsWithinMonthProduceExactDistance()
    {
        var readings = new[] { new OdometerPoint(At(2026, 1, 1), 1000),
            new OdometerPoint(At(2026, 1, 15), 1100), new OdometerPoint(At(2026, 2, 1), 1200) };
        var month = MileageAnalyticsCalculator.CalculateMonth(readings, 2026, 1, At(2026, 3, 1));
        Assert.Equal(200m, month.DistanceKm);
        Assert.Equal(DistanceConfidence.Exact, month.Confidence);
        Assert.False(month.IsPartial);
    }

    [Fact]
    public void SparseReadingsInterpolateAcrossMonthsWithoutAssigningAllDistanceToMarch()
    {
        var readings = new[] { new OdometerPoint(At(2026, 1, 10), 50000),
            new OdometerPoint(At(2026, 3, 10), 53000) };
        var result = MileageAnalyticsCalculator.CalculateYear(readings, 2026, At(2026, 3, 15));
        Assert.Null(result.Months[0].Distance.DistanceKm);
        Assert.Equal(DistanceConfidence.Unavailable, result.Months[0].Distance.Confidence);
        Assert.Equal(1423.73m, result.Months[1].Distance.DistanceKm);
        Assert.Equal(DistanceConfidence.Estimated, result.Months[1].Distance.Confidence);
        Assert.True(result.Months[2].Distance.IsPartial);
        Assert.Equal(DistanceConfidence.Estimated, result.Months[2].Distance.Confidence);
        Assert.Null(result.Total.DistanceKm);
    }

    [Fact]
    public void CalendarYearAndMonthlyTotalsAgreeWithExactBoundaries()
    {
        var readings = Enumerable.Range(0, 13).Select(month =>
            new OdometerPoint(At(2026, 1, 1).AddMonths(month), month * 1000)).ToArray();
        var year = MileageAnalyticsCalculator.CalculateYear(readings, 2026, At(2027, 2, 1));
        Assert.Equal(12, year.Months.Count);
        Assert.All(year.Months, month =>
        {
            Assert.Equal(1000m, month.Distance.DistanceKm);
            Assert.Equal(DistanceConfidence.Exact, month.Distance.Confidence);
        });
        Assert.Equal(12000m, year.Total.DistanceKm);
        Assert.Equal(DistanceConfidence.Exact, year.Total.Confidence);
        Assert.Equal(1000m, MileageAnalyticsCalculator.AverageCompleteMonth(readings, At(2027, 2, 1)));
    }

    [Fact]
    public void ReadingsAcrossYearsCalculateDecemberAndJanuarySeparately()
    {
        var readings = new[] { new OdometerPoint(At(2025, 12, 1), 1000),
            new OdometerPoint(At(2026, 1, 1), 2000), new OdometerPoint(At(2026, 2, 1), 3000) };
        var previous = MileageAnalyticsCalculator.CalculateMonth(readings, 2025, 12, At(2026, 2, 2));
        var current = MileageAnalyticsCalculator.CalculateYear(readings, 2026, At(2026, 2, 2));
        Assert.Equal(1000m, previous.DistanceKm);
        Assert.Equal(1000m, current.Months[0].Distance.DistanceKm);
        Assert.Equal(1000m, current.Total.DistanceKm);
        Assert.True(current.Total.IsPartial);
        Assert.Null(current.Months[1].Distance.DistanceKm);
    }

    [Fact]
    public void MissingBaselineAndNoExtrapolationReturnUnavailable()
    {
        var readings = new[] { new OdometerPoint(At(2026, 1, 10), 1000),
            new OdometerPoint(At(2026, 2, 10), 2000) };
        Assert.Null(MileageAnalyticsCalculator.CalculateMonth(readings, 2026, 1, At(2026, 4, 1)).DistanceKm);
        Assert.Null(MileageAnalyticsCalculator.CalculateMonth(readings, 2026, 3, At(2026, 4, 1)).DistanceKm);
        Assert.Null(MileageAnalyticsCalculator.AverageCompleteMonth(readings, At(2026, 4, 1)));
        Assert.Null(MileageAnalyticsCalculator.CalculateMonth([], 2026, 1, At(2026, 4, 1)).DistanceKm);
    }

    [Fact]
    public void SameDayAndDuplicateTimeReadingsRemainOrdered()
    {
        var readings = new[] { new OdometerPoint(At(2026, 1, 1), 1000),
            new OdometerPoint(At(2026, 1, 1), 1005),
            new OdometerPoint(At(2026, 1, 1, 12), 1050),
            new OdometerPoint(At(2026, 2, 1), 1100) };
        var month = MileageAnalyticsCalculator.CalculateMonth(readings, 2026, 1, At(2026, 3, 1));
        Assert.Equal(95m, month.DistanceKm);
        Assert.Equal(DistanceConfidence.Exact, month.Confidence);
    }

    [Fact]
    public void ZeroDistanceIsAvailableAndDecreasingHistoryIsNotReportedAsTravel()
    {
        var zero = new[] { new OdometerPoint(At(2026, 1, 1), 1000),
            new OdometerPoint(At(2026, 2, 1), 1000) };
        Assert.Equal(0m, MileageAnalyticsCalculator.CalculateMonth(zero, 2026, 1, At(2026, 3, 1)).DistanceKm);
        var decreasing = new[] { new OdometerPoint(At(2026, 1, 1), 1000),
            new OdometerPoint(At(2026, 1, 15), 900), new OdometerPoint(At(2026, 2, 1), 1100) };
        Assert.Equal(DistanceConfidence.Unavailable,
            MileageAnalyticsCalculator.CalculateMonth(decreasing, 2026, 1, At(2026, 3, 1)).Confidence);
    }

    [Fact]
    public void CurrentMonthStopsAtLastReadingAndMarksPartial()
    {
        var readings = new[] { new OdometerPoint(At(2026, 4, 1), 1000),
            new OdometerPoint(At(2026, 4, 15), 1600) };
        var month = MileageAnalyticsCalculator.CalculateMonth(readings, 2026, 4, At(2026, 4, 20));
        Assert.Equal(600m, month.DistanceKm);
        Assert.True(month.IsPartial);
        Assert.Equal(DistanceConfidence.Exact, month.Confidence);
    }
}
