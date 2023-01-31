using Orleans.Concurrency;
using Orleans.Runtime;
using ServicePanel.Grains;
using ServicePanel.Models;

namespace ServicePanel.Agent.Grains;

[StatelessWorker(1)]
public class UpdateRecordGrain : Grain, IUpdateRecordGrain
{
    private readonly IPersistentState<Queue<UpdateRecord>> updateRecord;

    public UpdateRecordGrain([PersistentState("updaterecord")] IPersistentState<Queue<UpdateRecord>> updateRecord)
    {
        this.updateRecord = updateRecord;
    }

    public async Task Add(UpdateRecord record)
    {
        updateRecord.State.Enqueue(record);
        if (updateRecord.State.Count > 100)
        {
            updateRecord.State.Dequeue();
        }
        await updateRecord.WriteStateAsync();
    }

    public Task<List<UpdateRecord>> GetAll()
    {
        return Task.FromResult(updateRecord.State.ToList());
    }
}
