namespace ServicePanel.Grains;

public interface IUpdatorGrain : IGrainWithStringKey
{
    public Task Update(string updateAddress, string filePath, string fileName, string[] serviceNames);
}
