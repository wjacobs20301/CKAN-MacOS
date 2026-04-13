using System.Diagnostics;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CKAN.GUI.Avalonia.Services;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class UpdateAvailableDialog : Window
    {
        private readonly ReleaseInfo release;

        /// <summary>
        /// Parameterless constructor exists only to satisfy the Avalonia
        /// XAML runtime loader. Do not call at runtime.
        /// </summary>
        public UpdateAvailableDialog()
        {
            throw new System.InvalidOperationException(
                "UpdateAvailableDialog requires a ReleaseInfo; use the other constructor.");
        }

        public UpdateAvailableDialog(ReleaseInfo release)
        {
            if (release is null)
            {
                throw new System.ArgumentNullException(nameof(release));
            }
            this.release = release;
            InitializeComponent();
            VersionLine.Text = $"Installed: {Meta.GetVersion(Versioning.VersionFormat.Normal)}    →    Latest: {release.Tag}";
            ReleaseNotes.Text = string.IsNullOrWhiteSpace(release.Body)
                ? "(no release notes)"
                : release.Body;
        }

        public async Task ShowDialogAsync(Window? owner)
        {
            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnDownloadClick(object? sender, RoutedEventArgs e)
        {
            var url = release.DmgUrl ?? release.HtmlUrl;
            Process.Start(new ProcessStartInfo { FileName = url.ToString(), UseShellExecute = true });
            Close();
        }

        private void OnLaterClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSkipClick(object? sender, RoutedEventArgs e)
        {
            UpdateChecker.Skip(release.Tag);
            Close();
        }
    }
}
