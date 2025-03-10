// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class IndexerInvoker : Invoker<IIndexer>, IIndexerInvoker
{
    public IndexerInvoker( IIndexer indexer, InvokerOptions? options = default, object? target = null ) : base( indexer, options, target ) { }

    public object GetValue( params object?[] args )
        => new DelegateUserExpression(
            context => this.CreateIndexerAccess( args, context ),
            this.Member.Type,
            isAssignable: this.Member.Writeability != Writeability.None );

    public object SetValue( object? value, params object?[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                var propertyAccess = this.CreateIndexerAccess( args, context );

                return AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    propertyAccess,
                    TypedExpressionSyntaxImpl.GetSyntaxFromValue( value, context ) );
            },
            this.Member.Type,
            isAssignable: true );
    }

    private ExpressionSyntax CreateIndexerAccess( object?[]? args, SyntaxSerializationContext context )
    {
        args ??= [];

        var receiverInfo = this.GetReceiverInfo( context );
        var receiverSyntax = this.Member.GetReceiverSyntax( receiverInfo.TypedExpressionSyntax, context );
        var argExpressions = TypedExpressionSyntaxImpl.FromValues( args, context ).AssertNotNull();

        // TODO: Aspect references.

        var expression = ElementAccessExpression( receiverSyntax ).AddArgumentListArguments( argExpressions.SelectAsArray( e => Argument( e.Syntax ) ) );

        return expression;
    }

    public IIndexerInvoker With( InvokerOptions options ) => this.Options == options ? this : new IndexerInvoker( this.Member, options );

    public IIndexerInvoker With( object? target, InvokerOptions options = default )
        => this.Target == target && this.Options == options ? this : new IndexerInvoker( this.Member, options, target );
}