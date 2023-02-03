namespace ServicePanel.Models;

[GenerateSerializer]
public class ServiceLabel
{
    [Id(0)]
    public string Name { get; set; }

    [Id(1)]
    public string Description { get; set; }

    [Id(2)]
    public string Address { get; set; }
}
