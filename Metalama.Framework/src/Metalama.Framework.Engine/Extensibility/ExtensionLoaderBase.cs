// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using SharpCrafters.Backstage.Diagnostics;
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
        var targetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net10.0";

        this._logger.Trace?.Log( $"Getting extension assemblies for target framework '{targetFramework}'." );

        var assemblyReferenceList = assemblyReferences.ToReadOnlyList();
        var selectedReferences = TargetedAssemblyReference.SelectForCurrentProcess( assemblyReferenceList ).ToReadOnlyList();

        if ( this._logger.Trace != null )
        {
            foreach ( var skippedReference in assemblyReferenceList.Except( selectedReferences ) )
            {
                this._logger.Trace.Log(
                    $"Skipping the extension assembly reference '{skippedReference}' because it does not satisfy the current process, "
                    + "or because another reference of the same extension targets a higher Roslyn version." );
            }
        }

        return selectedReferences.SelectAsArray( a => a.Path.AssertNotNull() );
    }

    /// <summary>
    /// Reports a warning for each extension that has no reference satisfying the current process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An extension package can declare one reference per Roslyn version and target framework. A reference satisfies the current process
    /// when it targets the target framework of the current process and a Roslyn version lower than or equal to the one of the current process.
    /// When no reference satisfies the current process, for instance because the package was built only for a Roslyn version that is
    /// higher than the one of the current process, the extension would otherwise be ignored without any diagnostic.
    /// </para>
    /// <para>
    /// The references are grouped by the name that <see cref="TargetedAssemblyReference.GetExtensionName"/> returns, so that the references
    /// of an extension whose assembly names end with the Roslyn version belong to the same group.
    /// </para>
    /// </remarks>
    private void ReportUnsatisfiedExtensionAssemblies( IEnumerable<TargetedAssemblyReference> assemblyReferences, IDiagnosticAdder diagnosticAdder )
    {
        foreach ( var group in assemblyReferences.GroupBy( a => a.GetExtensionName(), StringComparer.OrdinalIgnoreCase ) )
        {
            if ( group.Any( a => a.SatisfiesCurrentProcess ) )
            {
                continue;
            }

            var declaredTargets = string.Join(
                ", ",
                group.Select(
                    a => $"Roslyn {a.TargetRoslynVersion?.ToString() ?? "any"} and target framework '{a.TargetFramework ?? "any"}'" ) );

            var currentRoslynVersion = RoslynApiVersion.Current.ToVersion().ToString();

            this._logger.Warning?.Log(
                $"The extension assembly '{group.Key}' has no reference for Roslyn {currentRoslynVersion} and target framework "
                + $"'{TargetedAssemblyReference.CurrentTargetFramework}'. Declared targets: {declaredTargets}." );

            diagnosticAdder.Report(
                GeneralDiagnosticDescriptors.ExtensionAssemblyNotAvailableForCurrentProcess.CreateRoslynDiagnostic(
                    null,
                    (group.Key, declaredTargets, currentRoslynVersion, TargetedAssemblyReference.CurrentTargetFramework) ) );
        }
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

        var assemblyReferenceList = assemblyReferences.ToReadOnlyList();

        this.ReportUnsatisfiedExtensionAssemblies( assemblyReferenceList, diagnosticAdder );

        foreach ( var path in this.GetExtensionAssemblyPaths( assemblyReferenceList ) )
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