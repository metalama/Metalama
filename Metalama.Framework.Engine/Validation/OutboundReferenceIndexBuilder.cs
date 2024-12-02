// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Validation;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Engine.Validation;

internal sealed class OutboundReferenceIndexBuilder : ReferenceIndexBuilder
{
    private readonly ConcurrentQueue<OutboundReference> _references = new();
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;
    private readonly ITaskRunner _taskRunner;
    private bool _frozen;

    public OutboundReferenceIndexBuilder( ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
        this._taskRunner = serviceProvider.Global.GetRequiredService<ITaskRunner>();
    }

    protected override void AddReferenceCore( ISymbol referencedSymbol, ISymbol referencingSymbol, SyntaxNodeOrToken node, ReferenceKinds referenceKind )
    {
        if ( this._frozen )
        {
            throw new InvalidOperationException();
        }

        this._references.Enqueue( new OutboundReference( referencedSymbol, referencingSymbol, node, referenceKind ) );
    }

    public IReadOnlyCollection<OutboundReference> GetReferences()
    {
        this._frozen = true;

        return this._references;
    }

    private void IndexSyntaxNode( SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken = default )
    {
        var walker = new ReferenceIndexWalker( this._serviceProvider, this, ReferenceIndexerOptions.All, semanticModel, cancellationToken );
        walker.Visit( node );
    }

    public void IndexDeclaration( IDeclaration declaration, CancellationToken cancellationToken )
    {
        var semanticModelProvider = declaration.GetCompilationContext().SemanticModelProvider;

        var sources = declaration.Sources;

        switch ( sources.Length )
        {
            case 0:
                return;

            case 1:
                Index( sources[0] );

                break;

            default:
                this._taskRunner.RunSynchronously(
                    () => this._concurrentTaskRunner.RunConcurrentlyAsync( sources, Index, cancellationToken ),
                    cancellationToken );

                break;
        }

        void Index( SourceReference source )
        {
            var node = source.SyntaxNodeOrToken().AsNode().AssertNotNull();
            this.IndexSyntaxNode( node, semanticModelProvider.GetSemanticModel( node.SyntaxTree ), cancellationToken );
        }
    }
}