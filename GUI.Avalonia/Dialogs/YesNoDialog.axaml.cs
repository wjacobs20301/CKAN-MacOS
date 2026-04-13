using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class YesNoDialog : Window
    {
        private bool result;

        public YesNoDialog()
        {
            InitializeComponent();
        }

        public async Task<bool> ShowDialogAsync(string question, Window? owner)
        {
            QuestionText.Text = question;
            if (owner != null)
            {
                await ShowDialog(owner);
            }
            return result;
        }

        private void OnYesClick(object? sender, RoutedEventArgs e)
        {
            result = true;
            Close();
        }

        private void OnNoClick(object? sender, RoutedEventArgs e)
        {
            result = false;
            Close();
        }
    }
}
