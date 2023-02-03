namespace ServicePanel.Grains;

public interface IUpdatorGrain : IGrainWithStringKey
{
    Task Update(byte[] buffers, string fileName, string[] serviceNames);

    Task Subscribe(IChat observer);

    Task UnSubscribe(IChat observer);
}
