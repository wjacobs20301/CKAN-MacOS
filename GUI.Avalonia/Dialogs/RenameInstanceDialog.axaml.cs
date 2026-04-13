using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class RenameInstanceDialog : Window
    {
        public string NewName { get; private set; } = "";

        public RenameInstanceDialog()
        {
            InitializeComponent();
        }

        public async Task ShowDialogAsync(string title, string prompt, string defaultValue, Window? owner)
        {
            Title = title;
            PromptText.Text = prompt;
            NameBox.Text = defaultValue;

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            NewName = NameBox.Text ?? "";
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
