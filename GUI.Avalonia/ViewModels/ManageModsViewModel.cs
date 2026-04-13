using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Autofac;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CKAN.Configuration;
using CKAN.Versioning;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class ManageModsViewModel : ObservableObject
    {
        public ManageModsViewModel(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            navigationHistory = new NavigationHistory<ModListState>();
            navigationHistory.OnHistoryChange += () =>
            {
                OnPropertyChanged(nameof(CanNavigateBack));
                OnPropertyChanged(nameof(CanNavigateForward));
            };
        }

        private readonly MainWindowViewModel mainViewModel;
        private readonly NavigationHistory<ModListState> navigationHistory;

        public ObservableCollection<ModItemViewModel> AllMods { get; } = new();
        public ObservableCollection<ModItemViewModel> FilteredMods { get; } = new();

        [ObservableProperty]
        private ModItemViewModel? selectedMod;

        [ObservableProperty]
        private ModInfoViewModel? modInfoViewModel;

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string modCountText = "";

        partial void OnSelectedModChanged(ModItemViewModel? value)
        {
            if (value != null)
            {
                ModInfoViewModel = new ModInfoViewModel(value, mainViewModel);
            }
            else
            {
                ModInfoViewModel = null;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyCurrentFilter();
        }

        public bool CanNavigateBack => navigationHistory.CanNavigateBackward;
        public bool CanNavigateForward => navigationHistory.CanNavigateForward;

        private string currentFilter = "Compatible";

        public void ApplyFilter(string filterName)
        {
            currentFilter = filterName;
            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            FilteredMods.Clear();

            IEnumerable<ModItemViewModel> filtered = AllMods;

            // Apply status filter
            filtered = currentFilter switch
            {
                "Compatible"       => filtered.Where(m => !m.IsIncompatible),
                "Installed"        => filtered.Where(m => m.IsInstalled),
                "Upgradeable"      => filtered.Where(m => m.HasUpdate),
                "Replaceable"      => filtered.Where(m => m.HasReplacement),
                "Cached"           => filtered.Where(m => m.IsCached),
                "Newly compatible" => filtered.Where(m => m.IsNew),
                "Not installed"    => filtered.Where(m => !m.IsInstalled && !m.IsIncompatible),
                "Incompatible"     => filtered.Where(m => m.IsIncompatible),
                "All"              => filtered,
                _                  => filtered.Where(m => !m.IsIncompatible),
            };

            // Apply search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = CkanModule.nonAlphaNums.Replace(SearchText.Trim(), "");
                filtered = filtered.Where(m =>
                    m.SearchableName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || m.SearchableIdentifier.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || m.SearchableAbstract.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || m.SearchableDescription.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || m.SearchableAuthors.Any(a => a.Contains(search, StringComparison.OrdinalIgnoreCase))
                    || m.Abbrevation.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var mod in filtered.OrderBy(m => m.Name))
            {
                FilteredMods.Add(mod);
            }

            ModCountText = $"{FilteredMods.Count} mods";
        }

        public void RefreshModList()
        {
            var instance = mainViewModel.CurrentInstance;
            var repoData = mainViewModel.RepoData;
            if (instance == null || repoData == null)
            {
                return;
            }

            var registry = RegistryManager.Instance(instance, repoData).registry;
            var cache = mainViewModel.Manager!.Cache!;
            var stabilityTolerance = instance.StabilityToleranceConfig;

            // Build the mod list in a local list (safe from any thread)
            var allMods = new List<ModItemViewModel>();

            // Add installed modules
            foreach (var instMod in registry.InstalledModules)
            {
                allMods.Add(new ModItemViewModel(
                    instMod, repoData, registry, stabilityTolerance,
                    instance, cache, null, false, false));
            }

            // Add available (not installed) modules
            var installedIds = new HashSet<string>(
                registry.InstalledModules.Select(im => im.identifier));

            foreach (var availMod in registry.CompatibleModules(stabilityTolerance, instance.VersionCriteria()))
            {
                if (!installedIds.Contains(availMod.identifier))
                {
                    allMods.Add(new ModItemViewModel(
                        availMod, repoData, registry, stabilityTolerance,
                        instance, cache, false, false, false));
                }
            }

            // Add incompatible modules
            foreach (var incompMod in registry.IncompatibleModules(stabilityTolerance, instance.VersionCriteria()))
            {
                if (!installedIds.Contains(incompMod.identifier))
                {
                    allMods.Add(new ModItemViewModel(
                        incompMod, repoData, registry, stabilityTolerance,
                        instance, cache, true, false, false));
                }
            }

            // Check for updates
            foreach (var mod in allMods.Where(m => m.IsInstalled && m.InstalledMod != null))
            {
                if (mod.LatestCompatibleMod != null
                    && mod.InstalledMod!.Module.version.IsLessThan(mod.LatestCompatibleMod.version))
                {
                    mod.HasUpdate = true;
                }
            }

            // Subscribe to checkbox changes
            foreach (var mod in allMods)
            {
                mod.InstallCheckedChanged += OnModCheckChanged;
            }

            // Update ObservableCollections on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                AllMods.Clear();
                foreach (var mod in allMods)
                {
                    AllMods.Add(mod);
                }
                ApplyCurrentFilter();
            });
        }

        private void OnModCheckChanged()
        {
            mainViewModel.HasChanges = GetChangeset().Any();
        }

        public void MarkAllUpdates()
        {
            foreach (var mod in FilteredMods.Where(m => m.HasUpdate))
            {
                mod.IsInstallChecked = true;
            }
            mainViewModel.HasChanges = GetChangeset().Any();
        }

        public List<ModChange> GetChangeset()
        {
            var changes = new List<ModChange>();
            // Mods that are checked but not installed -> install
            foreach (var mod in AllMods.Where(m => m.IsInstallChecked && !m.IsInstalled))
            {
                changes.Add(new ModChange(mod.SelectedMod ?? mod.Mod, GUIModChangeType.Install, null));
            }
            // Mods that are installed but unchecked -> remove
            foreach (var mod in AllMods.Where(m => !m.IsInstallChecked && m.IsInstalled && !m.IsAutodetected))
            {
                changes.Add(new ModChange(mod.Mod, GUIModChangeType.Remove, null));
            }
            // Mods that have updates and are checked -> update
            foreach (var mod in AllMods.Where(m => m.IsInstallChecked && m.IsInstalled && m.HasUpdate))
            {
                if (mod.LatestCompatibleMod != null)
                {
                    changes.Add(new ModChange(mod.LatestCompatibleMod, GUIModChangeType.Update, null));
                }
            }
            return changes;
        }

        public Task ApplyChangesAsync()
        {
            var changeset = GetChangeset();
            if (!changeset.Any())
            {
                return Task.CompletedTask;
            }

            mainViewModel.ChangesetViewModel.LoadChanges(changeset);
            mainViewModel.IsChangesetVisible = true;
            mainViewModel.SwitchToTab(1);
            return Task.CompletedTask;
        }

        public void NavigateBack()
        {
            if (navigationHistory.TryGoBackward(out var state) && state != null)
            {
                RestoreState(state);
            }
        }

        public void NavigateForward()
        {
            if (navigationHistory.TryGoForward(out var state) && state != null)
            {
                RestoreState(state);
            }
        }

        private void RestoreState(ModListState state)
        {
            SearchText = state.SearchText;
            currentFilter = state.Filter;
            ApplyCurrentFilter();
        }
    }

    public class ModListState
    {
        public string SearchText { get; set; } = "";
        public string Filter     { get; set; } = "Compatible";
    }

    /// <summary>
    /// Generic class for keeping a browser-like navigation history.
    /// Ported from GUI/NavigationHistory.cs
    /// </summary>
    public class NavigationHistory<T> where T : notnull
    {
        private readonly List<T> history = new();
        private int currentIndex = -1;

        public delegate void HistoryChangeHandler();
        public event HistoryChangeHandler? OnHistoryChange;

        public bool CanNavigateBackward => currentIndex > 0;
        public bool CanNavigateForward  => currentIndex < (history.Count - 1);
        public bool IsReadOnly { get; set; }

        public void AddToHistory(T item)
        {
            if (IsReadOnly) return;
            if (CanNavigateForward)
            {
                history.RemoveRange(currentIndex + 1, history.Count - (currentIndex + 1));
            }
            history.Add(item);
            currentIndex++;
            OnHistoryChange?.Invoke();
        }

        public bool TryGoBackward(out T? item)
        {
            if (!IsReadOnly && CanNavigateBackward)
            {
                item = history[--currentIndex];
                OnHistoryChange?.Invoke();
                return true;
            }
            item = default;
            return false;
        }

        public bool TryGoForward(out T? item)
        {
            if (!IsReadOnly && CanNavigateForward)
            {
                item = history[++currentIndex];
                OnHistoryChange?.Invoke();
                return true;
            }
            item = default;
            return false;
        }
    }
}
