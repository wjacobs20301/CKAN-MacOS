using System;
using System.Collections.Generic;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CKAN.Configuration;
using CKAN.Versioning;

namespace CKAN.GUI.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel wrapping a CkanModule for display in the mod list DataGrid.
    /// Equivalent of GUIMod in the WinForms GUI.
    /// </summary>
    public partial class ModItemViewModel : ObservableObject
    {
        public ModItemViewModel(CkanModule               mod,
                                RepositoryDataManager    repoDataMgr,
                                IRegistryQuerier         registry,
                                StabilityToleranceConfig stabilityTolerance,
                                GameInstance             instance,
                                NetModuleCache           cache,
                                bool? incompatible,
                                bool  hideEpochs,
                                bool  hideV)
        {
            Mod = mod;
            Identifier = mod.identifier;
            Name = mod.name.Trim();
            Abstract = mod.@abstract.Trim();
            Description = mod.description?.Trim() ?? "";
            Authors = mod.author ?? new List<string>();
            IsAutodetected = registry.IsAutodetected(Identifier);
            DownloadCount = repoDataMgr.GetDownloadCount(registry.Repositories.Values, Identifier);

            Abbrevation = new string(Name.Split(' ')
                                        .Select(s => s.Length > 0 ? (char?)s[0] : null)
                                        .OfType<char>()
                                        .ToArray());

            SearchableName        = mod.SearchableName;
            SearchableIdentifier  = CkanModule.nonAlphaNums.Replace(Identifier, "");
            SearchableAbstract    = mod.SearchableAbstract;
            SearchableDescription = mod.SearchableDescription;
            SearchableAuthors     = mod.SearchableAuthors;

            // Find latest compatible version
            ModuleVersion? latestVersion = null;
            if (incompatible != true)
            {
                try
                {
                    LatestCompatibleMod = registry.LatestAvailable(
                        Identifier, stabilityTolerance,
                        instance.VersionCriteria(), null,
                        registry.InstalledModules.Select(im => im.Module).ToArray());
                    latestVersion = LatestCompatibleMod?.version;
                }
                catch (ModuleNotFoundKraken)
                {
                    if (!incompatible.HasValue)
                    {
                        incompatible = true;
                    }
                }
            }

            IsIncompatible = incompatible ?? (LatestCompatibleMod == null);

            try
            {
                LatestAvailableMod = registry.LatestAvailable(
                    Identifier, stabilityTolerance, null, null,
                    registry.InstalledModules.Select(im => im.Module).ToArray());
            }
            catch { }

            if (LatestAvailableMod != null)
            {
                GameCompatibilityVersion = registry.LatestCompatibleGameVersion(
                    instance.Game.KnownVersions, Identifier);
            }

            if (latestVersion != null)
            {
                LatestVersion = latestVersion.ToString(hideEpochs, hideV);
            }
            else if (LatestAvailableMod != null)
            {
                LatestVersion = LatestAvailableMod.version.ToString(hideEpochs, hideV);
            }
            else
            {
                LatestVersion = "-";
            }

            HasReplacement = registry.GetReplacement(mod, stabilityTolerance, instance.VersionCriteria()) != null;
            DownloadSize = mod.download_size == 0 ? "N/A" : CkanModule.FmtSize(mod.download_size);
            InstallSize  = mod.install_size  == 0 ? "N/A" : CkanModule.FmtSize(mod.install_size);

            if (GameCompatibilityVersion == null)
            {
                GameCompatibilityVersion = mod.LatestCompatibleGameVersion();
                if (GameCompatibilityVersion.IsAny)
                {
                    GameCompatibilityVersion = mod.LatestCompatibleRealGameVersion(
                        instance.Game.KnownVersions);
                }
            }

            UpdateIsCached(cache);
        }

        /// <summary>
        /// Initialize from an installed module
        /// </summary>
        public ModItemViewModel(InstalledModule          instMod,
                                RepositoryDataManager    repoDataMgr,
                                IRegistryQuerier         registry,
                                StabilityToleranceConfig stabilityTolerance,
                                GameInstance             instance,
                                NetModuleCache           cache,
                                bool? incompatible,
                                bool  hideEpochs,
                                bool  hideV)
            : this(instMod.Module, repoDataMgr, registry, stabilityTolerance,
                   instance, cache, incompatible, hideEpochs, hideV)
        {
            IsInstalled      = true;
            isInstallChecked = true;  // start checked (lowercase to avoid triggering event)
            InstalledMod     = instMod;
            SelectedMod  = registry.GetModuleByVersion(instMod.identifier, instMod.Module.version)
                           ?? instMod.Module;
            InstallDate  = instMod.InstallTime;
            InstalledVersion = instMod.Module.version.ToString(hideEpochs, hideV);

            if (string.IsNullOrEmpty(LatestVersion) || LatestVersion == "-")
            {
                LatestVersion = InstalledVersion;
            }

            IsIncompatible = incompatible ?? (LatestCompatibleMod == null
                             && !instMod.Module.IsCompatible(instance.VersionCriteria()));
        }

        // Core data
        public CkanModule       Mod                 { get; }
        public CkanModule?      LatestCompatibleMod { get; private set; }
        public CkanModule?      LatestAvailableMod  { get; private set; }
        public InstalledModule? InstalledMod        { get; private set; }

        [ObservableProperty]
        private CkanModule? selectedMod;

        // Display properties
        public string       Identifier              { get; }
        public string       Name                    { get; }
        public string       Abstract                { get; }
        public string       Description             { get; }
        public List<string> Authors                 { get; }
        public string       AuthorsString           => string.Join(", ", Authors);
        public string       Abbrevation             { get; }

        [ObservableProperty]
        private bool isInstalled;

        public bool IsAutoInstalled => InstalledMod?.AutoInstalled ?? false;

        [ObservableProperty]
        private bool hasUpdate;

        public bool HasReplacement   { get; }
        public bool IsIncompatible   { get; }
        public bool IsAutodetected   { get; }

        public string? InstalledVersion { get; private set; }
        public DateTime? InstallDate    { get; private set; }
        public string  LatestVersion   { get; private set; }
        public string  DownloadSize    { get; private set; }
        public string  InstallSize     { get; private set; }
        public int?    DownloadCount   { get; private set; }

        [ObservableProperty]
        private bool isCached;

        public GameVersion? GameCompatibilityVersion { get; private set; }

        public string GameCompatibility
            => GameCompatibilityVersion == null ? "Unknown"
               : GameCompatibilityVersion.IsAny ? GameVersion.AnyString
               : GameCompatibilityVersion.ToString() ?? "Unknown";

        public string Version => InstalledVersion ?? LatestVersion;
        public bool IsNew { get; set; }

        // Searchable fields
        public string       SearchableName        { get; }
        public string       SearchableIdentifier  { get; }
        public string       SearchableAbstract    { get; }
        public string       SearchableDescription { get; }
        public List<string> SearchableAuthors     { get; }

        public bool IsInstallable()
            => !IsIncompatible || IsInstalled;

        public void UpdateIsCached(NetModuleCache cache)
        {
            IsCached = Mod.download is { Count: > 0 }
                       && cache.IsMaybeCachedZip(Mod);
        }

        [ObservableProperty]
        private bool isInstallChecked;

        /// <summary>
        /// Raised when the install checkbox changes so the parent can update HasChanges.
        /// </summary>
        public event Action? InstallCheckedChanged;

        partial void OnIsInstallCheckedChanged(bool value)
        {
            InstallCheckedChanged?.Invoke();
        }
    }
}
