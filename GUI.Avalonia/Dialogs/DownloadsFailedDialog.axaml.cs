using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public enum DownloadFailedAction
    {
        Retry,
        Skip,
        Cancel
    }

    public partial class DownloadsFailedDialog : Window
    {
        public DownloadFailedAction Result { get; private set; } = DownloadFailedAction.Cancel;

        public DownloadsFailedDialog()
        {
            InitializeComponent();
        }

        public async Task ShowDialogAsync(string[] failedMods, Window? owner)
        {
            FailedList.ItemsSource = failedMods;
            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnRetryClick(object? sender, RoutedEventArgs e)
        {
            Result = DownloadFailedAction.Retry;
            Close();
        }

        private void OnSkipClick(object? sender, RoutedEventArgs e)
        {
            Result = DownloadFailedAction.Skip;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Result = DownloadFailedAction.Cancel;
            Close();
        }
    }
}
