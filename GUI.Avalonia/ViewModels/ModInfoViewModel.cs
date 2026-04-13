using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CKAN.Versioning;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class ModInfoViewModel : ObservableObject
    {
        public ModInfoViewModel(ModItemViewModel mod, MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            Mod = mod;

            // Metadata
            Title       = mod.Name;
            Identifier  = mod.Identifier;
            Abstract    = mod.Abstract;
            Description = mod.Description;
            Authors     = mod.AuthorsString;
            Version     = mod.Version;
            License     = mod.Mod.license?.ToString() ?? "Unknown";

            if (mod.Mod.resources != null)
            {
                Homepage   = mod.Mod.resources.homepage?.ToString();
                Repository = mod.Mod.resources.repository?.ToString();
                Bugtracker = mod.Mod.resources.bugtracker?.ToString();
                SpaceDock  = mod.Mod.resources.spacedock?.ToString();
                Curse      = mod.Mod.resources.curse?.ToString();
            }

            GameCompatibility = mod.GameCompatibility;
            DownloadSize      = mod.DownloadSize;
            InstallSize       = mod.InstallSize;
            DownloadCount     = mod.DownloadCount?.ToString("N0") ?? "N/A";
            ReleaseDate       = mod.Mod.release_date?.ToString("yyyy-MM-dd");

            // Relationships
            LoadRelationships(mod.Mod);

            // Versions
            LoadVersions(mod, mainViewModel);
        }

        private readonly MainWindowViewModel mainViewModel;

        public ModItemViewModel Mod { get; }

        // Metadata tab
        public string  Title             { get; }
        public string  Identifier        { get; }
        public string  Abstract          { get; }
        public string  Description       { get; }
        public string  Authors           { get; }
        public string  Version           { get; }
        public string  License           { get; }
        public string? Homepage          { get; }
        public string? Repository        { get; }
        public string? Bugtracker        { get; }
        public string? SpaceDock         { get; }
        public string? Curse             { get; }
        public string  GameCompatibility { get; }
        public string  DownloadSize      { get; }
        public string  InstallSize       { get; }
        public string  DownloadCount     { get; }
        public string? ReleaseDate       { get; }

        // Relationships tab
        public ObservableCollection<RelationshipNode> Dependencies  { get; } = new();
        public ObservableCollection<RelationshipNode> Recommends    { get; } = new();
        public ObservableCollection<RelationshipNode> Suggests      { get; } = new();
        public ObservableCollection<RelationshipNode> Conflicts     { get; } = new();
        public ObservableCollection<RelationshipNode> Supports      { get; } = new();

        // Versions tab
        public ObservableCollection<VersionEntry> Versions { get; } = new();

        // Contents tab
        public ObservableCollection<string> Contents { get; } = new();

        [ObservableProperty]
        private int selectedInfoTab;

        private void LoadRelationships(CkanModule mod)
        {
            if (mod.depends != null)
            {
                foreach (var dep in mod.depends)
                {
                    Dependencies.Add(new RelationshipNode(dep.ToString() ?? "", "Depends"));
                }
            }
            if (mod.recommends != null)
            {
                foreach (var rec in mod.recommends)
                {
                    Recommends.Add(new RelationshipNode(rec.ToString() ?? "", "Recommends"));
                }
            }
            if (mod.suggests != null)
            {
                foreach (var sug in mod.suggests)
                {
                    Suggests.Add(new RelationshipNode(sug.ToString() ?? "", "Suggests"));
                }
            }
            if (mod.conflicts != null)
            {
                foreach (var con in mod.conflicts)
                {
                    Conflicts.Add(new RelationshipNode(con.ToString() ?? "", "Conflicts"));
                }
            }
            if (mod.supports != null)
            {
                foreach (var sup in mod.supports)
                {
                    Supports.Add(new RelationshipNode(sup.ToString() ?? "", "Supports"));
                }
            }
        }

        private void LoadVersions(ModItemViewModel mod, MainWindowViewModel mainVM)
        {
            var instance = mainVM.CurrentInstance;
            var repoData = mainVM.RepoData;
            if (instance == null || repoData == null) return;

            var registry = RegistryManager.Instance(instance, repoData).registry;
            try
            {
                var available = registry.AvailableByIdentifier(mod.Identifier);
                if (available != null)
                {
                    foreach (var ver in available.OrderByDescending(m => m.version))
                    {
                        var isCompat = ver.IsCompatible(instance.VersionCriteria());
                        Versions.Add(new VersionEntry(
                            ver.version.ToString(),
                            isCompat,
                            ver.Equals(mod.InstalledMod?.Module)));
                    }
                }
            }
            catch { }
        }

        public void LoadContents()
        {
            Contents.Clear();
            if (Mod.InstalledMod != null)
            {
                foreach (var file in Mod.InstalledMod.Files)
                {
                    Contents.Add(file);
                }
            }
        }
    }

    public class RelationshipNode
    {
        public RelationshipNode(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }

    public class VersionEntry
    {
        public VersionEntry(string version, bool isCompatible, bool isInstalled)
        {
            Version      = version;
            IsCompatible = isCompatible;
            IsInstalled  = isInstalled;
        }

        public string Version      { get; }
        public bool   IsCompatible { get; }
        public bool   IsInstalled  { get; }
    }
}
