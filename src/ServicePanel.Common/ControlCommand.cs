namespace ServicePanel;

public class ControlCommand
{
    public string ConnectionId { get; set; }

    public ControlType ControlType { get; set; }

    public string ServiceName { get; set; }

    public override string ToString()
    {
        return $"{ControlType} {ServiceName}";
    }
}

public class ControlResult
{
    public bool Success { get; set; }

    public string Message { get; set; }

    public static ControlResult OK => new ControlResult { Success = true };

    public static ControlResult Error(string message) => new ControlResult { Success = false, Message = message };
}

public class UpdateCommand
{
    public string ConnectionId { get; set; }

    public string FileName { get; set; }

    public string ServiceName { get; set; }

    public byte[] Buffers { get; set; }
}

public enum ControlType
{
    Start,

    Stop,

    ReStart
}
