// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
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
        new KillableProcessSpec( "jetbrains.resharper.roslyn.worker", KillableModuleKind.DotNet, false, false, "Rider/Resharper" ),
        new KillableProcessSpec( "jetbrains.roslyn.worker", KillableModuleKind.DotNet, false, false, "Rider/Resharper" ),
        new KillableProcessSpec( "omnisharp", KillableModuleKind.DotNet, false, false, "Visual Studio Code / Omnisharp" ) );

    protected ILogger Logger { get; }

    protected ProcessManagerBase( IServiceProvider serviceProvider )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "ProcessManager" );
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

    protected bool? ReferencesMetalama( Process process, IReadOnlyList<string> modules )
    {
#pragma warning disable CA1307
        if ( modules.Any( m => Path.GetFileNameWithoutExtension( m ).ToLowerInvariant().Contains( "metalama" ) ) )
#pragma warning restore CA1307
        {
            return true;
        }

        // TODO: Determines if the process references Metalama. 
        // We cannot do it by looking at the modules because .NET assemblies are not always exposed as modules.

        this.Logger.Trace?.Log( $"Cannot determine if process '{process.ProcessName}' ({process.Id}) uses Metalama." );

        return null;
    }

    protected IEnumerable<KillableProcess> GetDotNetProcesses( ImmutableArray<KillableProcessSpec> processSpecs )
    {
        foreach ( var process in Process.GetProcessesByName( "dotnet" ) )
        {
            if ( !this.TryGetModulePaths( process, out var modules ) )
            {
                continue;
            }

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

                    if ( this.ReferencesMetalama( process, modules ) == false )
                    {
                        this.Logger.Trace?.Log( $"Do not kill '{process.ProcessName}' '{mainModule}' ({process.Id}) because it does not contain Metalama." );
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
                if ( this.ReferencesMetalama( process, modules ) != false )
                {
                    this.Logger.Trace?.Log(
                        $"Do not kill '{process.ProcessName}' ({process.Id}) even if it references Metalama because it is not a known process." );
                }
            }
        }
    }

    protected abstract IEnumerable<KillableProcess> GetProcesses( ImmutableArray<KillableProcessSpec> processNames );

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
                    $"The process {process.Process.Id} ({process.Spec.DisplayName ?? process.Spec.Name}), if it uses Metalama, must be closed manually." );
            }
        }
    }
}