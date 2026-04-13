using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using CKAN.GUI.Avalonia.Dialogs;
using CKAN.GUI.Avalonia.ViewModels;
using CKAN.GUI.Avalonia.Views;

namespace CKAN.GUI.Avalonia
{
    public class App : Application
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(App));

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                log.Error("Unhandled AppDomain exception", e.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                log.Error("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                log.Error("Unhandled UI-thread exception", e.Exception);
                e.Handled = true;
                ShowFatalError(e.Exception);
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var viewModel = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = viewModel
                };

                var manager = AvaloniaGui.ManagerInstance;
                if (manager != null)
                {
                    var user = new AvaloniaUser(viewModel);
                    manager.User = user;
                    viewModel.Initialize(manager, AvaloniaGui.UserAgentString);
                }
                else
                {
                    log.Error("GameInstanceManager was null at startup; UI will show an error.");
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await viewModel.ShowErrorDialogAsync(
                            "CKAN failed to initialize its game-instance manager. "
                            + "Please relaunch from the command line (`ckan gui`) "
                            + "and report this if it keeps happening.");
                    });
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d
                    && d.MainWindow?.DataContext is MainWindowViewModel vm)
                {
                    _ = vm.ShowErrorDialogAsync($"An unexpected error occurred:\n\n{ex.Message}");
                }
            }
            catch (Exception inner)
            {
                log.Error("Failed to display fatal-error dialog", inner);
            }
        }
    }
}
