// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class IndexerInvoker : Invoker<IIndexer>, IIndexerInvoker
{
    public IndexerInvoker( IIndexer indexer, InvokerOptions options = default, IExpression? target = null ) : base( indexer, options, target ) { }

    [Obsolete]
    public object GetValue( params object?[] args ) => this.GetItemExpression( args );

    public object SetValue( object? value, params object?[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                var propertyAccess = this.CreateIndexerAccess( this.CaptureExpressions( args ), context );

                return AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    propertyAccess,
                    TypedExpressionSyntaxImpl.GetSyntaxFromValue( value, context ) );
            },
            this.Member.Type,
            isAssignable: true );
    }

    public IExpression this[ params IExpression[] args ] => this.GetItemExpression( args );

    public IExpression GetItemExpression( IExpression[] args )
    {
        return new DelegateUserExpression(
            context => this.CreateIndexerAccess( args, context ),
            this.Member.Type,
            isAssignable: this.Member.Writeability != Writeability.None );
    }

    public IExpression this[ params object?[] args ] => this.GetItemExpression( args );

    public IExpression GetItemExpression( object?[] args ) => this.GetItemExpression( this.CaptureExpressions( args ) );

    private IExpression[] CaptureExpressions( object?[] args ) => CapturedUserExpression.Create( this.Compilation, args );

    private ExpressionSyntax CreateIndexerAccess( IExpression[]? args, SyntaxSerializationContext context )
    {
        args ??= [];

        var receiverInfo = this.GetReceiverInfo( context );
        var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Member, context );
        var argExpressions = args.SelectAsReadOnlyList( x => x.ToTypedExpressionSyntax( context ) );

        // TODO: Aspect references.

        var expression = ElementAccessExpression( receiverSyntax ).AddArgumentListArguments( argExpressions.SelectAsArray( e => Argument( e.Syntax ) ) );

        return expression;
    }

    public IIndexerInvoker WithOptions( InvokerOptions options ) => this.Options == options ? this : new IndexerInvoker( this.Member, options, this.Target );

    public IIndexerInvoker WithObject( object obj ) => this.WithObject( CapturedUserExpression.Create( this.Compilation, obj ) );

    public IIndexerInvoker WithObject( IExpression obj ) => this.IsSameTarget( obj ) ? this : new IndexerInvoker( this.Member, this.Options, obj );

    IIndexerInvoker IIndexerInvoker.With( InvokerOptions options ) => this.WithOptions( options );

    IIndexerInvoker IIndexerInvoker.With( object? target, InvokerOptions options ) => this.WithOptions( options ).WithObject( target! );
}