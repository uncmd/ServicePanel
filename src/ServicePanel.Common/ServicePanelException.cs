namespace ServicePanel;

public class ServicePanelException : Exception
{
    public int Code { get; set; }

    public ServicePanelException(string message) : base(message) { }

    public ServicePanelException WithCode(int code)
    {
        this.Code = code;
        return this;
    }
}
