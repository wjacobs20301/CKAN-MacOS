using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class SelectionDialog : Window
    {
        private int result = -1;
        private int defaultIndex;

        public SelectionDialog()
        {
            InitializeComponent();
        }

        public async Task<int> ShowSelectionDialogAsync(string message, object[] args, Window? owner)
        {
            MessageText.Text = message;

            // First arg may be default index
            int startIndex = 0;
            if (args.Length > 0 && args[0] is int defIdx)
            {
                defaultIndex = defIdx;
                startIndex = 1;
            }

            var options = args.Skip(startIndex).Select(a => a.ToString() ?? "").ToList();
            OptionsList.ItemsSource = options;

            if (defaultIndex >= 0 && defaultIndex < options.Count)
            {
                OptionsList.SelectedIndex = defaultIndex;
            }

            if (owner != null)
            {
                await ShowDialog(owner);
            }
            return result;
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            result = OptionsList.SelectedIndex;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            result = -1;
            Close();
        }
    }
}
