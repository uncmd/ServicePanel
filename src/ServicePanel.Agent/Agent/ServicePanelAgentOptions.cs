namespace ServicePanel.Agent;

public class ServicePanelAgentOptions
{
    public const string Section = "Agent";

    public string AdminUrl { get; set; }

    public int HeartbeatIntervalSeconds { get; set; } = 10;

    public string ServiceKey { get; set; }
}
