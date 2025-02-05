// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Diagnostics;

public enum ProcessKind
{
    /// <summary>
    /// Not a logged process (used for tests).
    /// </summary>
    /// 
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
    /// <c>VisualStudio</c> process on Mac.
    /// </summary>
    VisualStudioMac,

    // Resharper disable once UnusedMember.Global

    /// <summary>
    /// <c>Metalama.Backstage.Worker</c>.
    /// </summary>
    BackstageWorker,

    /// <summary>
    /// <c>Metalama.Backstage.Desktop.Windows</c>.
    /// </summary>
    BackstageDesktopWindows,

    /// <summary>
    /// The <c>metalama</c> global CLI too.
    /// </summary>
    [PublicAPI]
    DotNetTool,

    /// <summary>
    /// The .Net test host.
    /// </summary>
    [PublicAPI]
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
    /// A test runner process of Rider or Resharper.
    /// </summary>
    ResharperTestRunner,

    /// <summary>
    /// LinqPad driver proxy process.
    /// </summary>
    LinqPad,

    /// <summary>
    /// Language server used by VS Code C# Dev Kit.
    /// </summary>
    LanguageServer
}