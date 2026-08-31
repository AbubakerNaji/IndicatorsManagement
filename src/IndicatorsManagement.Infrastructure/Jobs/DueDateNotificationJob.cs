using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Jobs;

public class DueDateNotificationJob
{
    private readonly IndicatorsDbContext _db;
    private readonly INotificationService _notification;
    private readonly IEmailService _email;
    private readonly ISystemConfigurationService _config;

    public DueDateNotificationJob(
        IndicatorsDbContext db,
        INotificationService notification,
        IEmailService email,
        ISystemConfigurationService config)
    {
        _db = db;
        _notification = notification;
        _email = email;
        _config = config;
    }

    public async Task ExecuteAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // B6 — thresholds come from system configuration, defaulting to (7, 3, 1) if any key is missing.
        var thresholds = await LoadThresholdsAsync();

        foreach (var daysAhead in thresholds)
        {
            var targetDate = today.AddDays(daysAhead);

            var obligations = await _db.SubmissionObligations
                .Include(o => o.IndicatorAssignment).ThenInclude(a => a.Indicator)
                .Include(o => o.IndicatorAssignment).ThenInclude(a => a.Entity)
                .Include(o => o.ReportingPeriod)
                .Where(o => o.DueDate == targetDate
                    && (o.Status == ObligationStatus.Not_Started || o.Status == ObligationStatus.In_Progress))
                .ToListAsync();

            foreach (var obligation in obligations)
            {
                var indicatorName = obligation.IndicatorAssignment.Indicator.NameAr;
                var entityName = obligation.IndicatorAssignment.Entity.NameAr;
                var periodName = obligation.ReportingPeriod.DisplayNameAr;
                var title = $"تذكير: موعد تسليم قريب ({daysAhead} أيام)";
                var message = $"المؤشر: {indicatorName} | الجهة: {entityName} | الفترة: {periodName} | الموعد: {obligation.DueDate}";

                // Find users of the entity
                var entityUsers = await _db.Users
                    .Where(u => u.EntityId == obligation.IndicatorAssignment.EntityId && u.IsActive)
                    .Select(u => new { u.Id, u.Email })
                    .ToListAsync();

                foreach (var user in entityUsers)
                {
                    // In-app notification
                    await _notification.CreateNotificationAsync(
                        user.Id, NotificationType.Due_Date, title, message,
                        "SubmissionObligation", obligation.Id);

                    // Email notification
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await _email.SendAsync(user.Email, title,
                            $"<div dir='rtl' style='font-family:sans-serif'><h3>{title}</h3><p>{message}</p></div>");
                    }
                }
            }
        }
    }

    private async Task<int[]> LoadThresholdsAsync()
    {
        var configuredKeys = new[]
        {
            "NotificationThreshold_Days_7",
            "NotificationThreshold_Days_3",
            "NotificationThreshold_Days_1"
        };
        var defaults = new[] { 7, 3, 1 };

        var results = new List<int>();
        for (int i = 0; i < configuredKeys.Length; i++)
        {
            var raw = await _config.GetConfigValueAsync(configuredKeys[i]);
            if (int.TryParse(raw, out var days) && days > 0)
                results.Add(days);
            else
                results.Add(defaults[i]);
        }
        // Deduplicate — someone might set them all to 7 by mistake.
        return results.Distinct().OrderByDescending(x => x).ToArray();
    }
}
