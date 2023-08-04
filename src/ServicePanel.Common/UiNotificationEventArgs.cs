namespace ServicePanel;

public class UiNotificationEventArgs : EventArgs
{
    public UiNotificationEventArgs() { }

    public UiNotificationEventArgs(UiNotificationType notificationType, string message, string title)
    {
        NotificationType = notificationType;
        Message = message;
        Title = title;
    }

    public UiNotificationType NotificationType { get; set; }

    public string Message { get; set; }

    public string Title { get; set; }
}
