using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class PreferredHostsDialog : Window
    {
        private readonly ObservableCollection<string> hosts = new();

        public PreferredHostsDialog()
        {
            InitializeComponent();
            HostList.ItemsSource = hosts;
        }

        public async Task ShowDialogAsync(Window? owner)
        {
            // Load known download hosts
            hosts.Clear();
            hosts.Add("github.com");
            hosts.Add("spacedock.info");
            hosts.Add("archive.org");
            hosts.Add("curseforge.com");
            hosts.Add("dropbox.com");
            hosts.Add("drive.google.com");

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e) => Close();
        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        private void OnMoveUp(object? sender, RoutedEventArgs e)
        {
            int idx = HostList.SelectedIndex;
            if (idx > 0)
            {
                hosts.Move(idx, idx - 1);
                HostList.SelectedIndex = idx - 1;
            }
        }

        private void OnMoveDown(object? sender, RoutedEventArgs e)
        {
            int idx = HostList.SelectedIndex;
            if (idx >= 0 && idx < hosts.Count - 1)
            {
                hosts.Move(idx, idx + 1);
                HostList.SelectedIndex = idx + 1;
            }
        }
    }
}
