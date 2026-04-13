using System;
using System.Diagnostics.CodeAnalysis;

using Avalonia;

namespace CKAN.GUI.Avalonia
{
    [ExcludeFromCodeCoverage]
    public static class AvaloniaGui
    {
        private static GameInstanceManager? _managerInstance;
        private static string? _userAgentString;
        private static bool _initialized;

        internal static GameInstanceManager? ManagerInstance => _managerInstance;
        internal static string? UserAgentString => _userAgentString;

        public static void Main(string[] args)
        {
            Main_(args, null);
        }

        public static void Main_(string[]             args,
                                 string?              userAgent,
                                 GameInstanceManager? manager = null)
        {
            if (_initialized)
            {
                throw new InvalidOperationException(
                    "AvaloniaGui.Main_ can only be called once per process.");
            }
            _initialized     = true;
            _managerInstance = manager;
            _userAgentString = userAgent;

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
