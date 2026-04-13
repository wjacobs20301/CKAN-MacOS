using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class NewRepoDialog : Window
    {
        public string RepoName { get; private set; } = "";
        public string RepoUrl  { get; private set; } = "";

        public NewRepoDialog()
        {
            InitializeComponent();
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
            RepoName = RepoNameBox.Text ?? "";
            RepoUrl  = RepoUrlBox.Text ?? "";
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
