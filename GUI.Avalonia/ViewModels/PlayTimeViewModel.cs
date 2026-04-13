using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class PlayTimeViewModel : ObservableObject
    {
        public ObservableCollection<PlayTimeEntry> Entries { get; } = new();

        [ObservableProperty]
        private string totalPlayTime = "";

        public void Refresh(GameInstanceManager? manager)
        {
            Entries.Clear();
            if (manager == null) return;

            // Load play time entries from instances
            // Implementation connects to Core's play time tracking
        }
    }

    public class PlayTimeEntry
    {
        public string InstanceName { get; set; } = "";
        public string PlayTime     { get; set; } = "";
    }
}
