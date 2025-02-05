// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Implements the <see cref="IDependencyCollector"/> interface.
/// </summary>
internal sealed class DependencyCollector : BaseDependencyCollector, IDependencyCollector
{
    private readonly ILogger _logger;
    private readonly bool _storeTypeName;

    private readonly ConcurrentDictionary<(ISymbol, ISymbol), bool> _processedSymbolDependencies = new();
    private readonly ConcurrentDictionary<(ISymbol, string), bool> _processedSyntaxTreeDependencies = new();
    private readonly Dictionary<AssemblyIdentity, ProjectKey> _referencesProjects = new();
    private readonly SafeSymbolComparer _symbolEqualityComparer;

    public DependencyCollector( in ProjectServiceProvider serviceProvider, IProjectVersion projectVersion, PartialCompilation? partialCompilation = null ) :
        base( projectVersion, partialCompilation )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "DependencyCollector" );
        this._storeTypeName = serviceProvider.GetRequiredService<IProjectOptions>().IsTest;

        this._symbolEqualityComparer = this.PartialCompilation.Compilation.GetCompilationContext().SymbolComparer;
        this.IndexReferencedProjects( projectVersion );
    }

    private void IndexReferencedProjects( IProjectVersion projectVersion )
    {
        var assemblyIdentity = projectVersion.Compilation.Assembly.Identity;

        if ( !this._referencesProjects.ContainsKey( assemblyIdentity ) )
        {
            this._referencesProjects.Add( assemblyIdentity, projectVersion.ProjectKey );

            foreach ( var reference in projectVersion.ReferencedProjectVersions )
            {
                this.IndexReferencedProjects( reference.Value );
            }
        }
    }

    private void AddDependency( INamedTypeSymbol masterSymbol, IReadOnlyCollection<SyntaxTree> dependentTrees )
    {
        var currentCompilationAssembly = this.ProjectVersion.Compilation.Assembly;

        var masterIsPartial = masterSymbol.DeclaringSyntaxReferences[0].GetSyntax() is BaseTypeDeclarationSyntax type
                              && type.Modifiers.Any( m => m.IsKind( SyntaxKind.PartialKeyword ) );

        if ( this._symbolEqualityComparer.Equals( masterSymbol.ContainingAssembly, currentCompilationAssembly ) )
        {
            // We have a dependency within the current assembly.
            foreach ( var dependentTree in dependentTrees )
            {
                foreach ( var masterSyntaxReference in masterSymbol.DeclaringSyntaxReferences )
                {
                    if ( dependentTree != masterSyntaxReference.SyntaxTree )
                    {
                        this.AddSyntaxTreeDependency(
                            dependentTree.FilePath,
                            this.ProjectVersion.ProjectKey,
                            masterSyntaxReference.SyntaxTree.FilePath,
                            0 );
                    }
                }

                if ( masterIsPartial )
                {
                    this.AddPartialTypeDependency(
                        dependentTree.FilePath,
                        this.ProjectVersion.ProjectKey,
                        new TypeDependencyKey( masterSymbol, this._storeTypeName ) );
                }
            }
        }
        else
        {
            if ( !this._referencesProjects.TryGetValue( masterSymbol.ContainingAssembly.Identity, out var containingProjectKey ) )
            {
                throw new AssertionFailedException( $"Assembly '{masterSymbol.ContainingAssembly.Identity}' not found in references." );
            }

            if ( this.ProjectVersion.ReferencedProjectVersions.TryGetValue( containingProjectKey, out var referencedCompilationVersion ) )
            {
                // We have a dependency to a different compilation.

                foreach ( var masterSyntaxReference in masterSymbol.DeclaringSyntaxReferences )
                {
                    if ( referencedCompilationVersion.TryGetSyntaxTreeVersion( masterSyntaxReference.SyntaxTree.FilePath, out var masterSyntaxTreeVersion ) )
                    {
                        foreach ( var dependentSyntaxReference in dependentTrees )
                        {
                            this.AddSyntaxTreeDependency(
                                dependentSyntaxReference.FilePath,
                                referencedCompilationVersion.ProjectKey,
                                masterSyntaxReference.SyntaxTree.FilePath,
                                masterSyntaxTreeVersion.DeclarationHash );
                        }
                    }
                    else
                    {
                        this._logger.Warning?.Log(
                            $"Cannot find '{masterSyntaxReference.SyntaxTree.FilePath}' in '{referencedCompilationVersion.ProjectKey}'." );
                    }

                    if ( masterIsPartial )
                    {
                        foreach ( var dependentSyntaxReference in dependentTrees )
                        {
                            this.AddPartialTypeDependency(
                                dependentSyntaxReference.FilePath,
                                referencedCompilationVersion.ProjectKey,
                                new TypeDependencyKey( masterSymbol, this._storeTypeName ) );
                        }
                    }
                }
            }
        }
    }

    public void AddDependency( INamedTypeSymbol masterSymbol, INamedTypeSymbol dependentSymbol )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        masterSymbol = masterSymbol.OriginalDefinition.GetTopmostContainingType();
        dependentSymbol = dependentSymbol.OriginalDefinition.GetTopmostContainingType();

        // If there is no syntax reference, it means it is not defined from source code but from a PE file.
        // PE references are tracked separately, and a change there invalidate the whole pipeline configuration,
        // so there is no need to track it.
        if ( masterSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty )
        {
            return;
        }

        // No need to consider self-references because we always include all partial files for all types included in a partial compilation.
        if ( this._symbolEqualityComparer.Equals( masterSymbol, dependentSymbol ) )
        {
            return;
        }

        // Avoid spending time processing the same call twice.
        if ( !this._processedSymbolDependencies.TryAdd( (masterSymbol, dependentSymbol), true ) )
        {
            return;
        }

        var currentCompilationAssembly = this.ProjectVersion.Compilation.Assembly;

        if ( !this._symbolEqualityComparer.Equals( dependentSymbol.ContainingAssembly, currentCompilationAssembly ) )
        {
            // We only collect dependencies in the current assembly.
            return;
        }

        this.AddDependency( masterSymbol, dependentSymbol.DeclaringSyntaxReferences.SelectAsArray( reference => reference.SyntaxTree ) );
    }

    public void AddDependency( INamedTypeSymbol masterSymbol, SyntaxTree dependentTree )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        masterSymbol = masterSymbol.OriginalDefinition.GetTopmostContainingType();
        var dependentTreePath = dependentTree.FilePath;

        if ( masterSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty )
        {
            return;
        }

        // Avoid spending time processing the same call twice.
        if ( !this._processedSyntaxTreeDependencies.TryAdd( (masterSymbol, dependentTreePath), true ) )
        {
            return;
        }

        this.AddDependency( masterSymbol, [dependentTree] );
    }

    public void AddDependency( SyntaxTree masterTree, SyntaxTree dependentTree )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        if ( dependentTree != masterTree )
        {
            this.AddSyntaxTreeDependency(
                dependentTree.FilePath,
                this.ProjectVersion.ProjectKey,
                masterTree.FilePath,
                0 );
        }
    }
}