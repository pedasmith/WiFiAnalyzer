using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SpeedTests
{
    public interface IGetSpeedTestOptions
    {
        string GetServer();
        string GetTestType();
        string GetNotes();
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
        public string GetNotes()
        {
            var retval = uiNotes.Text;
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
