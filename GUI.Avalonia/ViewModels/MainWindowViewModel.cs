using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Autofac;

using CKAN.Configuration;
using CKAN.Versioning;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel()
        {
            ManageModsViewModel       = new ManageModsViewModel(this);
            ChangesetViewModel        = new ChangesetViewModel(this);
            WaitViewModel             = new WaitViewModel();
            ChooseRecommendedViewModel = new ChooseRecommendedModsViewModel();
            PlayTimeViewModel         = new PlayTimeViewModel();
            UnmanagedFilesViewModel   = new UnmanagedFilesViewModel();
            InstallHistoryViewModel   = new InstallationHistoryViewModel();
            DownloadStatsViewModel    = new DownloadStatisticsViewModel();

            FilterOptions = new ObservableCollection<string>
            {
                "Compatible",
                "Installed",
                "Upgradeable",
                "Replaceable",
                "Cached",
                "Newly compatible",
                "Not installed",
                "Incompatible",
                "All",
            };
            SelectedFilter = "Compatible";
        }

        // Child ViewModels
        public ManageModsViewModel       ManageModsViewModel       { get; }
        public ChangesetViewModel        ChangesetViewModel        { get; }
        public WaitViewModel             WaitViewModel             { get; }
        public ChooseRecommendedModsViewModel ChooseRecommendedViewModel { get; }
        public PlayTimeViewModel         PlayTimeViewModel         { get; }
        public UnmanagedFilesViewModel   UnmanagedFilesViewModel   { get; }
        public InstallationHistoryViewModel InstallHistoryViewModel { get; }
        public DownloadStatisticsViewModel DownloadStatsViewModel  { get; }

        // Core objects
        public GameInstanceManager? Manager     { get; set; }
        public GameInstance?        CurrentInstance => Manager?.CurrentInstance;
        public RepositoryDataManager? RepoData  { get; set; }

        [ObservableProperty]
        private string title = "CKAN Mod Manager";

        [ObservableProperty]
        private string statusMessage = "Ready.";

        [ObservableProperty]
        private int progressPercent;

        [ObservableProperty]
        private bool isProgressVisible;

        [ObservableProperty]
        private string currentInstanceName = "(no instance)";

        [ObservableProperty]
        private int selectedTabIndex;

        [ObservableProperty]
        private bool hasChanges;

        // Tab visibility
        [ObservableProperty]
        private bool isManageModsVisible = true;

        [ObservableProperty]
        private bool isChangesetVisible;

        [ObservableProperty]
        private bool isWaitVisible;

        [ObservableProperty]
        private bool isChooseRecommendedVisible;

        [ObservableProperty]
        private bool isPlayTimeVisible;

        [ObservableProperty]
        private bool isUnmanagedFilesVisible;

        [ObservableProperty]
        private bool isInstallHistoryVisible;

        [ObservableProperty]
        private bool isDownloadStatsVisible;

        // Filter
        public ObservableCollection<string> FilterOptions { get; }

        [ObservableProperty]
        private string selectedFilter = "Compatible";

        partial void OnSelectedFilterChanged(string value)
        {
            ManageModsViewModel.ApplyFilter(value);
        }

        /// <summary>
        /// Initialize the core manager and load mod list for current instance
        /// </summary>
        public async void Initialize(GameInstanceManager manager, string? userAgent)
        {
            Manager = manager;
            RepoData = ServiceLocator.Container.Resolve<RepositoryDataManager>();

            // Try to auto-detect a game instance
            if (CurrentInstance == null)
            {
                manager.GetPreferredInstance();
            }

            // If still no instance, prompt the user to set one up
            while (CurrentInstance == null)
            {
                StatusMessage = "No game instance found. Please add one.";
                var dialog = new Dialogs.ManageGameInstancesDialog();
                await dialog.ShowDialogAsync(this, GetMainWindow());

                // Re-check after dialog closes
                if (CurrentInstance == null)
                {
                    manager.GetPreferredInstance();
                }
            }

            OnInstanceChanged();
        }

        /// <summary>
        /// Refresh UI after instance is set or changed
        /// </summary>
        public void OnInstanceChanged()
        {
            if (CurrentInstance != null)
            {
                var displayName = string.IsNullOrEmpty(CurrentInstance.Name)
                    ? System.IO.Path.GetFileName(CurrentInstance.GameDir)
                    : CurrentInstance.Name;
                CurrentInstanceName = displayName;
                Title = $"CKAN - {displayName}";
                StatusMessage = "Loading mod list...";
                Task.Run(() =>
                {
                    ManageModsViewModel.RefreshModList();
                    Dispatcher.UIThread.Post(() => StatusMessage = "Ready.");
                });
            }
            else
            {
                CurrentInstanceName = "(no instance)";
                Title = "CKAN Mod Manager";
                ManageModsViewModel.AllMods.Clear();
                ManageModsViewModel.FilteredMods.Clear();
                StatusMessage = "No game instance selected.";
            }
        }

        public void SwitchToTab(int tabIndex)
        {
            SelectedTabIndex = tabIndex;
        }

        // Dialog helpers called by AvaloniaUser
        // These must be called from the UI thread directly
        public async Task<bool> ShowYesNoDialogAsync(string question)
        {
            var dialog = new Dialogs.YesNoDialog();
            return await dialog.ShowDialogAsync(question, GetMainWindow());
        }

        public async Task<int> ShowSelectionDialogAsync(string message, object[] args)
        {
            var dialog = new Dialogs.SelectionDialog();
            return await dialog.ShowSelectionDialogAsync(message, args, GetMainWindow());
        }

        public async Task ShowErrorDialogAsync(string message)
        {
            var dialog = new Dialogs.ErrorDialog();
            await dialog.ShowErrorAsync(message, GetMainWindow());
        }

        private static global::Avalonia.Controls.Window? GetMainWindow()
        {
            return global::Avalonia.Application.Current?.ApplicationLifetime
                is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;
        }

        // Commands
        [RelayCommand]
        private void NavigateBack()
        {
            ManageModsViewModel.NavigateBack();
        }

        [RelayCommand]
        private void NavigateForward()
        {
            ManageModsViewModel.NavigateForward();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (Manager is null || CurrentInstance is null || RepoData is null)
            {
                StatusMessage = "No game instance selected.";
                return;
            }
            StatusMessage = "Refreshing mod list...";
            IsProgressVisible = true;
            var manager = Manager;
            var instance = CurrentInstance;
            var repoData = RepoData;
            await Task.Run(() =>
            {
                var regMgr = RegistryManager.Instance(instance, repoData);
                var repos = regMgr.registry.Repositories.Values.ToArray();
                var downloader = new NetAsyncDownloader(manager.User, () => null);
                repoData.Update(repos, instance.Game, false, downloader, manager.User);
            });
            ManageModsViewModel.RefreshModList();
            IsProgressVisible = false;
            StatusMessage = "Ready.";
        }

        [RelayCommand]
        private void UpdateAll()
        {
            ManageModsViewModel.MarkAllUpdates();
        }

        [RelayCommand]
        private async Task ApplyChanges()
        {
            await ManageModsViewModel.ApplyChangesAsync();
        }

        [RelayCommand]
        private void LaunchGame()
        {
            if (CurrentInstance != null && Manager != null)
            {
                var commandLines = CurrentInstance.Game.DefaultCommandLines(
                    Manager.SteamLibrary,
                    new System.IO.DirectoryInfo(CurrentInstance.GameDir));
                if (commandLines.Length > 0)
                {
                    CurrentInstance.PlayGame(commandLines[0]);
                }
            }
        }

        [RelayCommand]
        private async Task ManageInstances()
        {
            var dialog = new Dialogs.ManageGameInstancesDialog();
            await dialog.ShowDialogAsync(this, GetMainWindow());
            OnInstanceChanged();
        }

        [RelayCommand]
        private async Task Settings()
        {
            var dialog = new Dialogs.SettingsDialog();
            await dialog.ShowDialogAsync(this, GetMainWindow());
        }

        [RelayCommand]
        private void Import()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void Export()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void Plugins()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void PreferredHosts()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void InstallFilters()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void GameCommandLine()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void CompatibleVersions()
        {
            // Will be implemented in Phase 4
        }

        [RelayCommand]
        private void UserGuide()
        {
            OpenUrl("https://github.com/KSP-CKAN/CKAN/wiki");
        }

        [RelayCommand]
        private void Discord()
        {
            OpenUrl("https://discord.gg/Mb4nXQD");
        }

        [RelayCommand]
        private void ReportIssue()
        {
            OpenUrl("https://github.com/wjacobs20301/CKAN-MacOS/issues/new/choose");
        }

        [RelayCommand]
        private async Task About()
        {
            var dialog = new Dialogs.AboutDialog();
            await dialog.ShowDialogAsync(GetMainWindow());
        }

        [RelayCommand]
        private async Task CheckForUpdates()
        {
            var release = await Services.UpdateChecker.CheckAsync();
            Services.UpdateChecker.RecordCheckNow();
            if (release == null)
            {
                StatusMessage = "Could not reach the update server.";
                return;
            }
            if (!Services.UpdateChecker.IsNewerThanCurrent(release.Tag))
            {
                StatusMessage = $"You're up to date (v{Meta.GetVersion(Versioning.VersionFormat.Normal)}).";
                return;
            }
            await new Dialogs.UpdateAvailableDialog(release).ShowDialogAsync(GetMainWindow());
        }

        [RelayCommand]
        private void Exit()
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime
                is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private static void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
