// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class MethodInvoker : Invoker<IMethod>, IMethodInvoker
{
    public MethodInvoker( IMethod method, InvokerOptions? options = default, object? target = null ) : base( method, options, target ) { }

    public object? Invoke( params object?[]? args )
    {
        // For some reason, overload resolution chooses the wrong overload in the template,
        // so redirect to the correct one.
        if ( args is [IEnumerable<IExpression> expressionArgs] )
        {
            return this.Invoke( expressionArgs );
        }

        args ??= [];

        var parametersCount = this.Member.Parameters.Count;

        if ( parametersCount > 0 && this.Member.Parameters[parametersCount - 1].IsParams )
        {
            // The this.Declaration has a 'params' param.
            if ( args.Length < parametersCount - 1 )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresAtLeastNArguments.CreateException( (this.Member, parametersCount - 1, args.Length) );
            }
        }
        else if ( args.Length != parametersCount )
        {
            throw GeneralDiagnosticDescriptors.MemberRequiresNArguments.CreateException( (this.Member, parametersCount, args.Length) );
        }

        this.CheckInvocationOptionsAndTarget();

        switch ( this.Member.MethodKind )
        {
            case MethodKind.Default:
            case MethodKind.LocalFunction:
            case MethodKind.ExplicitInterfaceImplementation:
                return this.InvokeDefaultMethod( args );

            case MethodKind.EventAdd:
                return ((IEvent) this.Member.DeclaringMember!).With( this.Target, this.Options ).Add( args[0] );

            case MethodKind.EventRaise:
                return ((IEvent) this.Member.DeclaringMember!).With( this.Target, this.Options ).Raise( args );

            case MethodKind.EventRemove:
                return ((IEvent) this.Member.DeclaringMember!).With( this.Target, this.Options ).Remove( args[0] );

            case MethodKind.PropertyGet:
                switch ( this.Member.DeclaringMember )
                {
                    case IProperty property:
                        return property.With( this.Target, this.Options ).Value;

                    case IIndexer indexer:
                        return indexer.With( this.Target, this.Options ).GetValue( args );

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertyGet: '{this.Member.DeclaringMember}'." );
                }

            case MethodKind.PropertySet:
                switch ( this.Member.DeclaringMember )
                {
                    case IProperty property:
                        ((FieldOrPropertyInvoker) property.With( this.Target, this.Options )).SetValue( args[0] );

                        return null;

                    case IIndexer indexer:
                        indexer.With( this.Options ).SetValue( this.Target, args );

                        return null;

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertySet: '{this.Member.DeclaringMember}'." );
                }

            default:
                throw new NotImplementedException(
                    $"Cannot generate syntax to invoke the this.Declaration '{this.Member}' because this.Declaration kind {this.Member.MethodKind} is not implemented." );
        }
    }

    public object? Invoke( IEnumerable<IExpression> args ) => this.Invoke( args.ToArray<object>() );

    private DelegateUserExpression InvokeDefaultMethod( object?[] args )
    {
        return new DelegateUserExpression(
            context =>
            {
                SimpleNameSyntax name;

                var receiverInfo = this.GetReceiverInfo( context );

                if ( this.Member.IsGeneric )
                {
                    name = GenericName(
                        Identifier( this.GetCleanTargetMemberName() ),
                        TypeArgumentList( SeparatedList( this.Member.TypeArguments.SelectAsImmutableArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
                }
                else
                {
                    name = IdentifierName( this.GetCleanTargetMemberName() );
                }

                var arguments = this.Member.GetArguments(
                    this.Member.Parameters,
                    TypedExpressionSyntaxImpl.FromValues( args, context ),
                    context.SyntaxGenerationContext );

                if ( this.Member.MethodKind == MethodKind.LocalFunction )
                {
                    if ( receiverInfo.Syntax.Kind() != SyntaxKind.NullLiteralExpression )
                    {
                        throw GeneralDiagnosticDescriptors.CannotProvideInstanceForLocalFunction.CreateException( this.Member );
                    }

                    return this.CreateInvocationExpression(
                        receiverInfo.ToReceiverExpressionSyntax(),
                        name,
                        arguments,
                        AspectReferenceTargetKind.Self,
                        context );
                }
                else
                {
                    var receiver = receiverInfo.WithSyntax( this.Member.GetReceiverSyntax( receiverInfo.TypedExpressionSyntax, context ) );

                    return this.CreateInvocationExpression( receiver, name, arguments, AspectReferenceTargetKind.Self, context );
                }
            },
            (this.Options & InvokerOptions.NullConditional) != 0 ? this.Member.ReturnType.ToNullable() : this.Member.ReturnType );
    }

    private ExpressionSyntax CreateInvocationExpression(
        ReceiverExpressionSyntax receiverTypedExpressionSyntax,
        SimpleNameSyntax name,
        ArgumentSyntax[]? arguments,
        AspectReferenceTargetKind targetKind,
        SyntaxSerializationContext context )
    {
        if ( !receiverTypedExpressionSyntax.RequiresNullConditionalAccessMember )
        {
            ExpressionSyntax memberAccessExpression =
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiverTypedExpressionSyntax.Syntax, name )
                    .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );

            // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or its declaring type).
            if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
            {
                memberAccessExpression =
                    memberAccessExpression.WithAspectReferenceAnnotation(
                        receiverTypedExpressionSyntax.AspectReferenceSpecification.WithTargetKind( targetKind ) );
            }

            return
                arguments != null
                    ? InvocationExpression(
                        memberAccessExpression,
                        ArgumentList( SeparatedList( arguments ) ) )
                    : InvocationExpression( memberAccessExpression );
        }
        else
        {
            var expression =
                arguments != null
                    ? ConditionalAccessExpression(
                        receiverTypedExpressionSyntax.Syntax,
                        InvocationExpression(
                            MemberBindingExpression( name ),
                            ArgumentList( SeparatedList( arguments ) ) ) )
                    : ConditionalAccessExpression(
                        receiverTypedExpressionSyntax.Syntax,
                        InvocationExpression( MemberBindingExpression( name ) ) );

            // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or its declaring type).
            if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
            {
                expression = expression.WithAspectReferenceAnnotation(
                    receiverTypedExpressionSyntax.AspectReferenceSpecification.WithTargetKind( targetKind ) );
            }

            return expression;
        }
    }

    public IMethodInvoker With( InvokerOptions options ) => this.Options == options ? this : new MethodInvoker( this.Member, options );

    public IMethodInvoker With( object? target, InvokerOptions options = default )
        => this.Target == target && this.Options == options ? this : new MethodInvoker( this.Member, options, target );

    public IMethodInvoker With( IExpression target, InvokerOptions options = default ) => new MethodInvoker( this.Member, options, target );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => this.InvokeDefaultMethod( args.ToArray<object>() );
}