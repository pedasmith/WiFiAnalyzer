using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Vpn;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SmartWiFiControls
{
    public sealed partial class VpnControl : UserControl
    {
        public VpnControl()
        {
            this.InitializeComponent();
        }


        VpnManagementAgent Agent = null;
        private bool EnsureAgent()
        {
            if (Agent == null)
            {
                Agent = new VpnManagementAgent();
            }
            return Agent != null;
        }
        public async Task UpdateVpnList()
        {
            if (!EnsureAgent())
            {
                return;
            }
            uiLog.Text = "";
            var profiles = await Agent.GetProfilesAsync();
            foreach (var profile in profiles )
            {
                uiLog.Text += $"{profile.ProfileName}\n";
            }
        }
    }
}
