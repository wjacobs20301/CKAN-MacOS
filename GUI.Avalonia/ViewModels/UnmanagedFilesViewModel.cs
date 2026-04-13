using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class UnmanagedFilesViewModel : ObservableObject
    {
        public ObservableCollection<UnmanagedFileEntry> Files { get; } = new();

        [ObservableProperty]
        private bool isLoading;

        public void Refresh(GameInstance? instance, RepositoryDataManager? repoData)
        {
            Files.Clear();
            if (instance == null || repoData == null) return;

            IsLoading = true;
            var registry = RegistryManager.Instance(instance, repoData).registry;
            foreach (var file in instance.UnmanagedFiles(registry))
            {
                Files.Add(new UnmanagedFileEntry { FilePath = file });
            }
            IsLoading = false;
        }
    }

    public class UnmanagedFileEntry
    {
        public string FilePath { get; set; } = "";
    }
}
