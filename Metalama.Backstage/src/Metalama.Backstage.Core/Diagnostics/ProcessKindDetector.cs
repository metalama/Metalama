// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

// This file is compiled into Metalama.Backstage and into Metalama.Framework.CompilerExtensions. The second assembly
// can reference nothing, because Metalama.Backstage is one of the assemblies that it embeds and extracts, so the
// classification is shared as source rather than through an assembly reference. The METALAMA_BACKSTAGE compilation
// symbol selects the namespace, so that the two assemblies do not declare a type of the same full name:
// ResourceExtractor loads Metalama.Backstage into the process that already contains
// Metalama.Framework.CompilerExtensions.
#if METALAMA_BACKSTAGE
namespace Metalama.Backstage.Diagnostics;
#else
namespace Metalama.Framework.CompilerExtensions;
#endif

/// <summary>
/// Classifies a process into a <see cref="ProcessKind"/> from its name and its command line.
/// </summary>
/// <remarks>
/// <para>
/// The classification is a pure function of the two parameters, so that a test can exercise every arm of the table
/// without the corresponding process existing. The callers cache the result for the current process:
/// <c>Metalama.Backstage.Utilities.ProcessUtilities.ProcessKind</c> and
/// <c>Metalama.Framework.CompilerExtensions.ProcessKindHelper.CurrentProcessKind</c> are both computed once, in a
/// static property initializer.
/// </para>
/// <para>
/// Every arm below names the host of PB-2027.0 that it serves. The platform baseline is defined in
/// <c>Metalama.Framework/docs/platform-support.md</c>, whose verification checklist requires the process name of
/// the Roslyn analysis process and of the C# Dev Kit language server to be measured again before each release.
/// </para>
/// <para>
/// One arm was removed rather than annotated. The process name <c>visualstudio</c> was classified as
/// <see cref="ProcessKind.VisualStudioMac"/> until 2027.0. Visual Studio for Mac is sunset and PB-2027.0 does not
/// include it, so no process is classified as that kind any more.
/// </para>
/// </remarks>
public static class ProcessKindDetector
{
    /// <summary>
    /// Classifies a process from its name and its command line.
    /// </summary>
    /// <param name="processName">
    /// The process name, without the directory and without the file name extension, as
    /// <see cref="System.Diagnostics.Process.ProcessName"/> gives it. The comparison is case insensitive.
    /// </param>
    /// <param name="commandLine">
    /// The command line of the process, as <see cref="Environment.CommandLine"/> gives it. Several arms of the
    /// table need it, because the host runs under the <c>dotnet</c> or <c>ServiceHub.Host</c> process name and is
    /// identified by the assembly that the command line names.
    /// </param>
    /// <returns>The kind of the process, or <see cref="ProcessKind.Other"/> when no arm matches.</returns>
    public static ProcessKind GetProcessKind( string processName, string commandLine )
    {
        var normalizedProcessName = processName.ToLowerInvariant();
        var normalizedCommandLine = commandLine.ToLowerInvariant();

#pragma warning disable CA1307

        switch ( normalizedProcessName )
        {
            // The user interface process of Visual Studio 2026.
            case "devenv":
                return ProcessKind.DevEnv;

            // The Roslyn analysis process. Visual Studio 2022 names it ServiceHub.RoslynCodeAnalysisService and
            // Visual Studio 2026 names it DevHub. See issue #1463 for the rename.
            case "servicehub.roslyncodeanalysisservice":
            case "servicehub.roslyncodeanalysisservices":
            case "devhub":
                return ProcessKind.RoslynCodeAnalysisService;

            // The Code Lens background process of Visual Studio. It shares the ServiceHub.Host process name with
            // every other ServiceHub service, so the command line is what distinguishes it.
            case "servicehub.host":
                return normalizedCommandLine.Contains( "$codelensservice$" ) ? ProcessKind.CodeLensService : ProcessKind.Other;

            // The C# compiler, and the compiler server, of the .NET Framework build.
            case "csc":
            case "vbcscompiler":
                return ProcessKind.Compiler;

            // The test runner of Rider and of ReSharper.
            case "resharpertestrunner":
            case "resharpertestrunner64":
                return ProcessKind.ResharperTestRunner;

            // The language server of the Visual Studio Code C# Dev Kit. The second name is the one that earlier
            // versions of the C# extension used.
            case "microsoft.codeanalysis.languageserver":
            case "microsoft.visualstudio.code.languageserver":
                return ProcessKind.LanguageServer;

            // An MSBuild node of the .NET Framework build.
            case "msbuild":
                return ProcessKind.MsBuild;

            // The test host of the .NET test platform.
            case "testhost":
                return ProcessKind.TestHost;

            // Several hosts run as an assembly under the dotnet process name, and the command line names the
            // assembly that identifies them.
            case "dotnet":
                if ( normalizedCommandLine.Contains( "jetbrains.resharper.roslyn.worker" ) ||
                     normalizedCommandLine.Contains( "jetbrains.roslyn.worker" ) )
                {
                    // The Roslyn worker of the Rider backend.
                    return ProcessKind.Rider;
                }
                else if ( normalizedCommandLine.Contains( "vbcscompiler.dll" ) || normalizedCommandLine.Contains( "csc.dll" ) )
                {
                    // The C# compiler, and the compiler server, of the .NET build.
                    return ProcessKind.Compiler;
                }
                else if ( normalizedCommandLine.Contains( "languageserver.dll" ) )
                {
                    // The language server of the Visual Studio Code C# Dev Kit, started as an assembly.
                    return ProcessKind.LanguageServer;
                }
                else if ( normalizedCommandLine.Contains( "omnisharp.dll" ) )
                {
                    // OmniSharp. PB-2027.0 does not include it, and the arm is kept because a support report of a
                    // user of an earlier Visual Studio Code extension must still name the host.
                    return ProcessKind.OmniSharp;
                }
                else if ( normalizedCommandLine.Contains( "resharpertestrunner.dll" ) )
                {
                    // The test runner of Rider and of ReSharper, started as an assembly.
                    return ProcessKind.ResharperTestRunner;
                }
                else if ( normalizedCommandLine.Contains( "msbuild.dll" ) )
                {
                    // An MSBuild node of the .NET build.
                    return ProcessKind.MsBuild;
                }
                else if ( normalizedCommandLine.Contains( "dotnet-format.dll" ) )
                {
                    // The dotnet format command.
                    return ProcessKind.Format;
                }
                else
                {
                    return ProcessKind.Other;
                }

            default:
                // The LinqPad driver proxy process. Its name carries the LinqPad version, so it is matched by
                // prefix and not by an exact name.
                if ( normalizedProcessName.StartsWith( "linqpad", StringComparison.Ordinal ) )
                {
                    return ProcessKind.LinqPad;
                }
                else
                {
                    return ProcessKind.Other;
                }
        }

#pragma warning restore CA1307
    }
}
