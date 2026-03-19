// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal abstract partial class Invoker<T>
    where T : IMember
{
    protected InvokerOptions Options { get; }

    protected IExpression? Target { get; }

    protected Invoker( T member, InvokerOptions options, IExpression? target )
    {
        this.Options = options;
        this.Target = target;
        this.Member = member;
    }

    /// <summary>
    /// Gets a value indicating whether the member is declared in an extension block.
    /// </summary>
    protected bool IsExtensionMember => this.Member.DeclaringType.TypeKind == TypeKind.Extension;

    // Note that the result of this method indirectly depends on the execution context, so it cannot be cached.
    private AspectReferenceOrder GetAspectReferenceOrder()
    {
        var isSelfTarget = this.Target is null or InstanceUserReceiver or ThisTypeUserReceiver;

        var orderOptions = GetOrderOptions( this.Member, this.Options, isSelfTarget );

        return orderOptions switch
        {
            InvokerOptions.Current => AspectReferenceOrder.Current,
            InvokerOptions.Base => AspectReferenceOrder.Base,
            InvokerOptions.Final => AspectReferenceOrder.Final,
            _ => throw new AssertionFailedException( $"Invalid value: {this.Options}." )
        };
    }

    private static InvokerOptions GetOrderOptions( IMember member, InvokerOptions options, bool isSelfTarget )
    {
        options &= InvokerOptions.OrderMask;

        if ( options != InvokerOptions.Default )
        {
            return options;
        }
        else if ( isSelfTarget && TemplateExpansionContext.IsTransformingDeclaration( member ) )
        {
            // When we expand a template, the default invoker for the declaration being overridden or introduced is
            // always the base one.

            return InvokerOptions.Base;
        }
        else
        {
            return InvokerOptions.Final;
        }
    }

    protected T Member { get; }

    protected ICompilation Compilation => this.Member.Compilation;

    internal readonly record struct ReceiverExpressionSyntax(
        ExpressionSyntax Syntax,
        bool RequiresNullConditionalAccessMember,
        AspectReferenceSpecification AspectReferenceSpecification );

    internal virtual string GetCleanTargetMemberName()
    {
        var definition = this.Member.Definition;

        return
            definition.IsExplicitInterfaceImplementation
                ? definition.GetExplicitInterfaceImplementation().Name
                : definition.Name;
    }

    internal ReceiverTypedExpressionSyntax GetReceiverInfo( SyntaxSerializationContext syntaxSerializationContext )
    {
        var order = this.GetAspectReferenceOrder();

        if ( this.Target is UserReceiver receiver )
        {
            receiver = receiver.WithAspectReferenceOrder( order );

            return new ReceiverTypedExpressionSyntax(
                receiver.ToTypedExpressionSyntax( syntaxSerializationContext ),
                this.Options,
                receiver.AspectReferenceSpecification );
        }
        else
        {
            // CurrentAspectLayerId may be null when we are not executing in a template execution context.
            var aspectReferenceSpecification =
                new AspectReferenceSpecification(
                    TemplateExpansionContext.CurrentAspectLayerId ?? default,
                    order,
                    flags: this.Target == null ? AspectReferenceFlags.None : AspectReferenceFlags.CustomReceiver );

            if ( this.Target != null )
            {
                var typedExpressionSyntax = TypedExpressionSyntaxImpl.FromValue( this.Target, syntaxSerializationContext );

                return new ReceiverTypedExpressionSyntax(
                    typedExpressionSyntax,
                    this.Options,
                    aspectReferenceSpecification );
            }
            else if ( this.Member.IsStatic )
            {
                return new ReceiverTypedExpressionSyntax(
                    new ThisTypeUserReceiver( this.Member.DeclaringType, aspectReferenceSpecification )
                        .ToTypedExpressionSyntax( syntaxSerializationContext ),
                    InvokerOptions.Default,
                    aspectReferenceSpecification );
            }
            else
            {
                ThisInstanceUserReceiver.TryCreate( syntaxSerializationContext.CurrentDeclaration, aspectReferenceSpecification, true, out var thisReceiver );

                return new ReceiverTypedExpressionSyntax(
                    thisReceiver.AssertNotNull().ToTypedExpressionSyntax( syntaxSerializationContext ),
                    InvokerOptions.Default,
                    aspectReferenceSpecification );
            }
        }
    }

    internal static INamedType? GetTargetType()
        => TemplateExpansionContext.CurrentTargetDeclaration?.DeclarationKind switch
        {
            DeclarationKind.NamedType or DeclarationKind.ExtensionBlock when TemplateExpansionContext.CurrentTargetDeclaration is INamedType type => type,
            { IsMember: true } when TemplateExpansionContext.CurrentTargetDeclaration is IMember member => member.DeclaringType,
            DeclarationKind.Parameter when TemplateExpansionContext.CurrentTargetDeclaration is IParameter parameter => parameter.DeclaringMember
                .AssertNotNull()
                .DeclaringType,
            null => null,
            _ => throw new AssertionFailedException( $"Unexpected target declaration: '{TemplateExpansionContext.CurrentTargetDeclaration}'." )
        };

    protected void CheckInvocationOptionsAndTarget()
    {
        // Specifying Base or Current option with non-default target is only allowed when the method is in the inheritance hierarchy of the template target.
        if ( this.Target != null && (this.Options & InvokerOptions.OrderMask) is InvokerOptions.Base or InvokerOptions.Current &&
             !(GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false) )
        {
            throw GeneralDiagnosticDescriptors.CantInvokeBaseOrCurrentOutsideTargetType.CreateException(
                (this.Member, GetTargetType()!, this.Options & InvokerOptions.OrderMask) );
        }
    }

    protected bool IsSameTarget( object? target ) => ReferenceEquals( target, this.Target ) || (target != null && target.Equals( this.Target ));

    public override string ToString() => $"{this.GetType().Name} Member={{{this.Member}}}, Options={{{this.Options}}}";
}