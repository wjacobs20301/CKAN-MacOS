using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class InstallationHistoryViewModel : ObservableObject
    {
        public ObservableCollection<HistoryEntry> History { get; } = new();

        [ObservableProperty]
        private HistoryEntry? selectedEntry;

        public void Refresh(GameInstance? instance)
        {
            History.Clear();
            if (instance == null) return;

            try
            {
                foreach (var fileInfo in instance.InstallHistoryFiles())
                {
                    History.Add(new HistoryEntry
                    {
                        Timestamp   = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        FileName    = fileInfo.Name,
                        Description = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    });
                }
            }
            catch
            {
                // History directory may not exist yet
            }
        }
    }

    public class HistoryEntry
    {
        public string Timestamp   { get; set; } = "";
        public string FileName    { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
