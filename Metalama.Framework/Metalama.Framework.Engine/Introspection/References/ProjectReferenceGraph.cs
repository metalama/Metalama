// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Introspection.References;

internal sealed class ProjectReferenceGraph : IIntrospectionReferenceGraph
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompilationModel _compilation;
    private readonly object _referenceIndexSync = new();
    private readonly ITaskRunner _taskRunner;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    private WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>? _includeDerivedTypesCache;
    private WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>? _includeContainedDeclarationsCache;
    private WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>? _includeAllCache;
    private WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>? _includeNoChildrenCache;
    private volatile InboundReferenceIndex? _referenceIndex;

    public ProjectReferenceGraph( ProjectServiceProvider serviceProvider, CompilationModel compilation )
    {
        this._serviceProvider = serviceProvider;
        this._compilation = compilation;
        this._taskRunner = serviceProvider.Global.GetRequiredService<ITaskRunner>();
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
    }

    private InboundReferenceIndex GetInboundReferencesIndex()
    {
        if ( this._referenceIndex != null )
        {
            return this._referenceIndex;
        }

        lock ( this._referenceIndexSync )
        {
            if ( this._referenceIndex != null )
            {
                return this._referenceIndex;
            }

            var compilationModel = this._compilation;
            var compilationContext = compilationModel.CompilationContext;

            var builder = new InboundReferenceIndexBuilder(
                compilationModel.Project.ServiceProvider,
                ReferenceIndexerOptions.All,
                StructuralSymbolComparer.Default );

            this._taskRunner.RunSynchronously(
                () => this._concurrentTaskRunner.RunConcurrentlyAsync(
                    compilationContext.Compilation.SyntaxTrees,
                    tree => builder.IndexSyntaxTree( tree, compilationContext.SemanticModelProvider ),
                    CancellationToken.None ) );

            return this._referenceIndex = builder.ToReadOnly();
        }
    }

    public IEnumerable<IIntrospectionReference> GetInboundReferences(
        IDeclaration destination,
        IntrospectionChildKinds childKinds = IntrospectionChildKinds.ContainingDeclaration,
        CancellationToken cancellationToken = default )
    {
        var cache = childKinds switch
        {
            IntrospectionChildKinds.None => this._includeNoChildrenCache ??= new WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>(),
            IntrospectionChildKinds.DerivedType => this._includeDerivedTypesCache ??=
                new WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>(),
            IntrospectionChildKinds.ContainingDeclaration => this._includeContainedDeclarationsCache ??=
                new WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>(),
            IntrospectionChildKinds.All => this._includeAllCache ??= new WeakCache<IDeclaration, IReadOnlyCollection<IIntrospectionReference>>(),
            _ => throw new ArgumentOutOfRangeException( nameof(childKinds), childKinds, null )
        };

        // Cache fast path.
        if ( cache.TryGetValue( destination, out var result ) )
        {
            return result;
        }

        return cache.GetOrAdd( destination, _ => this.GetInboundReferencesCore( destination, childKinds ) );
    }

    public IEnumerable<IIntrospectionReference> GetOutboundReferences( IDeclaration origin, CancellationToken cancellationToken = default )
    {
        var builder = new OutboundReferenceIndexBuilder( this._serviceProvider );
        builder.IndexDeclaration( origin, cancellationToken );

        return builder.GetReferences()
            .GroupBy( r => new SymbolPair( r.ReferencedSymbol, r.ReferencingSymbol ) )
            .Select(
                group => new OutboundReference(
                    group.Key.Referenced,
                    group.Key.Referencing,
                    group,
                    this._compilation ) );
    }

    private sealed record SymbolPair( ISymbol Referenced, ISymbol Referencing )
    {
        public override int GetHashCode()
            => HashCode.Combine(
                SymbolEqualityComparer.Default.GetHashCode( this.Referenced ),
                SymbolEqualityComparer.Default.GetHashCode( this.Referencing ) );

        public bool Equals( SymbolPair? other )
            => other != null && SymbolEqualityComparer.Default.Equals( this.Referenced, other.Referenced )
                             && SymbolEqualityComparer.Default.Equals( this.Referencing, other.Referencing );
    }

    private IReadOnlyCollection<IIntrospectionReference> GetInboundReferencesCore( IDeclaration destination, IntrospectionChildKinds childKinds )
    {
        var symbol = destination.GetSymbol();

        if ( symbol == null )
        {
            return [];
        }
        else if ( this.GetInboundReferencesIndex().TryGetInboundReferences( symbol, out var referencedSymbolInfo ) )
        {
            var descendants = referencedSymbolInfo.DescendantsAndSelf( ChildKindHelper.ToChildKinds( childKinds ) );

            return descendants
                .SelectMany( d => d.References.Select( r => (d.ReferencedSymbol, ReferencingSymbolInfo: r) ) )
                .Select( x => new InboundReference( x.ReferencedSymbol, x.ReferencingSymbolInfo, this._compilation ) )
                .ToReadOnlyList();
        }
        else
        {
            return [];
        }
    }
}