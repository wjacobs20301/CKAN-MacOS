using System;

using Avalonia.Threading;

using CKAN.GUI.Avalonia.ViewModels;

namespace CKAN.GUI.Avalonia
{
    /// <summary>
    /// The Avalonia GUI implementation of the IUser interface.
    /// Routes Core callbacks to ViewModel properties on the UI thread.
    /// </summary>
    public class AvaloniaUser : IUser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(AvaloniaUser));

        public AvaloniaUser(MainWindowViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        private readonly MainWindowViewModel mainViewModel;

        public bool Headless => false;

        public bool RaiseYesNoDialog(string question)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                // IUser.RaiseYesNoDialog must block and return an answer, but we
                // are on the UI thread; blocking here would deadlock the dialog.
                // This indicates a bug in the caller — log and default to "no".
                log.Error("RaiseYesNoDialog called on UI thread; returning false to avoid deadlock.\n"
                          + Environment.StackTrace);
                return false;
            }
            return Dispatcher.UIThread.InvokeAsync(
                () => mainViewModel.ShowYesNoDialogAsync(question)
            ).GetAwaiter().GetResult();
        }

        public int RaiseSelectionDialog(string message, params object[] args)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                log.Error("RaiseSelectionDialog called on UI thread; returning -1 to avoid deadlock.\n"
                          + Environment.StackTrace);
                return -1;
            }
            return Dispatcher.UIThread.InvokeAsync(
                () => mainViewModel.ShowSelectionDialogAsync(message, args)
            ).GetAwaiter().GetResult();
        }

        public void RaiseError(string message, params object[] args)
        {
            var fullMsg = SafeFormat(message, args);
            log.Error(fullMsg);
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    mainViewModel.StatusMessage = fullMsg;
                    await mainViewModel.ShowErrorDialogAsync(fullMsg);
                }
                catch (Exception ex)
                {
                    log.Error("Failed to show error dialog", ex);
                }
            });
        }

        public void RaiseProgress(string message, int percent)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainViewModel.StatusMessage = message.Replace("\r\n", " ")
                                                     .Replace("\n",   " ");
                mainViewModel.ProgressPercent = Math.Max(0, Math.Min(100, percent));
                mainViewModel.IsProgressVisible = true;
                mainViewModel.WaitViewModel.SetProgress(message, percent);
            });
        }

        public void RaiseProgress(ByteRateCounter rateCounter)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainViewModel.ProgressPercent = Math.Max(0, Math.Min(100, rateCounter.Percent));
                mainViewModel.IsProgressVisible = true;
                mainViewModel.WaitViewModel.SetProgress(rateCounter);
            });
        }

        public void RaiseMessage(string message, params object[] args)
        {
            var fullMsg = SafeFormat(message, args);
            Dispatcher.UIThread.Post(() =>
            {
                mainViewModel.StatusMessage = fullMsg.Replace("\r\n", " ")
                                                     .Replace("\n",   " ");
                mainViewModel.WaitViewModel.AddLogMessage(fullMsg);
            });
        }

        private static string SafeFormat(string message, object[] args)
        {
            try
            {
                return args is { Length: > 0 } ? string.Format(message, args) : message;
            }
            catch (FormatException)
            {
                return message;
            }
        }

    }
}
