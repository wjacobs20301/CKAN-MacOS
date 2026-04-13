using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class ErrorDialog : Window
    {
        private string fullText = "";

        public ErrorDialog()
        {
            InitializeComponent();
        }

        public async Task ShowErrorAsync(string message, Window? owner)
            => await ShowErrorAsync(message, null, owner);

        public async Task ShowErrorAsync(string message, string? details, Window? owner)
        {
            ErrorText.Text = message;
            fullText = string.IsNullOrWhiteSpace(details)
                ? message
                : $"{message}\n\n{details}";

            if (!string.IsNullOrWhiteSpace(details))
            {
                DetailsText.Text = details;
                DetailsExpander.IsVisible = true;
            }
            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e) => Close();

        private async void OnCopyClick(object? sender, RoutedEventArgs e)
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(fullText);
            }
        }

        private void OnOpenLogClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Logs", "CKAN");
                Directory.CreateDirectory(logDir);
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDir,
                    UseShellExecute = true,
                });
            }
            catch
            {
                // best-effort; ignore failures
            }
        }
    }
}
