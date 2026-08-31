using FluentAssertions;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Tests;

/// <summary>
/// Property 25: Due Date Notification Content (Req 10.4, 11.3)
/// Property 26: Overdue Notification Trigger (Req 11.1)
/// Property 27: Workflow Change Notifications (Req 12.1-12.5)
/// </summary>
public class NotificationServiceTests
{
    [Fact]
    public async Task CreateNotification_ShouldPersist()
    {
        var db = TestDbContextFactory.Create(nameof(CreateNotification_ShouldPersist));
        await TestDbContextFactory.SeedBasicData(db);
        var service = new NotificationService(db);

        await service.CreateNotificationAsync(1, NotificationType.Due_Date, "تذكير", "محتوى التذكير", "SubmissionObligation", 1);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
        notifications[0].TitleAr.Should().Be("تذكير");
        notifications[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsRead_ShouldUpdateNotification()
    {
        var db = TestDbContextFactory.Create(nameof(MarkAsRead_ShouldUpdateNotification));
        await TestDbContextFactory.SeedBasicData(db);
        var service = new NotificationService(db);

        await service.CreateNotificationAsync(1, NotificationType.Workflow_Change, "إشعار", "تغيير حالة");
        var notif = await db.Notifications.FirstAsync();

        var result = await service.MarkAsReadAsync(notif.Id, 1);
        result.Success.Should().BeTrue();

        var updated = await db.Notifications.FirstAsync();
        updated.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldUpdateAllNotifications()
    {
        var db = TestDbContextFactory.Create(nameof(MarkAllAsRead_ShouldUpdateAllNotifications));
        await TestDbContextFactory.SeedBasicData(db);
        var service = new NotificationService(db);

        await service.CreateNotificationAsync(1, NotificationType.Due_Date, "تذكير 1", "محتوى");
        await service.CreateNotificationAsync(1, NotificationType.Overdue, "تذكير 2", "محتوى");
        await service.CreateNotificationAsync(1, NotificationType.Workflow_Change, "تذكير 3", "محتوى");

        var result = await service.MarkAllAsReadAsync(1);
        result.Success.Should().BeTrue();

        var unread = await db.Notifications.CountAsync(n => !n.IsRead);
        unread.Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_FilterByType()
    {
        var db = TestDbContextFactory.Create(nameof(GetNotifications_FilterByType));
        await TestDbContextFactory.SeedBasicData(db);
        var service = new NotificationService(db);

        await service.CreateNotificationAsync(1, NotificationType.Due_Date, "تذكير", "محتوى");
        await service.CreateNotificationAsync(1, NotificationType.Overdue, "متأخر", "محتوى");

        var result = await service.GetNotificationsAsync(1, NotificationType.Due_Date);
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].NotificationType.Should().Be(NotificationType.Due_Date);
    }
}
