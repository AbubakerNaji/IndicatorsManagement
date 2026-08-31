using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Jobs;

public class OverdueNotificationJob
{
    private readonly IndicatorsDbContext _db;
    private readonly INotificationService _notification;
    private readonly IEmailService _email;

    public OverdueNotificationJob(IndicatorsDbContext db, INotificationService notification, IEmailService email)
    {
        _db = db;
        _notification = notification;
        _email = email;
    }

    public async Task ExecuteAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueObligations = await _db.SubmissionObligations
            .Include(o => o.IndicatorAssignment).ThenInclude(a => a.Indicator)
            .Include(o => o.IndicatorAssignment).ThenInclude(a => a.Entity)
            .Include(o => o.ReportingPeriod)
            .Where(o => o.DueDate < today
                && (o.Status == ObligationStatus.Not_Started || o.Status == ObligationStatus.In_Progress))
            .ToListAsync();

        foreach (var obligation in overdueObligations)
        {
            obligation.Status = ObligationStatus.Overdue;

            var indicatorName = obligation.IndicatorAssignment.Indicator.NameAr;
            var entityName = obligation.IndicatorAssignment.Entity.NameAr;
            var periodName = obligation.ReportingPeriod.DisplayNameAr;
            var daysOverdue = today.DayNumber - obligation.DueDate.DayNumber;
            var title = "تنبيه: تسليم متأخر";
            var message = $"المؤشر: {indicatorName} | الجهة: {entityName} | الفترة: {periodName} | متأخر بـ {daysOverdue} يوم";
            var emailHtml = $"<div dir='rtl' style='font-family:sans-serif'><h3 style='color:#dc2626'>{title}</h3><p>{message}</p></div>";

            // Notify entity users
            var entityUsers = await _db.Users
                .Where(u => u.EntityId == obligation.IndicatorAssignment.EntityId && u.IsActive)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            foreach (var user in entityUsers)
            {
                await _notification.CreateNotificationAsync(
                    user.Id, NotificationType.Overdue, title, message,
                    "SubmissionObligation", obligation.Id);

                if (!string.IsNullOrEmpty(user.Email))
                    await _email.SendAsync(user.Email, title, emailHtml);
            }

            // Also notify Ministry Admins
            var ministryAdmins = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "Ministry_Admin")
                .Select(x => x.UserId)
                .ToListAsync();

            var adminTitle = "تنبيه: تسليم متأخر من جهة تابعة";
            foreach (var adminId in ministryAdmins)
            {
                await _notification.CreateNotificationAsync(
                    adminId, NotificationType.Overdue, adminTitle, message,
                    "SubmissionObligation", obligation.Id);

                var adminEmail = await _db.Users
                    .Where(u => u.Id == adminId && u.IsActive)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(adminEmail))
                    await _email.SendAsync(adminEmail, adminTitle, emailHtml);
            }
        }

        await _db.SaveChangesAsync();
    }
}
