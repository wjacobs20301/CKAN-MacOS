using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Autofac;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using CKAN.Configuration;
using CKAN.GUI.Avalonia.ViewModels;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class SettingsDialog : Window
    {
        private MainWindowViewModel? mainViewModel;
        private IConfiguration? config;
        private readonly ObservableCollection<RepoEntry> repos = new();
        private List<string> languageCodes = new();

        private record RepoEntry(string Name, Uri Uri, int Priority)
        {
            public override string ToString() => $"{Name} - {Uri}";
        }

        public SettingsDialog()
        {
            InitializeComponent();
        }

        public async Task ShowDialogAsync(MainWindowViewModel vm, Window? owner)
        {
            mainViewModel = vm;
            config = ServiceLocator.Container.Resolve<IConfiguration>();

            CachePathText.Text = config.DownloadCacheDir ?? "";
            CacheSizeLimit.Value = (config.CacheSizeLimit ?? 0) / (1024 * 1024);
            RefreshOnStartup.IsChecked = config.RefreshRate > 0;

            repos.Clear();
            if (vm.CurrentInstance != null && vm.RepoData != null)
            {
                var registry = RegistryManager.Instance(vm.CurrentInstance, vm.RepoData).registry;
                foreach (var repo in registry.Repositories.Values.OrderBy(r => r.priority))
                {
                    repos.Add(new RepoEntry(repo.name, repo.uri, repo.priority));
                }
            }
            RepoList.ItemsSource = repos;

            // Language — show native display names but persist the culture code.
            languageCodes = Utilities.AvailableLanguages.ToList();
            LanguageCombo.ItemsSource = languageCodes
                .Select(code =>
                {
                    try
                    {
                        var c = CultureInfo.GetCultureInfo(code);
                        return $"{c.NativeName} ({code})";
                    }
                    catch
                    {
                        return code;
                    }
                })
                .ToArray();
            var current = config.Language;
            var idx = current is null ? -1 : languageCodes.IndexOf(current);
            LanguageCombo.SelectedIndex = idx >= 0 ? idx : languageCodes.IndexOf("en-US");

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (config != null)
            {
                config.DownloadCacheDir = CachePathText.Text;
                config.CacheSizeLimit = (long?)((CacheSizeLimit.Value ?? 0) * 1024 * 1024);
                config.RefreshRate = (RefreshOnStartup.IsChecked ?? false) ? 4 : 0;

                if (LanguageCombo.SelectedIndex is int li
                    && li >= 0 && li < languageCodes.Count)
                {
                    config.Language = languageCodes[li];
                }
            }

            PersistRepos();
            Close();
        }

        private void PersistRepos()
        {
            if (mainViewModel?.CurrentInstance is null || mainViewModel.RepoData is null)
            {
                return;
            }
            var regMgr = RegistryManager.Instance(mainViewModel.CurrentInstance, mainViewModel.RepoData);
            var updated = new System.Collections.Generic.SortedDictionary<string, Repository>();
            for (int i = 0; i < repos.Count; i++)
            {
                var r = repos[i];
                updated[r.Name] = new Repository(r.Name, r.Uri) { priority = i };
            }
            regMgr.registry.RepositoriesSet(updated);
            regMgr.Save();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OnBrowseCachePath(object? sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select Cache Directory",
                    AllowMultiple = false,
                });

            if (folders.Count > 0)
            {
                CachePathText.Text = folders[0].Path.LocalPath;
            }
        }

        private async void OnAddRepo(object? sender, RoutedEventArgs e)
        {
            var dialog = new NewRepoDialog();
            await dialog.ShowDialogAsync(this);
            if (!string.IsNullOrEmpty(dialog.RepoName)
                && !string.IsNullOrEmpty(dialog.RepoUrl)
                && Uri.TryCreate(dialog.RepoUrl, UriKind.Absolute, out var uri))
            {
                repos.Add(new RepoEntry(dialog.RepoName, uri, repos.Count));
            }
        }

        private void OnRemoveRepo(object? sender, RoutedEventArgs e)
        {
            if (RepoList.SelectedIndex >= 0)
            {
                repos.RemoveAt(RepoList.SelectedIndex);
            }
        }

        private void OnMoveRepoUp(object? sender, RoutedEventArgs e)
        {
            int idx = RepoList.SelectedIndex;
            if (idx > 0)
            {
                repos.Move(idx, idx - 1);
                RepoList.SelectedIndex = idx - 1;
            }
        }

        private void OnMoveRepoDown(object? sender, RoutedEventArgs e)
        {
            int idx = RepoList.SelectedIndex;
            if (idx >= 0 && idx < repos.Count - 1)
            {
                repos.Move(idx, idx + 1);
                RepoList.SelectedIndex = idx + 1;
            }
        }
    }
}
