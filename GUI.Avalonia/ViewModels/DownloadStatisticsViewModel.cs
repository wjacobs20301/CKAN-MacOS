using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class DownloadStatisticsViewModel : ObservableObject
    {
        public ObservableCollection<DownloadStatEntry> TopMods { get; } = new();

        [ObservableProperty]
        private long totalDownloads;

        public void Refresh(GameInstance? instance, RepositoryDataManager? repoData)
        {
            TopMods.Clear();
            TotalDownloads = 0;

            if (instance == null || repoData == null) return;

            var registry = RegistryManager.Instance(instance, repoData).registry;
            var repos = registry.Repositories.Values;

            var allMods = registry.CompatibleModules(
                instance.StabilityToleranceConfig,
                instance.VersionCriteria());

            var stats = allMods
                .Select(m => new
                {
                    m.name,
                    count = repoData.GetDownloadCount(repos, m.identifier) ?? 0
                })
                .Where(s => s.count > 0)
                .OrderByDescending(s => s.count)
                .Take(50);

            foreach (var stat in stats)
            {
                TopMods.Add(new DownloadStatEntry
                {
                    ModName       = stat.name,
                    DownloadCount = stat.count,
                });
                TotalDownloads += stat.count;
            }
        }
    }

    public class DownloadStatEntry
    {
        public string ModName       { get; set; } = "";
        public int    DownloadCount { get; set; }
    }
}
