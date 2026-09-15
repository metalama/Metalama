// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Backstage.Maintenance;

internal abstract partial class ProcessManagerBase : IProcessManager
{
    private static readonly ImmutableArray<KillableProcessSpec> _processesToKill = ImmutableArray.Create(
        new KillableProcessSpec( "VBCSCompiler", KillableModuleKind.Both, true, true ),
        new KillableProcessSpec( "MSBuild", KillableModuleKind.Both, false, true ),
        new KillableProcessSpec( "servicehub.roslyncodeanalysisservice", KillableModuleKind.Both, false, false, "Visual Studio" ),

        // Visual Studio 2026 runs the Roslyn analysis process under this name instead. See issue #1463. Like the
        // process that it replaces, it is reported to the user rather than stopped, because Visual Studio owns it.
        // Ending a child process of the integrated development environment leaves that environment in an
        // inconsistent state, and Visual Studio starts the analysis process again as soon as a document is opened,
        // which would lock the files again before the clean-up finished.
        new KillableProcessSpec( "devhub", KillableModuleKind.Both, false, false, "Visual Studio" ),
        new KillableProcessSpec( "jetbrains.resharper.roslyn.worker", KillableModuleKind.DotNet, false, false, "Rider/Resharper" ),
        new KillableProcessSpec( "jetbrains.roslyn.worker", KillableModuleKind.DotNet, false, false, "Rider/Resharper" ),
        new KillableProcessSpec( "omnisharp", KillableModuleKind.DotNet, false, false, "Visual Studio Code / Omnisharp" ),

        // The language server of the Visual Studio Code C# Dev Kit. It runs either as its own executable or as an
        // assembly under 'dotnet', so it is matched as both kinds of module. Like OmniSharp above, which is the
        // language server that preceded it, it is reported to the user rather than stopped, because Visual Studio
        // Code owns it and starts it again.
        new KillableProcessSpec(
            "microsoft.codeanalysis.languageserver",
            KillableModuleKind.Both,
            false,
            false,
            "Visual Studio Code / C# Dev Kit" ),

        // The Backstage Worker runs under 'dotnet' (hosting Metalama.Backstage.Worker.dll), so it is matched as a DotNet module.
        new KillableProcessSpec( "Metalama.Backstage.Worker", KillableModuleKind.DotNet, false, true ),

        // The Backstage Desktop tray app is a standalone '.exe'.
        new KillableProcessSpec( "Metalama.Backstage.Desktop.Windows", KillableModuleKind.StandaloneProcess, false, true ) );

    protected ILogger Logger { get; }

    /// <summary>
    /// The profile of the product, which gives the prefix of the assemblies that identify a process as using the
    /// product.
    /// </summary>
    private readonly ProductProfile _productProfile;

