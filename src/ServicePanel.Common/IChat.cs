namespace ServicePanel;

public interface IChat : IGrainObserver
{
    void ReceiveMessage(string message);
}
