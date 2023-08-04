using Microsoft.AspNetCore.SignalR;

namespace ServicePanel.Admin.Services;

public class ServiceControlHub : Hub<IServiceControlClient>
{
    private readonly MachineHoldService _machineHold;
    private readonly DefaultUiNotificationService _notificationService;

    public ServiceControlHub(DefaultUiNotificationService notificationService, MachineHoldService machineHold)
    {
        _notificationService = notificationService;
        _machineHold = machineHold;
    }

    public Task Heartbeat(MachineInfo machineInfo)
    {
        _machineHold.InsertOrUpdate(machineInfo);
        return Task.CompletedTask;
    }

    public async Task SendLog(UiNotificationEventArgs args)
    {
        await _notificationService.Success(args);
    }
}
