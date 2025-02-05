// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    private readonly ConcurrentDictionary<string, DependencyCollectorByDependentSyntaxTree> _dependenciesByDependentFilePath = new();

    public IReadOnlyDictionary<string, DependencyCollectorByDependentSyntaxTree> DependenciesByDependentFilePath => this._dependenciesByDependentFilePath;

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
        foreach ( var dependenciesByDependentSyntaxTree in this._dependenciesByDependentFilePath )
        {
            foreach ( var dependenciesInCompilation in dependenciesByDependentSyntaxTree.Value.DependenciesByMasterProject )
            {
                foreach ( var masterFilePath in dependenciesInCompilation.Value.MasterFilePathsAndHashes.Keys )
                {
                    yield return new SyntaxTreeDependency( masterFilePath, dependenciesInCompilation.Value.DependentFilePath );
                }
            }
        }
    }

    /// <summary>
    /// Enumerates partial type dependencies. This method is used in tests only.
    /// </summary>
    public IEnumerable<PartialTypeDependency> EnumeratePartialTypeDependencies()
    {
        foreach ( var dependenciesByDependentSyntaxTree in this._dependenciesByDependentFilePath )
        {
            foreach ( var dependenciesInCompilation in dependenciesByDependentSyntaxTree.Value.DependenciesByMasterProject )
            {
                foreach ( var masterType in dependenciesInCompilation.Value.MasterPartialTypes )
                {
                    yield return new PartialTypeDependency( masterType, dependenciesInCompilation.Value.DependentFilePath );
                }
            }
        }
    }

    public void AddPartialTypeDependency( string dependentFilePath, ProjectKey masterProjectKey, TypeDependencyKey masterPartialType )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var dependencies = this._dependenciesByDependentFilePath.GetOrAdd( dependentFilePath, x => new DependencyCollectorByDependentSyntaxTree( x ) );

        dependencies.AddPartialTypeDependency( masterProjectKey, masterPartialType );
    }

    public void AddSyntaxTreeDependency( string dependentFilePath, ProjectKey masterProjectKey, string masterFilePath, ulong masterHash )
    {
#if DEBUG
        if ( this.IsReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var dependencies = this._dependenciesByDependentFilePath.GetOrAdd( dependentFilePath, x => new DependencyCollectorByDependentSyntaxTree( x ) );

        dependencies.AddSyntaxTreeDependency( masterProjectKey, masterFilePath, masterHash );
    }

#if DEBUG
    protected bool IsReadOnly { get; private set; }

    public void Freeze()
    {
        this.IsReadOnly = true;

        foreach ( var child in this._dependenciesByDependentFilePath.Values )
        {
            child.Freeze();
        }
    }
#endif
}