namespace ServicePanel.Models;

[GenerateSerializer]
public class UpdateRecord
{
    [Id(0)]
    public DateTimeOffset UpdateTime { get; set; }

    [Id(1)]
    public string Address { get; set; }

    [Id(2)]
    public string ServiceName { get; set; }

    [Id(3)]
    public int TotalFiles { get; set; }
}
