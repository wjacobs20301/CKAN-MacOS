using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CKAN.GUI.Avalonia.ViewModels
{
    public partial class ChooseRecommendedModsViewModel : ObservableObject
    {
        public ObservableCollection<RecommendedModItem> RecommendedMods { get; } = new();
        public ObservableCollection<RecommendedModItem> SuggestedMods  { get; } = new();

        [ObservableProperty]
        private string message = "";

        public void LoadRecommendations(CkanModule[] recommended, CkanModule[] suggested,
                                         string message)
        {
            RecommendedMods.Clear();
            SuggestedMods.Clear();
            Message = message;

            foreach (var mod in recommended)
            {
                RecommendedMods.Add(new RecommendedModItem(mod, true));
            }
            foreach (var mod in suggested)
            {
                SuggestedMods.Add(new RecommendedModItem(mod, false));
            }
        }
    }

    public partial class RecommendedModItem : ObservableObject
    {
        public RecommendedModItem(CkanModule mod, bool isChecked)
        {
            Mod = mod;
            Name = mod.name;
            Abstract = mod.@abstract;
            IsChecked = isChecked;
        }

        public CkanModule Mod      { get; }
        public string     Name     { get; }
        public string     Abstract { get; }

        [ObservableProperty]
        private bool isChecked;
    }
}
