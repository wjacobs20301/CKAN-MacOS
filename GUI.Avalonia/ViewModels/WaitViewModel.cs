using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class WaitViewModel : ObservableObject
    {
        [ObservableProperty]
        private string progressMessage = "";

        [ObservableProperty]
        private int progressPercent;

        [ObservableProperty]
        private bool isIndeterminate;

        public ObservableCollection<string> LogMessages { get; } = new();

        public void SetProgress(string message, int percent)
        {
            ProgressMessage = message;
            ProgressPercent = percent;
            IsIndeterminate = false;
        }

        public void SetProgress(ByteRateCounter rateCounter)
        {
            ProgressMessage = rateCounter.Summary;
            ProgressPercent = rateCounter.Percent;
            IsIndeterminate = false;
        }

        public void AddLogMessage(string message)
        {
            LogMessages.Add(message);
        }

        public void Reset()
        {
            ProgressMessage = "";
            ProgressPercent = 0;
            IsIndeterminate = false;
            LogMessages.Clear();
        }
    }
}
