using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Revivo.Api.Notifications;
using Revivo.Application.Maintenance;
using Revivo.Application.Notifications;
using Revivo.Application.Warranties;
using Revivo.Domain.Entities;
using Revivo.Infrastructure.Persistence;

namespace Revivo.Api.Tests;

public class NotificationTests
{
    [Fact]
    public void DefaultsValidationAndUniqueMapping()
    {
        var settings = new NotificationSettings();
        Assert.False(settings.WeeklyReminderEnabled);
        Assert.Equal(1, settings.ReminderDayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), settings.ReminderTime);
        Assert.Equal("UTC", settings.TimeZoneId);
        Assert.True(settings.MaintenanceAlertsEnabled);
        Assert.True(settings.WarrantyAlertsEnabled);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.ReminderDayOfWeek = 7);
        Assert.Throws<ArgumentException>(() => settings.TimeZoneId = "Invalid/Zone");

        var options = new DbContextOptionsBuilder<RevivoDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Trusted_Connection=True").Options;
        using var db = new RevivoDbContext(options);
        var entity = db.Model.FindEntityType(typeof(NotificationSettings))!;
        Assert.Equal("NotificationSettings", entity.GetTableName());
        Assert.Equal(typeof(Vehicle), Assert.Single(entity.GetForeignKeys()).PrincipalEntityType.ClrType);
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(NotificationSettings.UserId), nameof(NotificationSettings.VehicleId)]));
    }

    [Fact]
    public async Task EndpointsValidateAndUseJwtOwner()
    {
        var owner = Guid.NewGuid();
        var service = new RecordingNotifications(owner);
        var get = typeof(NotificationEndpoints).GetMethod("GetSettingsAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var put = typeof(NotificationEndpoints).GetMethod("UpdateSettingsAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var alerts = typeof(NotificationEndpoints).GetMethod("GetAlertsAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var valid = new NotificationSettingsInput(true, 1, new TimeOnly(9, 0), "UTC", true, true);
        static async Task<int?> Status(MethodInfo method, object?[] args) =>
            ((IStatusCodeHttpResult)await (Task<IResult>)method.Invoke(null, args)!).StatusCode;

        Assert.Equal(401, await Status(get, [7, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(401, await Status(put, [7, valid, new ClaimsPrincipal(), service, CancellationToken.None]));
        Assert.Equal(400, await Status(put, [7, valid with { ReminderDayOfWeek = 8,
            ReminderTime = null, TimeZoneId = "Invalid/Zone" }, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(0, service.Writes);
        Assert.Equal(200, await Status(put, [7, valid, Principal(owner), service, CancellationToken.None]));
        Assert.Equal(owner, service.LastUser);
        Assert.Equal(404, await Status(get, [7, Principal(Guid.NewGuid()), service, CancellationToken.None]));
        Assert.Equal(404, await Status(alerts, [7, Principal(Guid.NewGuid()), service, CancellationToken.None]));
        Assert.Equal(200, await Status(alerts, [7, Principal(owner), service, CancellationToken.None]));
    }

    [Fact]
    public void AlertsKeepOnlyAssessedActionableStatusesAndRespectPreferences()
    {
        static MaintenanceStatusDto Maintenance(string status) => new(1, "Fluids", "Oil", 10000, null,
            1000, 30, null, null, null, null, null, null, status);
        static WarrantyDto Warranty(string status) => new(1, 7, 1, "Battery", 12,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 30, 10, status,
            null, null, null, DateTimeOffset.UtcNow);
        var result = NotificationAlertSelector.Select(7, true, true,
            [Maintenance("OnTrack"), Maintenance("DueSoon"), Maintenance("Overdue")],
            [Warranty("Active"), Warranty("ExpiringSoon"), Warranty("Expired")]);
        Assert.Equal(["DueSoon", "Overdue"], result.Maintenance.Select(item => item.Status));
        Assert.Equal(["ExpiringSoon", "Expired"], result.Warranties.Select(item => item.Status));
        var disabled = NotificationAlertSelector.Select(7, false, false, result.Maintenance, result.Warranties);
        Assert.Empty(disabled.Maintenance);
        Assert.Empty(disabled.Warranties);
    }

    private static ClaimsPrincipal Principal(Guid userId) =>
        new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], "test"));

    private sealed class RecordingNotifications(Guid owner) : INotificationService
    {
        public Guid LastUser { get; private set; }
        public int Writes { get; private set; }
        public Task<NotificationSettingsDto?> GetSettingsAsync(Guid userId, int vehicleId, CancellationToken cancellationToken) =>
            Task.FromResult<NotificationSettingsDto?>(userId == owner ? Dto(userId, vehicleId) : null);
        public Task<NotificationSettingsDto?> UpdateSettingsAsync(Guid userId, int vehicleId,
            NotificationSettingsInput input, CancellationToken cancellationToken)
        {
            LastUser = userId;
            Writes++;
            return GetSettingsAsync(userId, vehicleId, cancellationToken);
        }
        public Task<VehicleAlertsDto?> GetAlertsAsync(Guid userId, int vehicleId, CancellationToken cancellationToken) =>
            Task.FromResult<VehicleAlertsDto?>(userId == owner ? new VehicleAlertsDto(vehicleId, [], []) : null);
        private static NotificationSettingsDto Dto(Guid userId, int vehicleId) =>
            new(1, vehicleId, userId, true, 1, new TimeOnly(9, 0), "UTC", true, true);
    }
}
