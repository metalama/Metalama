// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Pipeline.Dependencies;

/// <summary>
/// Collects the dependencies of a given dependent syntax tree.
/// </summary>
internal sealed class DependencyCollectorByDependentSyntaxTree
{
    private readonly ConcurrentDictionary<ProjectKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject> _dependenciesByMasterProject = new();

    public DocumentKey DependentDocumentKey { get; }

    public IReadOnlyDictionary<ProjectKey, DependencyCollectorByDependentSyntaxTreeAndMasterProject> DependenciesByMasterProject
        => this._dependenciesByMasterProject;

    public DependencyCollectorByDependentSyntaxTree( DocumentKey dependentDocumentKey )
    {
        this.DependentDocumentKey = dependentDocumentKey;
    }

    public void AddSyntaxTreeDependency( ProjectKey masterCompilation, DocumentKey masterDocumentKey, ulong masterHash )
    {
#if DEBUG
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var compilationCollector = this._dependenciesByMasterProject.GetOrAdd(
            masterCompilation,
            static ( _, path ) => new DependencyCollectorByDependentSyntaxTreeAndMasterProject( path ),
            this.DependentDocumentKey );

        compilationCollector.AddSyntaxTreeDependency( masterDocumentKey, masterHash );
    }

    public void AddPartialTypeDependency( ProjectKey masterProject, TypeDependencyKey masterPartialType )
    {
#if DEBUG
        if ( this._isReadOnly )
        {
            throw new InvalidOperationException();
        }
#endif

        var compilationCollector = this._dependenciesByMasterProject.GetOrAdd(
            masterProject,
            static ( _, path ) => new DependencyCollectorByDependentSyntaxTreeAndMasterProject( path ),
            this.DependentDocumentKey );

        compilationCollector.AddPartialTypeDependency( masterPartialType );
    }

#if DEBUG
    private bool _isReadOnly;

    public void Freeze()
    {
        this._isReadOnly = true;

        foreach ( var child in this._dependenciesByMasterProject.Values )
        {
            child.Freeze();
        }
    }
#endif
}