// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class EventInvoker : Invoker<IEvent>, IEventInvoker
{
    public EventInvoker( IEvent @event, InvokerOptions options = default, IExpression? target = null ) : base( @event, options, target ) { }

    public object Add( object? handler ) => this.Add( CapturedUserExpression.Create( this.Compilation, handler ) );

    public object Add( IExpression handler )
    {
        // We might receive a null value as a result of incorrect selection of the overload when null is passed.
        this.EnsureHandlerNotNull( ref handler );

        return new DelegateUserExpression(
            context =>
            {
                var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventAddAccessor, context );

                return AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    eventAccess,
                    handler.ToExpressionSyntax( context, this.Member.Type ) );
            },
            this.Member.AddMethod.ReturnType );
    }

    public object Remove( object? handler ) => this.Remove( CapturedUserExpression.Create( this.Compilation, handler ) );

    public object Remove( IExpression handler )
    {
        // We might receive a null value as a result of incorrect selection of the overload when null is passed.
        this.EnsureHandlerNotNull( ref handler );

        return new DelegateUserExpression(
            context =>
            {
                var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventRemoveAccessor, context );

                return AssignmentExpression(
                    SyntaxKind.SubtractAssignmentExpression,
                    eventAccess,
                    handler.ToExpressionSyntax( context, this.Member.Type ) );
            },
            this.Member.RemoveMethod.ReturnType );
    }

    public object Raise( object?[]? args ) => this.Raise( CapturedUserExpression.Create( this.Compilation, args ) );

    private void EnsureHandlerNotNull( ref IExpression handler )
    {
        if ( handler == null! )
        {
            handler = new SyntaxUserExpression( SyntaxFactoryEx.Null, this.Member.Type );
        }
    }

    public object Raise( IExpression[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                var arguments = this.Member.GetArguments(
                    this.Member.Signature.Parameters,
                    args,
                    context );

                if ( context.AspectReferenceSyntaxProvider != null )
                {
                    var receiverInfo = this.GetReceiverInfo( context );
                    var argsTupleType = this.Member.Compilation.Factory.CreateTupleType( this.Member.RaiseMethod.Parameters );

                    return context.AspectReferenceSyntaxProvider.GetEventRaiseReference(
                        receiverInfo.AspectReferenceSpecification.AspectLayerId,
                        this.Member,
                        context.SyntaxGenerator,
                        argsTupleType,
                        arguments );
                }
                else
                {
                    var eventAccess = this.CreateEventExpression( AspectReferenceTargetKind.EventRaiseAccessor, context );

                    return ConditionalAccessExpression(
                        eventAccess,
                        InvocationExpression( MemberBindingExpression( IdentifierName( "Invoke" ) ) ).AddArgumentListArguments( arguments ) );
                }
            },
            this.Member.Signature.ReturnType );
    }

    public IEventInvoker WithOptions( InvokerOptions options ) => this.Options == options ? this : new EventInvoker( this.Member, options, this.Target );

    public IEventInvoker WithObject( IExpression? obj ) => this.IsSameTarget( obj ) ? this : new EventInvoker( this.Member, this.Options, obj );

    public IEventInvoker WithObject( object? obj )
        => this.IsSameTarget( obj ) ? this : this.WithObject( CapturedUserExpression.Create( this.Compilation, obj ) );

    IEventInvoker IEventInvoker.With( InvokerOptions options ) => this.WithOptions( options );

    IEventInvoker IEventInvoker.With( object? target, InvokerOptions options ) => this.WithOptions( options ).WithObject( target );

    private ExpressionSyntax CreateEventExpression( AspectReferenceTargetKind targetKind, SyntaxSerializationContext syntaxSerializationContext )
    {
        this.CheckInvocationOptionsAndTarget();

        var receiverInfo = this.GetReceiverInfo( syntaxSerializationContext );
        var name = SyntaxFactoryEx.SafeIdentifierName( this.GetCleanTargetMemberName() );

        var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Member, syntaxSerializationContext );

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