using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution.Project.Properties;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.IO;
using Cake.Frosting;
using AltCover;
using AltCover.Cake;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
        => new CakeHost()
            .ConfigureServices(services =>
            {
                services.UseToolPath(new DirectoryPath(Environment.CurrentDirectory)
                    .GetParent()
                    .Combine("_build")
                    .Combine("tools"));
            })
            .InstallTool(new Uri("nuget:?package=altcover&version=9.0.1"))
            .InstallTool(new Uri("nuget:?package=altcover.api&version=9.0.1"))
            .InstallTool(new Uri("nuget:?package=altcover.cake&version=9.0.1"))
            .UseContext<BuildContext>()
            .UseLifetime<BuildLifetime>()
            .Run(args);
}

[TaskName("Default")]
[TaskDescription("Build ckan and netkan")]
[IsDependentOn(typeof(CkanTask))]
[IsDependentOn(typeof(NetkanTask))]
public sealed class DefaultTask : FrostingTask<BuildContext>;

[TaskName("Debug")]
[TaskDescription("Build ckan and netkan in Debug configuration")]
[IsDependentOn(typeof(DefaultTask))]
public sealed class DebugTask : FrostingTask<BuildContext>;

[TaskName("Release")]
[TaskDescription("Build ckan and netkan in Release configuration")]
[IsDependentOn(typeof(DefaultTask))]
public sealed class ReleaseTask : FrostingTask<BuildContext>;

[TaskName("Netkan")]
[TaskDescription("Build only netkan")]
[IsDependentOn(typeof(BuildTask))]
public sealed class NetkanTask : FrostingTask<BuildContext>;

[TaskName("Ckan")]
[TaskDescription("Build only ckan")]
[IsDependentOn(typeof(BuildTask))]
public sealed class CkanTask : FrostingTask<BuildContext>;

[TaskName("Restore")]
[TaskDescription("Intermediate - Download dependencies")]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(new DotNetRestoreSettings
        {
            WorkingDirectory = context.Paths.RootDirectory,
            PackagesDirectory = context.Paths.NugetDirectory,
            EnvironmentVariables = new Dictionary<string, string?> { { "Configuration", context.BuildConfiguration } }
        });
    }
}

[TaskName("Generate-GlobalAssemblyVersionInfo")]
[TaskDescription("Intermediate - Calculate the version strings for the assembly.")]
public sealed class GenerateGlobalAssemblyVersionInfoTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var metaDirectory = context.Paths.BuildDirectory.Combine("meta");
        context.CreateDirectory(metaDirectory);

        var version = context.GetVersion();

        context.CreateAssemblyInfo(
            metaDirectory.CombineWithFilePath("GlobalAssemblyVersionInfo.cs"), new AssemblyInfoSettings
            {
                Version = $"{version.Major}.{version.Minor}",
                FileVersion = version.HasMeta
                    ? $"{version.Major}.{version.Minor}.{version.Patch}{version.Meta}"
                    : $"{version.Major}.{version.Minor}.{version.Patch}",
                InformationalVersion = version.ToString(),
            });
    }
}

[TaskName("Build")]
[TaskDescription("Intermediate - Build everything")]
[IsDependentOn(typeof(RestoreTask))]
[IsDependentOn(typeof(GenerateGlobalAssemblyVersionInfoTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.Solution, new DotNetBuildSettings
        {
            Configuration = context.BuildConfiguration,
            Framework     = "net8.0",
            NoRestore     = true,
        });
    }
}

[TaskName("Test")]
[TaskDescription("Run tests after compilation.")]
[IsDependentOn(typeof(DefaultTask))]
[IsDependentOn(typeof(TestOnlyTask))]
public sealed class TestTask : FrostingTask<BuildContext>;

[TaskName("Test-UnitTests")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(TestUnitTestsOnlyTask))]
public sealed class TestUnitTestsTask : FrostingTask<BuildContext>;

