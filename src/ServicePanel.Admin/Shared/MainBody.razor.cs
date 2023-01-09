using Microsoft.AspNetCore.Components;
using ServicePanel.Admin.Services;

namespace ServicePanel.Admin.Shared
{
    public partial class MainBody
    {
        [Inject] private LayoutService LayoutService { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private bool _drawerOpen = true;

        private void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }
    }
}