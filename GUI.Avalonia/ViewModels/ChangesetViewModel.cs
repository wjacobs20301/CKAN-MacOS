using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CKAN.Configuration;
using CKAN.IO;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class ChangesetViewModel : ObservableObject
    {
        public ChangesetViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        private readonly MainWindowViewModel mainViewModel;

        public ObservableCollection<ChangesetItemViewModel> Changes { get; } = new();

        [ObservableProperty]
        private bool hasConflicts;

        [ObservableProperty]
        private string conflictMessage = "";

        public void LoadChanges(List<ModChange> changes)
        {
            Changes.Clear();
            HasConflicts = false;
            ConflictMessage = "";

            foreach (var change in changes)
            {
                Changes.Add(new ChangesetItemViewModel(change));
            }
        }

        [RelayCommand]
        private void RemoveChange(ChangesetItemViewModel item)
        {
            Changes.Remove(item);
            if (!Changes.Any())
            {
                mainViewModel.HasChanges = false;
                mainViewModel.IsChangesetVisible = false;
                mainViewModel.SwitchToTab(0);
            }
        }

        [RelayCommand]
        private async Task Confirm()
        {
            mainViewModel.IsWaitVisible = true;
            mainViewModel.SwitchToTab(2);
            mainViewModel.WaitViewModel.Reset();

            var instance = mainViewModel.CurrentInstance;
            var repoData = mainViewModel.RepoData;
            if (instance == null || repoData == null) return;

            bool errorOccurred = false;
            bool cancelled = false;
            string errorMessage = "";

            await Task.Run(() =>
            {
                try
                {
                    var config = ServiceLocator.Container.Resolve<IConfiguration>();
                    var regMgr = RegistryManager.Instance(instance, repoData);
                    var cache = mainViewModel.Manager!.Cache!;
                    var installer = new ModuleInstaller(instance, cache, config,
                                                       mainViewModel.Manager.User);

                    var toInstall = Changes
                        .Where(c => c.ChangeType == GUIModChangeType.Install
                                 || c.ChangeType == GUIModChangeType.Update)
                        .Select(c => c.Mod)
                        .ToList();

                    var toRemove = Changes
                        .Where(c => c.ChangeType == GUIModChangeType.Remove)
                        .Select(c => c.Mod.identifier)
                        .ToList();

                    HashSet<string>? possibleConfigOnlyDirs = null;

                    if (toRemove.Any())
                    {
                        installer.UninstallList(toRemove,
                            ref possibleConfigOnlyDirs, regMgr, false);
                    }

                    if (toInstall.Any())
                    {
                        var autoInstalled = new HashSet<CkanModule>();

                        // Retry loop for provider selection
                        while (true)
                        {
                            try
                            {
                                installer.InstallList(toInstall,
                                    new RelationshipResolverOptions(instance.StabilityToleranceConfig),
                                    regMgr, ref possibleConfigOnlyDirs,
                                    autoInstalled: autoInstalled);
                                break; // Success
                            }
                            catch (TooManyModsProvideKraken k)
                            {
                                // Ask user to choose a provider
                                var choices = k.modules.Select(m => (object)$"{m.name} ({m.identifier})").ToArray();
                                var chosen = mainViewModel.Manager.User.RaiseSelectionDialog(
                                    k.Message, choices);

                                if (chosen >= 0 && chosen < k.modules.Count)
                                {
                                    // Add chosen provider to the install list and retry
                                    toInstall.Add(k.modules[chosen]);
                                    autoInstalled.Add(k.modules[chosen]);
                                }
                                else
                                {
                                    throw new CancelledActionKraken("Provider selection cancelled.");
                                }
                            }
                        }
                    }

                    mainViewModel.WaitViewModel.AddLogMessage("Installation complete!");
                }
                catch (CancelledActionKraken)
                {
                    mainViewModel.WaitViewModel.AddLogMessage("Operation cancelled by user.");
                    cancelled = true;
                }
                catch (Exception ex)
                {
                    mainViewModel.WaitViewModel.AddLogMessage($"Error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        mainViewModel.WaitViewModel.AddLogMessage($"  Detail: {ex.InnerException.Message}");
                    }
                    errorOccurred = true;
                    errorMessage = ex.Message;
                }
            });

            // Refresh mod list
            mainViewModel.ManageModsViewModel.RefreshModList();
            mainViewModel.HasChanges = false;
            mainViewModel.IsWaitVisible = false;
            mainViewModel.IsChangesetVisible = false;
            mainViewModel.SwitchToTab(0);

            if (errorOccurred)
            {
                mainViewModel.StatusMessage = $"Error: {errorMessage}";
                await mainViewModel.ShowErrorDialogAsync($"Installation failed: {errorMessage}");
            }
            else if (cancelled)
            {
                mainViewModel.StatusMessage = "Operation cancelled.";
            }
            else
            {
                mainViewModel.StatusMessage = "Changes applied successfully.";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            Changes.Clear();
            mainViewModel.HasChanges = false;
            mainViewModel.IsChangesetVisible = false;
            mainViewModel.SwitchToTab(0);
        }
    }

    public class ChangesetItemViewModel : ObservableObject
    {
        public ChangesetItemViewModel(ModChange change)
        {
            Mod = change.Mod;
            ChangeType = change.ChangeType;
            Reason = change.Reason?.ToString() ?? "";
        }

        public CkanModule Mod              { get; }
        public string     Name             => Mod.name;
        public string     Version          => Mod.version.ToString();
        public GUIModChangeType ChangeType { get; }
        public string     ChangeTypeString => ChangeType.ToString();
        public string     Reason           { get; }
    }
}