    protected ProcessManagerBase( IServiceProvider serviceProvider )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "ProcessManager" );
        this._productProfile = serviceProvider.GetRequiredBackstageService<ProductProfile>();
    }

    protected virtual bool TryGetModulePaths( Process process, [NotNullWhen( true )] out List<string>? modules )
    {
        modules = [];

        try
        {
            foreach ( ProcessModule module in process.Modules )
            {
                if ( module.FileName != null! )
                {
                    modules.Add( module.FileName );
                }
            }

            return true;
        }
        catch ( Exception e )
        {
            if ( !process.HasExited )
            {
                this.Logger.Warning?.Log( $"Cannot enumerate the modules of '{process.Id}': {e.Message}." );
            }

            return false;
        }
    }

    /// <summary>
    /// Determines whether a process has loaded the product, from the names of its modules.
    /// </summary>
    /// <returns><c>true</c> if a module of the process belongs to the product, or <c>null</c> when it cannot be determined.</returns>
    protected bool? ReferencesProduct( Process process, IReadOnlyList<string> modules )
    {
        var assemblyNamePrefix = this._productProfile.AssemblyNamePrefix;

        if ( modules.Any( m => Path.GetFileNameWithoutExtension( m ).StartsWith( assemblyNamePrefix, StringComparison.OrdinalIgnoreCase ) ) )
        {
            return true;
        }

        // TODO: Determines if the process references the product.
        // We cannot do it by looking at the modules because .NET assemblies are not always exposed as modules.

        this.Logger.Trace?.Log( $"Cannot determine if process '{process.ProcessName}' ({process.Id}) uses {this._productProfile.Name}." );

        return null;
    }

    protected IEnumerable<KillableProcess> GetDotNetProcesses( ImmutableArray<KillableProcessSpec> processSpecs )
    {
        var dotnetProcesses = Process.GetProcessesByName( "dotnet" );

        this.Logger.Trace?.Log( $"Found {dotnetProcesses.Length} 'dotnet' processes." );

        foreach ( var process in dotnetProcesses )
        {
            if ( !this.TryGetModulePaths( process, out var modules ) )
            {
                this.Logger.Trace?.Log( $"Cannot get module paths for process {process.Id}." );

                continue;
            }

            this.Logger.Trace?.Log( $"Process {process.Id} modules: {string.Join( ", ", modules )}." );

            var moduleFileNames = modules.Select( s => Path.GetFileNameWithoutExtension( s ).ToLowerInvariant() ).ToList();

            var hasMatch = false;

            foreach ( var processSpec in processSpecs )
            {
                if ( !processSpec.IsDotNet )
                {
                    continue;
                }

                var moduleIndex = moduleFileNames.IndexOf( processSpec.Name.ToLowerInvariant() );

                if ( moduleIndex >= 0 )
                {
                    var mainModule = modules[moduleIndex];

                    if ( this.ReferencesProduct( process, modules ) == false )
                    {
                        this.Logger.Trace?.Log( $"Do not kill '{process.ProcessName}' '{mainModule}' ({process.Id}) because it does not contain {this._productProfile.Name}." );
                    }
                    else
                    {
                        this.Logger.Trace?.Log( $"Process '{process.ProcessName}' '{mainModule}' ({process.Id}) should be killed." );

                        yield return new KillableProcess( process, this.Logger, mainModule, processSpec );

                        hasMatch = true;
                    }

                    break;
                }
            }

            if ( !hasMatch )
            {
                if ( this.ReferencesProduct( process, modules ) != false )
                {
                    this.Logger.Trace?.Log(
                        $"Do not kill '{process.ProcessName}' ({process.Id}) even if it references {this._productProfile.Name} because it is not a known process." );
                }
            }
        }
    }

    /// <summary>
    /// Gets the processes that run as their own executable and that match one of <paramref name="processSpecs"/>.
    /// </summary>
    /// <remarks>
    /// The enumeration is performed on every operating system, and not on Windows alone, because the language
    /// server of the Visual Studio Code C# Dev Kit runs as its own executable on Linux and on macOS as well. The
    /// comparison of the process name is case insensitive, which is what <see cref="Process.GetProcessesByName(string)"/>
    /// performs on every platform.
    /// </remarks>
#pragma warning disable CA1307
    protected IEnumerable<KillableProcess> GetStandaloneProcesses( ImmutableArray<KillableProcessSpec> processSpecs )
    {
        foreach ( var processSpec in processSpecs.Where( p => p.IsStandaloneProcess ) )
        {
            foreach ( var process in Process.GetProcessesByName( processSpec.Name.ToLowerInvariant() ) )
            {
                if ( !this.TryGetModulePaths( process, out var modules ) )
                {
                    continue;
                }

                if ( this.ReferencesProduct( process, modules ) == false )
                {
                    this.Logger.Trace?.Log( $"Do not kill '{process.ProcessName}' ({process.Id}) because it does not contain {this._productProfile.Name}." );
                }

                yield return new KillableProcess( process, this.Logger, null, processSpec );
            }
        }
    }
#pragma warning restore CA1307

    /// <summary>
    /// Gets the processes that match one of <paramref name="processSpecs"/>, whether they run as an assembly under
    /// the <c>dotnet</c> process name or as their own executable.
    /// </summary>
    protected IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processSpecs )
        => this.GetDotNetProcesses( processSpecs ).Concat( this.GetStandaloneProcesses( processSpecs ) );

    public virtual void KillCompilerProcesses( bool shouldEmitWarnings )
    {
        foreach ( var process in this.GetProcesses( _processesToKill ) )
        {
            if ( process.Spec.CanShutdownOrKill )
            {
                process.ShutdownOrKill();
            }
            else if ( shouldEmitWarnings )
            {
                this.Logger.Warning?.Log(
                    $"The process {process.Process.Id} ({process.Spec.DisplayName ?? process.Spec.Name}), if it uses {this._productProfile.Name}, must be closed manually." );
            }
        }
    }
}