using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            VersionText.Text = $"Version {Meta.GetVersion(Versioning.VersionFormat.Full)}";
        }

        public async Task ShowDialogAsync(Window? owner)
        {
            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
