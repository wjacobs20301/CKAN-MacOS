using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Autofac;
using Avalonia.Controls;
using Avalonia.Interactivity;

using CKAN.Configuration;

namespace CKAN.GUI.Avalonia.Dialogs
{
    public partial class InstallFiltersDialog : Window
    {
        private readonly ObservableCollection<string> filters = new();
        private readonly ObservableCollection<string> instanceFilters = new();
        private GameInstance? gameInstance;

        public InstallFiltersDialog()
        {
            InitializeComponent();
            FilterList.ItemsSource = filters;
            InstanceFilterList.ItemsSource = instanceFilters;
        }

        public async Task ShowDialogAsync(GameInstance? instance, Window? owner)
        {
            gameInstance = instance;

            // Load global filters from config
            var config = ServiceLocator.Container.Resolve<IConfiguration>();
            if (instance != null)
            {
                foreach (var filter in config.GetGlobalInstallFilters(instance.Game))
                {
                    filters.Add(filter);
                }

                // Load instance-specific filters
                foreach (var filter in instance.InstallFilters)
                {
                    instanceFilters.Add(filter);
                }
            }

            if (owner != null)
            {
                await ShowDialog(owner);
            }
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            if (gameInstance != null)
            {
                var config = ServiceLocator.Container.Resolve<IConfiguration>();
                config.SetGlobalInstallFilters(gameInstance.Game,
                    new string[filters.Count].CopyFrom(filters));
            }
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        private void OnAddFilter(object? sender, RoutedEventArgs e)
        {
            filters.Add("*.txt");
        }

        private void OnRemoveFilter(object? sender, RoutedEventArgs e)
        {
            if (FilterList.SelectedItem is string item)
                filters.Remove(item);
        }

        private void OnAddInstanceFilter(object? sender, RoutedEventArgs e)
        {
            instanceFilters.Add("*.txt");
        }

        private void OnRemoveInstanceFilter(object? sender, RoutedEventArgs e)
        {
            if (InstanceFilterList.SelectedItem is string item)
                instanceFilters.Remove(item);
        }
    }

    internal static class CollectionExtensions
    {
        public static string[] CopyFrom(this string[] arr, ObservableCollection<string> source)
        {
            for (int i = 0; i < source.Count && i < arr.Length; i++)
            {
                arr[i] = source[i];
            }
            return arr;
        }
    }
}
