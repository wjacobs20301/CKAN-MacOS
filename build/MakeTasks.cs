using System;
using System.Collections.Generic;

using Cake.Common;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;

namespace Build;

[TaskName("osx-app")]
[TaskDescription("Build the macOS app bundle.")]
[IsDependentOn(typeof(CkanTask))]
public sealed class OsxAppTask() : MakeTask("macosx", "app")
{
    public override void Run(BuildContext context)
    {
        context.DotNetPublish(context.Paths.CmdlineProject.FullPath, new DotNetPublishSettings
        {
            Configuration  = context.BuildConfiguration,
            Framework      = "net8.0",
            Runtime        = "osx-arm64",
            SelfContained  = true,
        });
        context.DotNetPublish(context.Paths.CmdlineProject.FullPath, new DotNetPublishSettings
        {
            Configuration  = context.BuildConfiguration,
            Framework      = "net8.0",
            Runtime        = "osx-x64",
            SelfContained  = true,
        });
        base.Run(context);
    }
}

[TaskName("osx-dmg")]
[TaskDescription("Build the macOS dmg package.")]
[IsDependentOn(typeof(OsxAppTask))]
public sealed class OsxDmgTask() : MakeTask("macosx");

[TaskName("osx-clean")]
[TaskDescription("Clean the output directory of the macOS package.")]
public sealed class OsxCleanTask() : MakeTask("macosx", "clean");

public abstract class MakeTask(string location, ProcessArgumentBuilder? args = null) : FrostingTask<BuildContext>
{
    private string Location { get; } = location;
    private ProcessArgumentBuilder Args { get; } = args ?? "";

    public override void Run(BuildContext context)
    {
        var exitCode = context.StartProcess("make", new ProcessSettings() {
            WorkingDirectory = context.Paths.RootDirectory.Combine(Location),
            Arguments = Args,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                { "CONFIGURATION", context.BuildConfiguration },
            }
        });
        if (exitCode != 0)
        {
            throw new Exception("Make failed with exit code: " + exitCode);
        }
    }
}
