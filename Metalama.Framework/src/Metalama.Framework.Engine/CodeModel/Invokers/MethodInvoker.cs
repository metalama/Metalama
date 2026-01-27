// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
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
    public MethodInvoker( IMethod method, InvokerOptions options = default, IExpression? target = null ) : base( method, options, target ) { }

    public object? Invoke( IEnumerable<IExpression> args ) => this.InvokeCore( args.ToReadOnlyList() );

    private object? InvokeCore( IReadOnlyList<IExpression> args )
    {
        var parametersCount = this.Member.Parameters.Count;

        if ( parametersCount > 0 && this.Member.Parameters[parametersCount - 1].IsParams )
        {
            // The this.Declaration has a 'params' param.
            if ( args.Count < parametersCount - 1 )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresAtLeastNArguments.CreateException( (this.Member, parametersCount - 1, args.Count) );
            }
        }
        else if ( args.Count != parametersCount )
        {
            throw GeneralDiagnosticDescriptors.MemberRequiresNArguments.CreateException( (this.Member, parametersCount, args.Count) );
        }

        this.CheckInvocationOptionsAndTarget();

        switch ( this.Member.MethodKind )
        {
            case MethodKind.Default:
            case MethodKind.LocalFunction:
            case MethodKind.ExplicitInterfaceImplementation:
                return this.InvokeDefaultMethod( args );

            case MethodKind.EventAdd:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Add( args[0] );

            case MethodKind.EventRaise:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Raise( args );

            case MethodKind.EventRemove:
                return ((IEvent) this.Member.DeclaringMember!).WithObject( this.Target ).WithOptions( this.Options ).Remove( args[0] );

            case MethodKind.PropertyGet:
                switch ( this.Member.DeclaringMember )
                {
                    case IProperty property:
                        return property.WithObject( this.Target ).WithOptions( this.Options ).Value;

                    case IIndexer indexer:
                        return indexer.WithObject( this.Target! ).WithOptions( this.Options )[args];

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertyGet: '{this.Member.DeclaringMember}'." );
                }

            case MethodKind.PropertySet:
                switch ( this.Member.DeclaringMember )
                {
                    case IProperty property:
                        ((FieldOrPropertyInvoker) property.WithObject( this.Target ).WithOptions( this.Options )).SetValue( args[0] );

                        return null;

                    case IIndexer indexer:
                        ((IndexerInvoker) indexer.WithObject( this.Target! ).WithOptions( this.Options )).SetValue( this.Target, args );

                        return null;

                    default:
                        throw new AssertionFailedException( $"Unexpected declaration for a PropertySet: '{this.Member.DeclaringMember}'." );
                }

            default:
                throw new NotImplementedException(
                    $"Cannot generate syntax to invoke the this.Declaration '{this.Member}' because this.Declaration kind {this.Member.MethodKind} is not implemented." );
        }
    }

    public object? Invoke( IEnumerable<object?> args ) => this.Invoke( args.ToArray() );

    public object? Invoke( object?[]? args )
    {
        // For some reason, overload resolution chooses the wrong overload in the template,
        // so redirect to the correct one.
        if ( args is [IEnumerable<IExpression> expressionArgs] )
        {
            return this.InvokeCore( expressionArgs.ToReadOnlyList() );
        }

        return this.InvokeCore( CapturedUserExpression.Create( this.Compilation, args ) );
    }

    private DelegateUserExpression InvokeDefaultMethod( IReadOnlyList<IExpression> args )
    {
#if ROSLYN_5_0_0_OR_GREATER

        // For extension members, redirect to the implementation method.
        if ( this.IsExtensionMember )
        {
            return this.InvokeExtensionImplementationMethod( args );
        }
#endif

        return new DelegateUserExpression(
            context =>
            {
                SimpleNameSyntax name;

                var receiverInfo = this.GetReceiverInfo( context );

                if ( this.Member.IsGeneric )
                {
                    name = GenericName(
                        SyntaxFactoryEx.SafeIdentifier( this.GetCleanTargetMemberName() ),
                        TypeArgumentList( SeparatedList( this.Member.TypeArguments.SelectAsImmutableArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
                }
                else
                {
                    name = SyntaxFactoryEx.SafeIdentifierName( this.GetCleanTargetMemberName() );
                }

                var arguments = this.Member.GetArguments( this.Member.Parameters, args, context );

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
                    var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Member, context );
                    var receiver = receiverInfo.WithSyntax( receiverSyntax );

                    return this.CreateInvocationExpression( receiver, name, arguments, AspectReferenceTargetKind.Self, context );
                }
            },
            (this.Options & InvokerOptions.NullConditional) != 0 ? this.Member.ReturnType.ToNullable() : this.Member.ReturnType );
    }

#if ROSLYN_5_0_0_OR_GREATER
    private DelegateUserExpression InvokeExtensionImplementationMethod( IReadOnlyList<IExpression> args )
    {
        var implMethod = this.Member.ExtensionImplementationMethod;

        if ( implMethod == null )
        {
            throw new InvalidOperationException( $"Cannot invoke extension member '{this.Member}' because its implementation method was not found." );
        }

        // Create an invoker for the implementation method (which is a regular static method).
        // Pass the receiver as the target for instance methods - the invoker will handle it.
        var implInvoker = new MethodInvoker( implMethod, this.Options, this.Member.IsStatic ? null : this.Target );

        // Invoke with the original arguments.
        return (DelegateUserExpression) implInvoker.CreateInvokeExpression( args );
    }
#endif

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

    public IMethodInvoker WithOptions( InvokerOptions options ) => this.Options == options ? this : new MethodInvoker( this.Member, options, this.Target );

    public IMethodInvoker WithObject( object? obj )
        => this.IsSameTarget( obj ) ? this : this.WithObject( CapturedUserExpression.Create( this.Compilation, obj ) );

    public IMethodInvoker WithObject( IExpression? obj ) => this.IsSameTarget( obj ) ? this : new MethodInvoker( this.Member, this.Options, obj );

    IMethodInvoker IMethodInvoker.With( InvokerOptions options ) => this.WithOptions( options );

    IMethodInvoker IMethodInvoker.With( object? obj, InvokerOptions options ) => this.WithOptions( options ).WithObject( obj );

    public IExpression CreateInvokeExpression( IEnumerable<IExpression> args ) => this.InvokeDefaultMethod( args.ToArray() );

    public IExpression CreateInvokeExpression( params IEnumerable<object?> args )
        => this.CreateInvokeExpression( args.Select( a => CapturedUserExpression.Create( this.Compilation, a ) ) );
}