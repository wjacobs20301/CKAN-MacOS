using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Threading;

using CKAN.GUI.Avalonia.Dialogs;
using CKAN.GUI.Avalonia.Services;

namespace CKAN.GUI.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            Opened -= OnOpened;

            if (UpdateChecker.CheckedWithinLast24Hours())
            {
                return;
            }

            // Run the check fully in the background and only show a dialog
            // once the main window is fully idle so startup doesn't feel blocked.
            _ = Task.Run(async () =>
            {
                var release = await UpdateChecker.CheckAsync();
                UpdateChecker.RecordCheckNow();
                if (release is null
                    || UpdateChecker.IsSkipped(release.Tag)
                    || !UpdateChecker.IsNewerThanCurrent(release.Tag))
                {
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await new UpdateAvailableDialog(release).ShowDialogAsync(this);
                });
            });
        }
    }
}
