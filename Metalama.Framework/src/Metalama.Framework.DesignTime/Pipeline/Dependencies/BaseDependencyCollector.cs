// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.CodeModel;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// A unit-testable base for <see cref="DependencyCollector"/>. 
/// </summary>
internal class BaseDependencyCollector
{
    protected IProjectVersion ProjectVersion { get; }

    /// <summary>
    /// Gets the <see cref="PartialCompilation"/> for which the dependency graph was collected.
    /// </summary>
    public PartialCompilation PartialCompilation { get; }

    private readonly ConcurrentDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTree> _dependenciesByDependentDocumentKey = new();

    public IReadOnlyDictionary<DocumentKey, DependencyCollectorByDependentSyntaxTree> DependenciesByDependentDocumentKey => this._dependenciesByDependentDocumentKey;

    public BaseDependencyCollector( IProjectVersion projectVersion, PartialCompilation? partialCompilation = null )
    {
        this.ProjectVersion = projectVersion;
        this.PartialCompilation = partialCompilation ?? PartialCompilation.CreateComplete( projectVersion.Compilation );
    }

    /// <summary>
    /// Enumerates the syntax tree dependencies. This method is used in tests only.
    /// </summary>
    public IEnumerable<SyntaxTreeDependency> EnumerateSyntaxTreeDependencies()
    {
        foreach ( var dependenciesByDependentSyntaxTree in this._dependenciesByDependentDocumentKey )
        {
            foreach ( var dependenciesInCompilation in dependenciesByDependentSyntaxTree.Value.DependenciesByMasterProject )
            {
                foreach ( var masterDocumentKey in dependenciesInCompilation.Value.MasterDocumentKeysAndHashes.Keys )
                {
                    yield return new SyntaxTreeDependency( masterDocumentKey, dependenciesInCompilation.Value.DependentDocumentKey );
                }
            }
        }
    }

    /// <summary>
    /// Enumerates partial type dependencies. This method is used in tests only.
    /// </summary>
    public IEnumerable<PartialTypeDependency> EnumeratePartialTypeDependencies()
    {
        foreach ( var dependenciesByDependentSyntaxTree in this._dependenciesByDependentDocumentKey )
        {
            foreach ( var dependenciesInCompilation in dependenciesByDependentSyntaxTree.Value.DependenciesByMasterProject )
            {
                foreach ( var masterType in dependenciesInCompilation.Value.MasterPartialTypes )
                {
                    yield return new PartialTypeDependency( masterType, dependenciesInCompilation.Value.DependentDocumentKey );
                }
            }
        }
    }

    public void AddPartialTypeDependency( DocumentKey dependentDocumentKey, ProjectKey masterProjectKey, TypeDependencyKey masterPartialType )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var dependencies = this._dependenciesByDependentDocumentKey.GetOrAdd( dependentDocumentKey, x => new DependencyCollectorByDependentSyntaxTree( x ) );

        dependencies.AddPartialTypeDependency( masterProjectKey, masterPartialType );
    }

    public void AddSyntaxTreeDependency( DocumentKey dependentDocumentKey, ProjectKey masterProjectKey, DocumentKey masterDocumentKey, ulong masterHash )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var dependencies = this._dependenciesByDependentDocumentKey.GetOrAdd( dependentDocumentKey, x => new DependencyCollectorByDependentSyntaxTree( x ) );

        dependencies.AddSyntaxTreeDependency( masterProjectKey, masterDocumentKey, masterHash );
    }

#if DEBUG
    protected bool IsReadOnly { get; private set; }

    public void Freeze()
    {
        this.IsReadOnly = true;

        foreach ( var child in this._dependenciesByDependentDocumentKey.Values )
        {
            child.Freeze();
        }
    }
#endif
}