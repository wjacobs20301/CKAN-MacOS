using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using CKAN.GUI.Avalonia.ViewModels;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class ManageGameInstancesDialog : Window
    {
        private MainWindowViewModel? mainViewModel;
        private readonly ObservableCollection<InstanceRow> instances = new();

        public ManageGameInstancesDialog()
        {
            InitializeComponent();
        }

        public async Task ShowDialogAsync(MainWindowViewModel vm, Window? owner)
        {
            mainViewModel = vm;
            RefreshInstances();
            InstanceGrid.ItemsSource = instances;

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void RefreshInstances()
        {
            instances.Clear();
            var mgr = mainViewModel?.Manager;
            if (mgr == null) return;

            var autoStart = mgr.Configuration.AutoStartInstance;
            var currentName = mgr.CurrentInstance?.Name;

            // Update the current instance label
            if (!string.IsNullOrEmpty(currentName))
            {
                CurrentInstanceLabel.Text = currentName;
            }
            else if (mgr.CurrentInstance != null)
            {
                CurrentInstanceLabel.Text = mgr.CurrentInstance.GameDir;
            }
            else
            {
                CurrentInstanceLabel.Text = "(none)";
            }

            foreach (var kvp in mgr.Instances)
            {
                try
                {
                    var isCurrent = mgr.CurrentInstance != null
                                    && kvp.Value.GameDir == mgr.CurrentInstance.GameDir;
                    instances.Add(new InstanceRow
                    {
                        Active  = isCurrent ? ">>>" : "",
                        Default = kvp.Key == autoStart ? "*" : "",
                        Name    = string.IsNullOrEmpty(kvp.Key) ? "(unnamed)" : kvp.Key,
                        Game    = kvp.Value.Game.ShortName,
                        Version = kvp.Value.Version()?.ToString() ?? "?",
                        Path    = kvp.Value.GameDir,
                    });
                }
                catch
                {
                    instances.Add(new InstanceRow
                    {
                        Name    = string.IsNullOrEmpty(kvp.Key) ? "(unnamed)" : kvp.Key,
                        Game    = "?",
                        Version = "Error",
                        Path    = "",
                    });
                }
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

        private void OnSelectInstance(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is InstanceRow selected)
            {
                var mgr = mainViewModel?.Manager;
                if (mgr != null)
                {
                    mgr.SetCurrentInstance(selected.Name);
                }
                Close();
            }
        }

        private async void OnAddInstance(object? sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Select Game Directory",
                    AllowMultiple = false,
                });

            if (folders.Count > 0)
            {
                var path = folders[0].Path.LocalPath;
                try
                {
                    var mgr = mainViewModel?.Manager;
                    if (mgr != null)
                    {
                        var name = System.IO.Path.GetFileName(path);
                        mgr.AddInstance(path, name, mgr.User);
                        mgr.SetCurrentInstance(name);
                        RefreshInstances();
                    }
                }
                catch (Exception ex)
                {
                    var errDlg = new ErrorDialog();
                    await errDlg.ShowErrorAsync(ex.Message, this);
                }
            }
        }

        private async void OnCloneInstance(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is not InstanceRow selected) return;

            // Simple clone - prompt for new name
            var inputDlg = new RenameInstanceDialog();
            await inputDlg.ShowDialogAsync("Clone Instance", "New instance name:", selected.Name + " (copy)", this);
            if (!string.IsNullOrEmpty(inputDlg.NewName))
            {
                try
                {
                    if (mainViewModel?.Manager?.Instances.TryGetValue(selected.Name, out var inst) == true)
                    {
                        mainViewModel.Manager.CloneInstance(inst, inputDlg.NewName, inst.GameDir + "_clone");
                        RefreshInstances();
                    }
                }
                catch (Exception ex)
                {
                    var errDlg = new ErrorDialog();
                    await errDlg.ShowErrorAsync(ex.Message, this);
                }
            }
        }

        private async void OnRenameInstance(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is not InstanceRow selected) return;

            var inputDlg = new RenameInstanceDialog();
            await inputDlg.ShowDialogAsync("Rename Instance", "New name:", selected.Name, this);
            if (!string.IsNullOrEmpty(inputDlg.NewName) && inputDlg.NewName != selected.Name)
            {
                try
                {
                    mainViewModel?.Manager?.RenameInstance(selected.Name, inputDlg.NewName);
                    RefreshInstances();
                }
                catch (Exception ex)
                {
                    var errDlg = new ErrorDialog();
                    await errDlg.ShowErrorAsync(ex.Message, this);
                }
            }
        }

        private void OnSetDefault(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is InstanceRow selected)
            {
                var mgr = mainViewModel?.Manager;
                if (mgr != null)
                {
                    mgr.Configuration.AutoStartInstance = selected.Name;
                    mgr.SetCurrentInstance(selected.Name);
                }
                RefreshInstances();
            }
        }

        private void OnRemoveInstance(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is InstanceRow selected)
            {
                mainViewModel?.Manager?.RemoveInstance(selected.Name);
                RefreshInstances();
            }
        }

        private void OnOpenDirectory(object? sender, RoutedEventArgs e)
        {
            if (InstanceGrid.SelectedItem is InstanceRow selected && !string.IsNullOrEmpty(selected.Path))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = selected.Path,
                    UseShellExecute = true
                });
            }
        }
    }

    public class InstanceRow
    {
        public string Active  { get; set; } = "";
        public string Default { get; set; } = "";
        public string Name    { get; set; } = "";
        public string Game    { get; set; } = "";
        public string Version { get; set; } = "";
        public string Path    { get; set; } = "";
    }
}
