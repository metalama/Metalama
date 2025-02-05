// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Creates <see cref="TemplateClass"/>.
/// </summary>
internal abstract class TemplateClassFactory<T>
    where T : TemplateClass
{
    protected CompilationContext CompilationContext { get; }

    private readonly Dictionary<string, T> _classes;

    protected TemplateClassFactory( CompilationContext compilationContext )
    {
        this.CompilationContext = compilationContext;
        this._classes = new Dictionary<string, T>( StringComparer.Ordinal );
    }

    /// <summary>
    /// Gets the aspect classes in a given <see cref="Compilation"/> for the closure of all references <see cref="CompileTimeProject"/>
    /// instances.
    /// </summary>
    public IReadOnlyList<T> GetClasses(
        ProjectServiceProvider serviceProvider,
        CompileTimeProject? compileTimeProject,
        IDiagnosticAdder diagnosticAdder )
    {
        if ( compileTimeProject == null )
        {
            // No compile-time project means that there is no aspect at all.
            return Array.Empty<T>();
        }

        // Add the abstract aspect classes from the framework because they define the abstract templates. The knowledge of abstract templates
        // is used by AspectClass. It is easier to do it here than to do it at the level of CompileTimeProject.
        // We assume the compilation references a single version of Metalama.Framework.

        var frameworkAspectClasses = this.GetFrameworkClasses();

        // Gets the aspect types in the current compilation, including aspects types in referenced assemblies.
        var aspectTypeData =
            compileTimeProject.ClosureProjects
                .SelectMany( p => this.GetTypeNames( p ).Select( t => (Project: p, TypeName: t) ) )
                .Select(
                    item =>
                    {
                        var templateDiscoveryContext = item.Project.TemplateReflectionContext ?? this.CompilationContext;

                        var typeSymbol = templateDiscoveryContext.Compilation.GetTypeByMetadataName( item.TypeName );

                        if ( typeSymbol == null )
                        {
                            // Two conflicting aspect types may be in aliased references.
                            // This is an edge case, but is used in tests to reproduce design-time problems, see below.
                            typeSymbol =
                                templateDiscoveryContext.Compilation.GetTypesByMetadataName( item.TypeName )
                                    .FirstOrDefault( s => s.ContainingAssembly.Identity.Equals( item.Project.RunTimeIdentity ) );

                            if ( typeSymbol == null )
                            {
                                diagnosticAdder.Report(
                                    TemplatingDiagnosticDescriptors.CannotFindAspectInCompilation.CreateRoslynDiagnostic(
                                        Location.None,
                                        (item.TypeName, item.Project.RunTimeIdentity.Name) ) );

                                return null;
                            }
                        }

                        var typeName = typeSymbol.GetReflectionFullName();

                        return new TemplateClassData(
                            item.Project,
                            item.TypeName,
                            typeSymbol,
                            item.Project.GetType( typeName ),
                            templateDiscoveryContext );
                    } )
                .WhereNotNull()
                .Concat( frameworkAspectClasses );

        var aspectTypeDataDictionary = new Dictionary<string, TemplateClassData>();

        foreach ( var aspectType in aspectTypeData )
        {
            // IMPORTANT: At design time, when a project is being renamed, we can get duplicate aspect types while project dependency tree is being updated.
            //            Two dependency projects referencing the same assembly may not be synchronized, causing two CompileTimeProjects to exist
            //            for one assembly (same aspects, two different assembly names).
            aspectTypeDataDictionary[aspectType.TypeName] = aspectType;
        }

        return this.GetClasses( aspectTypeDataDictionary, diagnosticAdder, serviceProvider );
    }

    protected abstract IEnumerable<TemplateClassData> GetFrameworkClasses();

    protected abstract IEnumerable<string> GetTypeNames( CompileTimeProject project );

    /// <summary>
    /// Creates a list of <see cref="TemplateClass"/> given input list of types. This method is used for test only.
    /// </summary>
    internal IReadOnlyList<T> GetClasses(
        ProjectServiceProvider serviceProvider,
        ITemplateReflectionContext templateReflectionContext,
        IReadOnlyList<INamedTypeSymbol> types,
        CompileTimeProject compileTimeProject,
        IDiagnosticAdder diagnosticAdder )
    {
        var aspectTypesDiagnostics = types
            .SelectAsImmutableArray( t => (Symbol: t, ReflectionName: t.GetReflectionFullName().AssertNotNull()) )
            .ToDictionary(
                t => t.ReflectionName,
                t => new TemplateClassData(
                    compileTimeProject,
                    t.ReflectionName,
                    t.Symbol,
                    compileTimeProject.GetType( t.ReflectionName ),
                    templateReflectionContext ) );

        return this.GetClasses( aspectTypesDiagnostics, diagnosticAdder, serviceProvider );
    }

    private IReadOnlyList<T> GetClasses(
        Dictionary<string, TemplateClassData> templateTypeDataDictionary,
        IDiagnosticAdder diagnosticAdder,
        ProjectServiceProvider serviceProvider )
    {
        // A local function that recursively processes an aspect type.
        bool TryProcessType(
            INamedTypeSymbol templateTypeSymbol,
            Type aspectReflectionType,
            CompileTimeProject? project,
            ITemplateReflectionContext templateDiscoveryContext,
            [NotNullWhen( true )] out T? metadata )
        {
            if ( this._classes.TryGetValue( templateTypeSymbol.GetReflectionFullName().AssertNotNull(), out var existingValue ) )
            {
                metadata = existingValue;

                return true;
            }

            T? baseTemplateClass = null;

            if ( templateTypeSymbol.BaseType != null )
            {
                // Process the base type.

                if ( templateTypeDataDictionary.TryGetValue( templateTypeSymbol.BaseType.GetReflectionFullName().AssertNotNull(), out var baseData ) )
                {
                    if ( !TryProcessType(
                            templateTypeSymbol.BaseType,
                            aspectReflectionType.BaseType!,
                            baseData.Project,
                            templateDiscoveryContext,
                            out baseTemplateClass ) )
                    {
                        metadata = null;

                        return false;
                    }
                }
                else
                {
                    // This is not an aspect class, typically System.Attribute.
                }
            }

            if ( !this.TryCreate(
                    serviceProvider,
                    templateTypeSymbol,
                    aspectReflectionType,
                    baseTemplateClass,
                    project,
                    diagnosticAdder,
                    templateDiscoveryContext,
                    out metadata ) )
            {
                return false;
            }

            this._classes.Add( templateTypeSymbol.GetReflectionFullName().AssertNotNull(), metadata );

            return true;
        }

        // Process all types.
        var resultList = new List<T>( templateTypeDataDictionary.Count );

        foreach ( var attributeTypeData in templateTypeDataDictionary.Values )
        {
            if ( TryProcessType(
                    attributeTypeData.TypeSymbol,
                    attributeTypeData.ReflectionType,
                    attributeTypeData.Project,
                    attributeTypeData.TemplateReflectionContext,
                    out var metadata ) )
            {
                resultList.Add( metadata );
            }
        }

        return resultList;
    }

    protected abstract bool TryCreate(
        ProjectServiceProvider serviceProvider,
        INamedTypeSymbol templateTypeSymbol,
        Type templateReflectionType,
        T? otherTemplateClass,
        CompileTimeProject? compileTimeProject,
        IDiagnosticAdder diagnosticAdder,
        ITemplateReflectionContext templateReflectionContext,
        [NotNullWhen( true )] out T? templateClass );

    protected sealed record TemplateClassData(
        CompileTimeProject? Project,
        string TypeName,
        INamedTypeSymbol TypeSymbol,
        Type ReflectionType,
        ITemplateReflectionContext TemplateReflectionContext );
}