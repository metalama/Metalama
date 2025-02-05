// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class EventInvoker : Invoker<IEvent>, IEventInvoker
{
    public EventInvoker( IEvent @event, InvokerOptions? options = default, object? target = null ) : base( @event, options, target ) { }

    public object Add( object? value )
    {
        return new DelegateUserExpression(
            context =>
            {
                var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventAddAccessor, context );

                return AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    eventAccess,
                    TypedExpressionSyntaxImpl.GetSyntaxFromValue( value, context ) );
            },
            this.Member.AddMethod.ReturnType );
    }

    public object Remove( object? value )
    {
        return new DelegateUserExpression(
            context =>
            {
                var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventRemoveAccessor, context );

                return AssignmentExpression(
                    SyntaxKind.SubtractAssignmentExpression,
                    eventAccess,
                    TypedExpressionSyntaxImpl.GetSyntaxFromValue( value, context ) );
            },
            this.Member.RemoveMethod.ReturnType );
    }

    public object Raise( params object?[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventRaiseAccessor, context );

                var arguments = this.Member.GetArguments(
                    this.Member.Signature.Parameters,
                    TypedExpressionSyntaxImpl.FromValues( args, context ),
                    context.SyntaxGenerationContext );

                return ConditionalAccessExpression(
                    eventAccess,
                    InvocationExpression( MemberBindingExpression( IdentifierName( "Invoke" ) ) ).AddArgumentListArguments( arguments ) );
            },
            this.Member.Signature.ReturnType );
    }

    public IEventInvoker With( InvokerOptions options ) => this.Options == options ? this : new EventInvoker( this.Member, options );

    public IEventInvoker With( object? target, InvokerOptions options = default )
        => this.Target == target && this.Options == options ? this : new EventInvoker( this.Member, options, target );

    private ExpressionSyntax CreateEventExpression( AspectReferenceTargetKind targetKind, SyntaxSerializationContext syntaxSerializationContext )
    {
        this.CheckInvocationOptionsAndTarget();

        var receiverInfo = this.GetReceiverInfo( syntaxSerializationContext );
        var name = IdentifierName( this.GetCleanTargetMemberName() );

        var receiverSyntax = this.Member.GetReceiverSyntax( receiverInfo.TypedExpressionSyntax, syntaxSerializationContext );

        var expression =
            receiverInfo.RequiresConditionalAccess
                ? (ExpressionSyntax) ConditionalAccessExpression( receiverSyntax, MemberBindingExpression( name ) )
                : MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        receiverSyntax,
                        name )
                    .WithSimplifierAnnotationIfNecessary( syntaxSerializationContext.SyntaxGenerationContext );

        // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or it's declaring type).
        if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
        {
            expression = expression.WithAspectReferenceAnnotation( receiverInfo.AspectReferenceSpecification.WithTargetKind( targetKind ) );
        }

        return expression;
    }
}