// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Validation;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.ReferenceGraph;

public sealed class ReferencedSymbolInfo
{
    private ConcurrentDictionary<ISymbol, ReferencingNodeList>? _explicitReferences;
    private ConcurrentQueue<ReferencedSymbolChild>? _children;

    // ReSharper disable once MemberCanBeInternal
    public ISymbol ReferencedSymbol { get; }

    internal ReferencedSymbolInfo( ISymbol referencedSymbol )
    {
        this.ReferencedSymbol = referencedSymbol;
    }

    internal void AddChild( ReferencedSymbolInfo child, ChildKinds kind )
    {
        LazyInitializer.EnsureInitialized( ref this._children, static () => new ConcurrentQueue<ReferencedSymbolChild>() );

        this._children.Enqueue( new ReferencedSymbolChild( child, kind ) );
    }

    internal void AddReference( ISymbol referencingSymbol, SyntaxNodeOrToken node, ReferenceKinds referenceKind )
    {
        LazyInitializer.EnsureInitialized( ref this._explicitReferences, static () => new ConcurrentDictionary<ISymbol, ReferencingNodeList>() );

        var nodes = this._explicitReferences.GetOrAddNew( referencingSymbol );

        lock ( nodes )
        {
            nodes.Add( new ReferencingNode( node, referenceKind ) );
        }
    }

    // ReSharper disable once MemberCanBeInternal
    public IEnumerable<ReferencingSymbolInfo> References
        => this._explicitReferences?.SelectAsReadOnlyCollection( x => new ReferencingSymbolInfo( x.Key, this.ReferencedSymbol, x.Value ) )
           ?? Enumerable.Empty<ReferencingSymbolInfo>();

    private IEnumerable<ReferencedSymbolInfo> Children( ChildKinds kinds ) => this._children?.Where( x => (x.Kind & kinds) != 0 ).Select( x => x.Info ) ?? [];

    internal IEnumerable<ReferencedSymbolInfo> DescendantsAndSelf( ChildKinds kinds )
    {
        if ( kinds == ChildKinds.None )
        {
            return [this];
        }
        else
        {
            return this.SelectManyRecursiveDistinct( x => x.Children( kinds ) );
        }
    }

    // ReSharper disable once MemberCanBeInternal
    public IEnumerable<ReferencingSymbolInfo> GetAllReferences( ChildKinds kinds ) => this.DescendantsAndSelf( kinds ).SelectMany( x => x.References );

    public override string ToString() => $"{{Symbol={this.ReferencedSymbol.ToDebugString()}, References.Count={this.References.Count()}}}";
}