using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SpeedTests
{
    public interface IGetSpeedTestOptions
    {
        string GetServer();
        string GetTestType();
    }
    public sealed partial class SpeedTestOptionControl : UserControl, IGetSpeedTestOptions
    {
        public string GetServer()
        {
            var retval = uiServerList.SelectedItem as string;
            return retval;
        }

        public string GetTestType()
        {
            var retval = (uiStatsType.SelectedItem as ComboBoxItem)?.Tag as String;
            return retval;
        }
        public SpeedTestOptionControl()
        {
            this.InitializeComponent();
            this.Loaded += SpeedTestOptionControl_Loaded;
        }

        private void SpeedTestOptionControl_Loaded(object sender, RoutedEventArgs e)
        {
            var list = SamKnowsServers.GetExampleServers();
            foreach (var item in list)
            {
                uiServerList.Items.Add(item.hostname);
            }
            uiServerList.SelectedIndex = 0;
        }
    }
}
