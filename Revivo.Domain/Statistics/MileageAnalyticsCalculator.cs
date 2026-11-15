namespace Revivo.Domain.Statistics;

public sealed record OdometerPoint(DateTimeOffset RecordedAt, int MileageKm);
public enum DistanceConfidence { Exact, Estimated, Unavailable }
public sealed record DistancePeriod(decimal? DistanceKm, DistanceConfidence Confidence, bool IsPartial);
public sealed record MonthlyDistance(int Month, DistancePeriod Distance);
public sealed record YearlyDistance(int Year, DistancePeriod Total, IReadOnlyList<MonthlyDistance> Months);

/// <summary>
/// Distances are interpolated linearly in elapsed UTC time between consecutive readings.
/// Values outside the first and last reading are never extrapolated.
/// </summary>
public static class MileageAnalyticsCalculator
{
    public static YearlyDistance CalculateYear(IEnumerable<OdometerPoint> readings, int year,
        DateTimeOffset now)
    {
        var points = Normalize(readings);
        var utcNow = now.ToUniversalTime();
        var latest = points.Count == 0 ? (DateTimeOffset?)null : points[^1].RecordedAt;
        var months = new List<MonthlyDistance>(12);
        for (var month = 1; month <= 12; month++)
        {
            var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1);
            var partial = year == utcNow.Year && latest is not null && latest >= start && latest < end;
            months.Add(new(month, CalculatePeriod(points, start, end, utcNow, partial)));
        }

        var yearStart = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = yearStart.AddYears(1);
        var total = CalculatePeriod(points, yearStart, yearEnd, utcNow, year == utcNow.Year);
        return new(year, total, months);
    }

    public static DistancePeriod CalculateMonth(IEnumerable<OdometerPoint> readings, int year, int month,
        DateTimeOffset now)
    {
        var points = Normalize(readings);
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        var latest = points.Count == 0 ? (DateTimeOffset?)null : points[^1].RecordedAt;
        var partial = year == now.ToUniversalTime().Year && month == now.ToUniversalTime().Month &&
            latest is not null && latest >= start && latest < end;
        return CalculatePeriod(points, start, end, now.ToUniversalTime(), partial);
    }

    public static decimal? AverageCompleteMonth(IEnumerable<OdometerPoint> readings, DateTimeOffset now)
    {
        var points = Normalize(readings);
        if (points.Count < 2) return null;
        var start = new DateTimeOffset(points[0].RecordedAt.Year, points[0].RecordedAt.Month, 1,
            0, 0, 0, TimeSpan.Zero);
        var last = points[^1].RecordedAt < now.ToUniversalTime() ? points[^1].RecordedAt : now.ToUniversalTime();
        var values = new List<decimal>();
        for (var month = start; month.AddMonths(1) <= last; month = month.AddMonths(1))
        {
            var result = CalculatePeriod(points, month, month.AddMonths(1), now.ToUniversalTime(), false);
            if (result.DistanceKm is decimal distance) values.Add(distance);
        }
        return values.Count == 0 ? null : decimal.Round(values.Average(), 2);
    }

    private static DistancePeriod CalculatePeriod(IReadOnlyList<OdometerPoint> points,
        DateTimeOffset start, DateTimeOffset end, DateTimeOffset now, bool allowPartial)
    {
        if (points.Count < 2 || start >= now) return Unavailable();
        var finish = allowPartial ? Min(end, now, points[^1].RecordedAt) : end;
        if (finish <= start || finish > now) return Unavailable();
        var startValue = ValueAt(points, start);
        var endValue = ValueAt(points, finish);
        if (startValue is null || endValue is null) return Unavailable();
        for (var i = 1; i < points.Count; i++)
            if (points[i].RecordedAt > start && points[i - 1].RecordedAt < finish &&
                points[i].MileageKm < points[i - 1].MileageKm) return Unavailable();
        var difference = endValue.Value.Mileage - startValue.Value.Mileage;
        if (difference < 0) return Unavailable();
        return new(decimal.Round(difference, 2),
            startValue.Value.Interpolated || endValue.Value.Interpolated
                ? DistanceConfidence.Estimated : DistanceConfidence.Exact,
            finish < end);
    }

    private static (decimal Mileage, bool Interpolated)? ValueAt(IReadOnlyList<OdometerPoint> points,
        DateTimeOffset at)
    {
        if (at < points[0].RecordedAt || at > points[^1].RecordedAt) return null;
        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].RecordedAt == at) return (points[i].MileageKm, false);
            if (i == 0 || points[i].RecordedAt < at) continue;
            var before = points[i - 1];
            var after = points[i];
            var elapsed = (decimal)(at - before.RecordedAt).Ticks;
            var span = (decimal)(after.RecordedAt - before.RecordedAt).Ticks;
            return (before.MileageKm + (after.MileageKm - before.MileageKm) * elapsed / span, true);
        }
        return null;
    }

    private static List<OdometerPoint> Normalize(IEnumerable<OdometerPoint> readings) => readings
        .Select(reading => new OdometerPoint(reading.RecordedAt.ToUniversalTime(), reading.MileageKm))
        .OrderBy(reading => reading.RecordedAt)
        .GroupBy(reading => reading.RecordedAt)
        .Select(group => new OdometerPoint(group.Key, group.Max(reading => reading.MileageKm)))
        .ToList();

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b, DateTimeOffset c) =>
        a < b ? a < c ? a : c : b < c ? b : c;

    private static DistancePeriod Unavailable() => new(null, DistanceConfidence.Unavailable, false);
}
