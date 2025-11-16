// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Extensibility;

public class ExtensionLoaderBase
{
    private readonly ILogger _logger;

    public ExtensionLoaderBase( GlobalServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(ExtensionLoaderBase) );
    }

    public IEnumerable<string> GetExtensionAssemblies( IEnumerable<ExtensionAssemblyReference> assemblyReferences )
    {
        var targetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net8.0";

        this._logger.Trace?.Log( $"Getting extension assemblies for target framework '{targetFramework}'." );

        return assemblyReferences.Where( a => a.TargetFramework == targetFramework || string.IsNullOrEmpty( a.TargetFramework ) )
            .Select( a => a.Path );
    }

    internal List<Type> DiscoverExtensionTypes(
        CompileTimeDomain domain,
        ExtensionKind extensionKind,
        IReadOnlyCollection<ExtensionAssemblyReference> assemblyReferences,
        bool avoidLockingAssemblies = false )
    {
        this._logger.Trace?.Log( $"Discovering extension types of kind '{extensionKind}' in assemblies: {string.Join( ", ", assemblyReferences )}." );

        // First load assemblies, because we don't know their order and dependencies.
        var assemblies = this.LoadExtensionAssemblies( domain, assemblyReferences, avoidLockingAssemblies );

        // Now we can load the types.
        return this.DiscoverExtensionTypes( extensionKind, assemblies );
    }

    private List<Assembly> LoadExtensionAssemblies(
        CompileTimeDomain domain,
        IEnumerable<ExtensionAssemblyReference> assemblyReferences,
        bool avoidLockingAssemblies )
    {
        // It is essential to materialize the query into a list, otherwise assemblies are not loaded if the caller does not evaluate the query.

        return this.GetExtensionAssemblies( assemblyReferences )
            .Select(
                path =>
                {
                    this._logger.Trace?.Log( $"Loading extension assembly '{path}'." );

                    return domain.LoadAssembly( path, null, new LoadAssemblyOptions() { IsShared = true, AvoidLocking = avoidLockingAssemblies } );
                } )
            .ToList();
    }

    public List<Type> DiscoverExtensionTypes( CompileTimeDomain domain, ExtensionKind extensionKind, IEnumerable<string> assemblies )
    {
        var loadedAssemblies = assemblies.Select(
                path =>
                {
                    this._logger.Trace?.Log( $"Loading extension assembly '{path}'." );

                    return domain.LoadAssembly( path, null, LoadAssemblyOptions.Shared );
                } )
            .ToList();

        return this.DiscoverExtensionTypes( extensionKind, loadedAssemblies );
    }

    private List<Type> DiscoverExtensionTypes( ExtensionKind extensionKind, IEnumerable<Assembly> assemblies )
    {
        this._logger.Trace?.Log( $"Discovering ExportExtensionAttribute with kind '{extensionKind}'." );

        return assemblies
            .SelectMany(
                assembly => assembly.GetCustomAttributes<ExportExtensionAttribute>()
                    .Where( attribute => attribute.ExtensionKind == extensionKind )
                    .Select( attribute => attribute.ExtensionType ) )
            .ToList();
    }
}