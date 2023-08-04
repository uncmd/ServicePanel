namespace ServicePanel.Admin.Services;

public class DefaultUiNotificationService
{
    /// <summary>
    /// An event raised after the notification is received.
    /// </summary>
    public event EventHandler<UiNotificationEventArgs> NotificationReceived;

    public Task Success(string message, string title = null)
    {
        NotificationReceived?.Invoke(this, new UiNotificationEventArgs(UiNotificationType.Success, message, title));

        return Task.CompletedTask;
    }

    public Task Error(string message, string title = null)
    {
        NotificationReceived?.Invoke(this, new UiNotificationEventArgs(UiNotificationType.Error, message, title));

        return Task.CompletedTask;
    }

    public Task Success(UiNotificationEventArgs args)
    {
        NotificationReceived?.Invoke(this, args);

        return Task.CompletedTask;
    }

    public Task Error(UiNotificationEventArgs args)
    {
        NotificationReceived?.Invoke(this, args);

        return Task.CompletedTask;
    }
}
