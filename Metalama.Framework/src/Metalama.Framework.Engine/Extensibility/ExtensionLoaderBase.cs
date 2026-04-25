// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Extensibility;

[PublicAPI]
public class ExtensionLoaderBase
{
    private readonly ILogger _logger;

    public ExtensionLoaderBase( GlobalServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ExtensionLoaderBase) );
    }

    public IEnumerable<string> GetExtensionAssemblyPaths( IEnumerable<TargetedAssemblyReference> assemblyReferences )
    {
        var targetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net8.0";

        this._logger.Trace?.Log( $"Getting extension assemblies for target framework '{targetFramework}'." );

        return assemblyReferences
            .Where( a => a.SatisfiesCurrentProcess )
            .Select( a => a.Path.AssertNotNull() );
    }

    internal IEnumerable<ExportExtensionAttribute> DiscoverExtensionTypes(
        CompileTimeDomain domain,
        ExtensionKinds extensionKinds,
        IReadOnlyCollection<TargetedAssemblyReference> assemblyReferences,
        bool avoidLockingAssemblies,
        IDiagnosticAdder diagnosticAdder )
    {
        this._logger.Trace?.Log( $"Discovering extension types of kind '{extensionKinds}' in assemblies: {string.Join( ", ", assemblyReferences )}." );

        // First load assemblies, because we don't know their order and dependencies.
        var assemblies = this.LoadExtensionAssemblies( domain, assemblyReferences, avoidLockingAssemblies, diagnosticAdder );

        // Now we can load the types.
        return this.DiscoverExtensionTypes( extensionKinds, assemblies, diagnosticAdder );
    }

    private IEnumerable<Assembly> LoadExtensionAssemblies(
        CompileTimeDomain domain,
        IEnumerable<TargetedAssemblyReference> assemblyReferences,
        bool avoidLockingAssemblies,
        IDiagnosticAdder diagnosticAdder )
    {
        // It is essential to materialize the query into a list, otherwise assemblies are not loaded if the caller does not evaluate the query.

        var assemblies = new List<Assembly>();

        foreach ( var path in this.GetExtensionAssemblyPaths( assemblyReferences ) )
        {
            try
            {
                this._logger.Trace?.Log( $"Loading extension assembly '{path}'." );

                var assembly = domain.LoadAssembly( path, null, new LoadAssemblyOptions() { IsShared = true, AvoidLocking = avoidLockingAssemblies } );
                assemblies.Add( assembly );
            }
            catch ( Exception e )
            {
                diagnosticAdder.Report( GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.CreateRoslynDiagnostic( null, (path, e.Message) ) );
                this._logger.Error?.Log( $"Cannot load extension assembly '{path}': {e.Message}" );
            }
        }

        return assemblies;
    }

    public IEnumerable<ExportExtensionAttribute> DiscoverExtensionTypes(
        CompileTimeDomain domain,
        ExtensionKinds extensionKinds,
        IEnumerable<string> assemblyPaths,
        IDiagnosticAdder diagnosticAdder )
    {
        var assemblies = new List<Assembly>();

        foreach ( var path in assemblyPaths )
        {
            try
            {
                this._logger.Trace?.Log( $"Loading extension assembly '{path}'." );

                var assembly = domain.LoadAssembly( path, null, LoadAssemblyOptions.Shared );
                assemblies.Add( assembly );
            }
            catch ( Exception e )
            {
                diagnosticAdder.Report( GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.CreateRoslynDiagnostic( null, (path, e.Message) ) );
                this._logger.Error?.Log( $"Cannot load extension assembly '{path}': {e.Message}" );
            }
        }

        return this.DiscoverExtensionTypes( extensionKinds, assemblies, diagnosticAdder );
    }

    private IEnumerable<ExportExtensionAttribute> DiscoverExtensionTypes(
        ExtensionKinds extensionKinds,
        IEnumerable<Assembly> assemblies,
        IDiagnosticAdder diagnosticAdder )
    {
        var extensionTypes = new List<ExportExtensionAttribute>();

        foreach ( var assembly in assemblies )
        {
            try
            {
                this._logger.Trace?.Log( $"Discovering ExportExtensionAttribute with kind '{extensionKinds}' in assembly '{assembly.FullName}'." );

                extensionTypes.AddRange(
                    assembly.GetCustomAttributes<ExportExtensionAttribute>()
                        .Where( attribute => (attribute.ExtensionKinds & extensionKinds) != 0 ) );
            }
            catch ( Exception e )
            {
                diagnosticAdder.Report(
                    GeneralDiagnosticDescriptors.CannotLoadExtensionAssembly.CreateRoslynDiagnostic(
                        null,
                        (assembly.FullName.AssertNotNull(), "Cannot enumerate custom attributes: " + e.Message) ) );

                this._logger.Error?.Log( $"Cannot load extension attributes from '{assembly.FullName}': {e.Message}" );
            }
        }

        return extensionTypes;
    }
}