[TaskName("Test+Only")]
[TaskDescription("Run tests without compiling.")]
[IsDependentOn(typeof(TestUnitTestsOnlyTask))]
public sealed class TestOnlyTask : FrostingTask<BuildContext>;

[TaskName("Test-UnitTests+Only")]
[TaskDescription("Intermediate - Run unit tests without compiling.")]
public sealed class TestUnitTestsOnlyTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var where  = context.Argument<string?>("where", null);
        var labels = context.Argument("labels", "Off");
        var nunitOutputDirectory = context.Paths.BuildDirectory
                                                .Combine("test")
                                                .Combine("nunit");
        context.CreateDirectory(nunitOutputDirectory);
        context.CreateDirectory(context.Paths.CoverageOutputDirectory);
        var dotNetFilter = where?.Replace("class=", "FullyQualifiedName=",
                                          StringComparison.CurrentCultureIgnoreCase)
                                 .Replace("category=", "TestCategory=",
                                          StringComparison.CurrentCultureIgnoreCase)
                                 .Replace("category!=", "TestCategory!=",
                                          StringComparison.CurrentCultureIgnoreCase)
                                 .Replace("namespace=", "FullyQualifiedName~",
                                          StringComparison.CurrentCultureIgnoreCase)
                                 .Replace("name=", "Name~",
                                          StringComparison.CurrentCultureIgnoreCase);

        var altcoverSettings = new CoverageSettings
        {
            PreparationPhase = new MyPrepareOptions(context),
            CollectionPhase  = new MyCollectOptions(context),
            Options          = new MyTestOptions(),
        };
        var testSettings = new DotNetTestSettings
        {
            Configuration    = context.BuildConfiguration,
            Framework        = "net8.0",
            NoRestore        = true,
            NoBuild          = true,
            NoLogo           = true,
            Filter           = dotNetFilter,
            ResultsDirectory = nunitOutputDirectory,
            Verbosity        = DotNetVerbosity.Minimal,
        };
        testSettings.ArgumentCustomization = altcoverSettings.Concatenate(testSettings.ArgumentCustomization);
        context.DotNetTest(context.Solution, testSettings);
    }
}

public class MyPrepareOptions(BuildContext context) : PrepareOptions
{
    public override TraceLevel Verbosity => TraceLevel.Info;

    public override IEnumerable<string> AssemblyFilter => [
        "Microsoft", "NUnit3", "testhost",
        "IndexRange", @"AltCover\.Monitor",
        "CKAN-ConsoleUI", @"CKAN\.Tests",
    ];

    public override IEnumerable<string> TypeFilter => [
        "System", "Microsoft",
    ];

    public override IEnumerable<string> PathFilter => [
        "_build",
    ];

    public override IEnumerable<string> AttributeFilter => [
        "ExcludeFromCodeCoverage",
    ];

    public override bool LocalSource => true;

    public override string Report => OutputPath("coverage.xml");

    private string OutputPath(string filename)
        => context.Paths.CoverageOutputFile(filename).FullPath;

    private readonly BuildContext context = context;
}

public class MyCollectOptions(BuildContext context) : CollectOptions
{
    public override TraceLevel Verbosity => TraceLevel.Info;
    public override string OutputFile    => OutputPath("output.xml");
    public override string Cobertura     => OutputPath("cobertura.xml");
    public override string LcovReport    => OutputPath("lcov.info");

    private string OutputPath(string filename)
        => context.Paths.CoverageOutputFile(filename).FullPath;

    private readonly BuildContext context = context;
}

public class MyTestOptions : TestOptions
{
    public override bool ForceDelete => true;
    public override bool FailFast    => true;
}

[TaskName("Version")]
[TaskDescription("Print the current CKAN version.")]
public sealed class VersionTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        using (context.NormalVerbosity())
        {
            context.Information(context.GetVersion().ToString());
        }
    }
}
