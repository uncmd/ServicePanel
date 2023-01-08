using MudBlazor;

namespace ServicePanel.Admin.Services;

public class LayoutService
{
    public bool IsRTL { get; private set; } = false;
    public bool IsDarkMode { get; private set; } = false;

    public MudTheme CurrentTheme { get; private set; }


    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
    }

    public event EventHandler MajorUpdateOccured;

    private void OnMajorUpdateOccured() => MajorUpdateOccured?.Invoke(this, EventArgs.Empty);

    public Task ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        OnMajorUpdateOccured();
        return Task.CompletedTask;
    }

    public Task ToggleRightToLeft()
    {
        IsRTL = !IsRTL;
        OnMajorUpdateOccured();
        return Task.CompletedTask;

    }

    public void SetBaseTheme(MudTheme theme)
    {
        CurrentTheme = theme;
        OnMajorUpdateOccured();
    }
}
