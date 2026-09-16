// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using System.Diagnostics;

namespace Metalama.Backstage.Tools;

/// <summary>
/// Describes one of the tool applications that the product starts as a separate process. The assembly name of a tool
/// depends on the product: see <see cref="GetAssemblyName"/>.
/// </summary>
public sealed class BackstageTool
{
    private BackstageTool( string suffix, bool isExe, ProcessWindowStyle windowStyle, bool useShellExecute )
    {
        this.Suffix = suffix;
        this.IsExe = isExe;
        this.WindowStyle = windowStyle;
        this.UseShellExecute = useShellExecute;
    }

    // We use have to use shell execute so that we don't inherit the environment variable of the parent process, which causes problems in case of the VS processes.

    /// <summary>
    /// Gets the worker, which hosts the setup web server and uploads the telemetry.
    /// </summary>
    public static BackstageTool Worker { get; } = new( "Worker", false, ProcessWindowStyle.Hidden, true );

    /// <summary>
    /// Gets the desktop notifier of Windows, which shows the toast notifications.
    /// </summary>
    public static BackstageTool DesktopWindows { get; } = new( "Desktop.Windows", true, ProcessWindowStyle.Normal, true );

    /// <summary>
    /// Gets the suffix of the assembly name of the tool, for instance <c>Worker</c>.
    /// </summary>
    public string Suffix { get; }

    /// <summary>
    /// Gets the assembly name of the tool for a product, which is <see cref="ProductProfile.ToolAssemblyNamePrefix"/>
    /// followed by <see cref="Suffix"/>, for instance <c>Metalama.Backstage.Worker</c>. It names the executable file,
    /// the embedded resource that holds it, and the directory to which it is extracted.
    /// </summary>
    public string GetAssemblyName( ProductProfile productProfile ) => $"{productProfile.ToolAssemblyNamePrefix}.{this.Suffix}";

    internal bool UseShellExecute { get; }

    internal bool IsExe { get; }

    internal ProcessWindowStyle WindowStyle { get; }

    public override string ToString() => this.Suffix;
}
