// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;

namespace Metalama.Framework.CompilerExtensions;

public static class ProcessKindHelper
{
    public static ProcessKind CurrentProcessKind { get; } = GetProcessKind();

    private static ProcessKind GetProcessKind()
    {
        // Note that the same logic is duplicated in Metalama.Backstage.Utilities.ProcessUtilities and cannot 
        // be shared. Any change here must be done there too.

        switch ( Process.GetCurrentProcess().ProcessName.ToLowerInvariant() )
        {
            case "devenv":
                return ProcessKind.DevEnv;

            case "servicehub.roslyncodeanalysisservice":
            case "servicehub.roslyncodeanalysisservices":
                return ProcessKind.RoslynCodeAnalysisService;

            case "csc":
            case "vbcscompiler":
                return ProcessKind.Compiler;

            case "dotnet":
                var commandLine = Environment.CommandLine.ToLowerInvariant();

#pragma warning disable CA1307
                if ( commandLine.Contains( "jetbrains.resharper.roslyn.worker" ) ||
                     commandLine.Contains( "jetbrains.roslyn.worker" ) )
                {
                    return ProcessKind.Rider;
                }
                else if ( commandLine.Contains( "vbcscompiler.dll" ) || commandLine.Contains( "csc.dll" ) )
                {
                    return ProcessKind.Compiler;
                }
                else
                {
                    return ProcessKind.Other;
                }
#pragma warning restore CA1307

            default:
                return ProcessKind.Other;
        }
    }
}

public enum ProcessKind
{
    Other,
    Compiler,
    DevEnv,
    RoslynCodeAnalysisService,
    Rider
}