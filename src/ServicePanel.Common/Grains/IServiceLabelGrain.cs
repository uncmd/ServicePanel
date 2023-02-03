using ServicePanel.Models;

namespace ServicePanel.Grains;

public interface IServiceLabelGrain : IGrainWithIntegerKey
{
    Task AddLabel(ServiceLabel label);

    Task RemoveLabel(string address, string name);

    Task<List<ServiceLabel>> GetLabels();
}
