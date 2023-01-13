namespace ServicePanel;

public interface IChat : IGrainObserver
{
    Task ReceiveMessage(string message);
}
