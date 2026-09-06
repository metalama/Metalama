// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This file is compiled into Metalama.Backstage and into Metalama.Framework.CompilerExtensions. See the remarks of
// ProcessKindDetector for the reason. The METALAMA_BACKSTAGE compilation symbol selects the namespace, so that the
// two assemblies do not declare a type of the same full name: ResourceExtractor extracts Metalama.Backstage and
// loads it into the process that already contains Metalama.Framework.CompilerExtensions.
#if METALAMA_BACKSTAGE
namespace Metalama.Backstage.Diagnostics;
#else
namespace Metalama.Framework.CompilerExtensions;
#endif

// ReSharper disable UnusedMember.Global

/// <summary>
/// Enumerates the kinds of process that Metalama runs in.
/// </summary>
/// <remarks>
/// The members are named in <c>diagnostics.json</c> and in telemetry, so a member is not removed when the
/// corresponding host leaves the supported set. Removing one would make an existing configuration file invalid.
/// </remarks>
public enum ProcessKind
{
    /// <summary>
    /// A process that <see cref="ProcessKindDetector"/> does not classify.
    /// </summary>
    Other,

    /// <summary>
    /// <c>Metalama.Compiler</c> itself.
    /// </summary>
    Compiler,

    /// <summary>
    /// <c>devenv.exe</c>, i.e. the UI process of Visual Studio.
    /// </summary>
    DevEnv,

    /// <summary>
    /// The Roslyn analysis process of Visual Studio.
    /// </summary>
    RoslynCodeAnalysisService,

    /// <summary>
    /// The process running Roslyn under Rider.
    /// </summary>
    Rider,

    /// <summary>
    /// The <c>VisualStudio</c> process of Visual Studio for Mac.
    /// </summary>
    /// <remarks>
    /// No process is classified as this kind since Visual Studio for Mac left the supported set. The member is
    /// kept because <c>diagnostics.json</c> names it.
    /// </remarks>
    VisualStudioMac,

    /// <summary>
    /// <c>Metalama.Backstage.Worker</c>.
    /// </summary>
    BackstageWorker,

    /// <summary>
    /// <c>Metalama.Backstage.Desktop.Windows</c>.
    /// </summary>
    BackstageDesktopWindows,

    /// <summary>
    /// The <c>metalama</c> global command line tool.
    /// </summary>
    DotNetTool,

    /// <summary>
    /// The .NET test host.
    /// </summary>
    TestHost,

    /// <summary>
    /// The OmniSharp background process of Visual Studio Code or other editors.
    /// </summary>
    OmniSharp,

    /// <summary>
    /// The Code Lens background process of Visual Studio.
    /// </summary>
    CodeLensService,

    /// <summary>
    /// A test runner process of Rider or ReSharper.
    /// </summary>
    ResharperTestRunner,

    /// <summary>
    /// The LinqPad driver proxy process.
    /// </summary>
    LinqPad,

    /// <summary>
    /// The language server of the Visual Studio Code C# Dev Kit.
    /// </summary>
    LanguageServer,

    /// <summary>
    /// An MSBuild node process.
    /// </summary>
    MsBuild,

    /// <summary>
    /// The <c>dotnet format</c> command.
    /// </summary>
    Format
}
