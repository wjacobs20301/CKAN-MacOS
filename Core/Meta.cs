using System.Linq;
using System.Reflection;

using CKAN.Versioning;

namespace CKAN
{
    public static class Meta
    {
        /// <summary>
        /// Programmatically generate the string "CKAN" from the assembly info attributes,
        /// so we don't have to embed that string in many places
        /// </summary>
        /// <returns>"CKAN"</returns>
        public static readonly string ProductName =
            Assembly.GetExecutingAssembly()
                    .GetAssemblyAttribute<AssemblyProductAttribute>()
                    .Product;

        public static readonly ModuleVersion ReleaseVersion = new ModuleVersion(GetVersion());

        /// <summary>
        /// The highest CKAN metadata spec version this client can parse.
        /// This is decoupled from the fork's own release version (which starts
        /// at 1.0.0) because `spec_version` in module metadata tracks upstream
        /// CKAN's client version, not ours. Bump this when picking up code
        /// changes from upstream that add support for a newer spec.
        /// </summary>
        public static readonly ModuleVersion SpecVersion =
            new ModuleVersion("v1.36");

        public static readonly bool IsNetKAN =
            Assembly.GetExecutingAssembly()
                    .GetAssemblyAttribute<AssemblyTitleAttribute>()
                    .Title.Contains("NetKAN");

        public static string GetVersion(VersionFormat format = VersionFormat.Normal)
            => "v" + (format switch
            {
                VersionFormat.Full =>
                    Assembly.GetExecutingAssembly()
                            .GetAssemblyAttribute<AssemblyInformationalVersionAttribute>()
                            .InformationalVersion,

                VersionFormat.Normal or _ =>
                    Assembly.GetExecutingAssembly()
                            .GetAssemblyAttribute<AssemblyFileVersionAttribute>()
                            .Version,
            });

        private static T GetAssemblyAttribute<T>(this Assembly assembly)
            => (T)assembly.GetCustomAttributes(typeof(T), false)
                          .First();
    }
}
