using Orleans.Concurrency;
using Orleans.Runtime;
using ServicePanel.Grains;
using ServicePanel.Models;

namespace ServicePanel.Agent.Grains;

[StatelessWorker(1)]
public class ServiceLabelGrain : Grain, IServiceLabelGrain
{
    private readonly IPersistentState<HashSet<ServiceLabel>> serviceLabel;

    public ServiceLabelGrain([PersistentState("servicelabel")] IPersistentState<HashSet<ServiceLabel>> serviceLabel)
    {
        this.serviceLabel = serviceLabel;
    }

    public Task<List<ServiceLabel>> GetLabels()
    {
        return Task.FromResult(serviceLabel.State.ToList());
    }

    public async Task AddLabel(ServiceLabel label)
    {
        if (label == null || string.IsNullOrWhiteSpace(label.Name) || string.IsNullOrWhiteSpace(label.Address))
            return;

        serviceLabel.State.Add(label);
        await serviceLabel.WriteStateAsync();
    }

    public async Task RemoveLabel(string address, string name)
    {
        serviceLabel.State.RemoveWhere(p => p.Address == address && p.Name == name);
        await serviceLabel.WriteStateAsync();
    }
}
