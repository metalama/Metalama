// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Diagnostics;

namespace Metalama.Backstage.Tools;

public sealed class BackstageTool
{
    private BackstageTool( string name, bool isExe, ProcessWindowStyle windowStyle, bool useShellExecute )
    {
        this.Name = name;
        this.IsExe = isExe;
        this.WindowStyle = windowStyle;
        this.UseShellExecute = useShellExecute;
    }

    // We use have to use shell execute so that we don't inherit the environment variable of the parent process, which causes problems in case of the VS processes.
    public static BackstageTool Worker { get; } = new( "Metalama.Backstage.Worker", false, ProcessWindowStyle.Hidden, true );

    public static BackstageTool DesktopWindows { get; } = new( "Metalama.Backstage.Desktop.Windows", true, ProcessWindowStyle.Normal, true );

    public string Name { get; }

    internal bool UseShellExecute { get; }

    internal bool IsExe { get; }

    internal ProcessWindowStyle WindowStyle { get; }

    public override string ToString() => this.Name;
}