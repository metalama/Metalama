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
    private readonly AspectReferenceOrder _order;

    protected InvokerOptions Options { get; }

    protected IExpression? Target { get; }

    protected Invoker( T member, InvokerOptions? options, IExpression? target )
    {
        options ??= InvokerOptions.Default;

        var isSelfTarget = target is null or ThisInstanceUserReceiver or ThisTypeUserReceiver;

        var orderOptions = GetOrderOptions( member, options.Value, isSelfTarget );

        var otherFlags = options.Value & ~InvokerOptions.OrderMask;

        this.Options = orderOptions | otherFlags;

        this.Target = target;
        this.Member = member;

        this._order = orderOptions switch
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

    protected readonly record struct ReceiverExpressionSyntax(
        ExpressionSyntax Syntax,
        bool RequiresNullConditionalAccessMember,
        AspectReferenceSpecification AspectReferenceSpecification );

    private AspectReferenceSpecification GetDefaultAspectReferenceSpecification()

        // CurrentAspectLayerId may be null when we are not executing in a template execution context.
        => new(
            TemplateExpansionContext.CurrentAspectLayerId ?? default,
            this._order,
            flags: this.Target == null ? AspectReferenceFlags.None : AspectReferenceFlags.CustomReceiver );

    protected string GetCleanTargetMemberName()
    {
        var definition = this.Member.Definition;

        return
            definition.IsExplicitInterfaceImplementation
                ? definition.GetExplicitInterfaceImplementation().Name
                : definition.Name;
    }

    protected ReceiverTypedExpressionSyntax GetReceiverInfo( SyntaxSerializationContext syntaxSerializationContext )
    {
        if ( this.Target is UserReceiver receiver )
        {
            receiver = receiver.WithAspectReferenceOrder( this._order );

            return new ReceiverTypedExpressionSyntax(
                receiver.ToTypedExpressionSyntax( syntaxSerializationContext ),
                this.Options,
                receiver.AspectReferenceSpecification );
        }
        else
        {
            var aspectReferenceSpecification = this.GetDefaultAspectReferenceSpecification();

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
                return new ReceiverTypedExpressionSyntax(
                    new ThisInstanceUserReceiver( this.Member.DeclaringType, aspectReferenceSpecification ).ToTypedExpressionSyntax(
                        syntaxSerializationContext ),
                    InvokerOptions.Default,
                    aspectReferenceSpecification );
            }
        }
    }

    protected static INamedType? GetTargetType()
        => TemplateExpansionContext.CurrentTargetDeclaration switch
        {
            INamedType type => type,
            IMember member => member.DeclaringType,
            IParameter parameter => parameter.DeclaringMember.AssertNotNull().DeclaringType,
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

    public override string ToString() => $"{this.GetType().Name} Member={{{this.Member}}}, Options={{{this.Options}}}";
}