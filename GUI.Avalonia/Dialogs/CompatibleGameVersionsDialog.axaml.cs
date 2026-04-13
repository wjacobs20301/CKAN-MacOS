using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CKAN.Versioning;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class CompatibleGameVersionsDialog : Window
    {
        private GameInstance? instance;

        public CompatibleGameVersionsDialog()
        {
            InitializeComponent();
        }

        public async Task ShowDialogAsync(GameInstance inst, Window? owner)
        {
            instance = inst;
            CurrentVersionText.Text = $"Current game version: {inst.Version()}";

            var knownVersions = inst.Game.KnownVersions
                .OrderByDescending(v => v)
                .Select(v => v.ToString())
                .ToList();

            VersionList.ItemsSource = knownVersions;

            // Pre-select currently compatible versions
            var compatible = inst.CompatibleVersions;
            foreach (var ver in compatible)
            {
                var idx = knownVersions.IndexOf(ver.ToString());
                if (idx >= 0)
                {
                    VersionList.Selection.Select(idx);
                }
            }

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (instance != null)
            {
                var selected = VersionList.Selection.SelectedItems
                    .Cast<string>()
                    .Select(s => GameVersion.Parse(s))
                    .ToList();
                instance.SetCompatibleVersions(selected);
            }
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
    }
}
