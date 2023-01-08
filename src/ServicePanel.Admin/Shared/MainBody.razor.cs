using Microsoft.AspNetCore.Components;
using MudBlazor;
using ServicePanel.Admin.Services;

namespace ServicePanel.Admin.Shared
{
    public partial class MainBody
    {
        [Inject] private LayoutService LayoutService { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private bool _drawerOpen = true;

        protected override Task OnInitializedAsync()
        {
            Snackbar.Add("欢迎来到Windows服务控制面板", Severity.Success);
            return Task.CompletedTask;
        }

        private void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }
    }
}