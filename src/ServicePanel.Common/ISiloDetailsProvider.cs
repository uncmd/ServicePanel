using ServicePanel.Models;

namespace ServicePanel;

public interface ISiloDetailsProvider
{
    Task<SiloDetails[]> GetSiloDetails();
}
