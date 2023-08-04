namespace ServicePanel;

public interface IServiceControlClient
{
    Task<ControlResult> ControlService(ControlCommand command);

    Task<List<ServiceInfo>> GetServiceInfo();

    Task<ControlResult> UpdateService(UpdateCommand command);

    Task<ControlResult> UpdateAgent(UpdateCommand command);
}
