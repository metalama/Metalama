// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal class FieldOrPropertyInvoker : Invoker<IFieldOrProperty>, IFieldOrPropertyInvoker, IUserExpression
{
    public FieldOrPropertyInvoker(
        IFieldOrProperty fieldOrProperty,
        InvokerOptions options = default,
        IExpression? target = null ) : base(
        fieldOrProperty,
        options,
        target ) { }

    private ExpressionSyntax CreatePropertyExpression( AspectReferenceTargetKind targetKind, SyntaxSerializationContext context )
    {
        this.CheckInvocationOptionsAndTarget();

#if ROSLYN_5_0_0_OR_GREATER

        // For extension properties, redirect to the implementation method.
        if ( this.IsExtensionMember && this.Member is IProperty property )
        {
            return this.CreateExtensionPropertyExpression( property, targetKind, context );
        }
#endif

        var receiverInfo = this.GetReceiverInfo( context );

        var name = SyntaxFactoryEx.SafeIdentifierName( this.GetCleanTargetMemberName() );

        var receiverSyntax = receiverInfo.GetReceiverSyntax( this.Member, context );

        ExpressionSyntax expression;

        if ( !receiverInfo.RequiresConditionalAccess )
        {
            expression = MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiverSyntax, name )
                .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
        }
        else
        {
            expression = ConditionalAccessExpression( receiverSyntax, MemberBindingExpression( name ) );
        }

        // Only create an aspect reference when the declaring type of the invoked declaration is ancestor of the target of the template (or it's declaring type).
        if ( GetTargetType()?.IsConvertibleTo( this.Member.DeclaringType ) ?? false )
        {
            expression = expression.WithAspectReferenceAnnotation( receiverInfo.AspectReferenceSpecification.WithTargetKind( targetKind ) );
        }

        return expression;
    }

#if ROSLYN_5_0_0_OR_GREATER
    private ExpressionSyntax CreateExtensionPropertyExpression( IProperty property, AspectReferenceTargetKind targetKind, SyntaxSerializationContext context )
    {
        // Get the appropriate accessor's implementation method.
        var accessor = targetKind == AspectReferenceTargetKind.PropertySetAccessor
            ? property.SetMethod
            : property.GetMethod;

        if ( accessor == null )
        {
            throw new InvalidOperationException( $"Cannot access extension property '{property}' because the required accessor is not available." );
        }

        var implMethod = accessor.ExtensionImplementationMethod;

        if ( implMethod == null )
        {
            throw new InvalidOperationException( $"Cannot access extension property '{property}' because its implementation method was not found." );
        }

        // Create an invoker for the implementation method (which is a regular static method).
        // Pass the receiver as the target for instance properties - the invoker will handle it.
        var implInvoker = new MethodInvoker( implMethod, this.Options, property.IsStatic ? null : this.Target );

        return implInvoker.CreateInvokeExpression( Array.Empty<IExpression>() ).ToTypedExpressionSyntax( context ).Syntax;
    }
#endif

    IType IHasType.Type => this.Member.Type;

    RefKind IHasType.RefKind => this.Member.RefKind;

    bool IExpression.IsAssignable => this.Member.IsAssignable;

    public object SetValue( object? value )
    {
#if ROSLYN_5_0_0_OR_GREATER

        // For extension properties, generate a call to the setter implementation method.
        if ( this.IsExtensionMember && this.Member is IProperty property )
        {
            return this.SetExtensionPropertyValue( property, value );
        }
#endif

        return new DelegateUserExpression(
            context =>
            {
                var propertyAccess = this.CreatePropertyExpression( AspectReferenceTargetKind.PropertySetAccessor, context );

                return AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    propertyAccess,
                    TypedExpressionSyntaxImpl.GetSyntaxFromValue( value, context ) );
            },
            this.Member.Type );
    }

#if ROSLYN_5_0_0_OR_GREATER
    private DelegateUserExpression SetExtensionPropertyValue( IProperty property, object? value )
    {
        var setter = property.SetMethod;

        if ( setter == null )
        {
            throw new InvalidOperationException( $"Cannot set extension property '{property}' because it has no setter." );
        }

        var implMethod = setter.ExtensionImplementationMethod;

        if ( implMethod == null )
        {
            throw new InvalidOperationException( $"Cannot set extension property '{property}' because its setter implementation method was not found." );
        }

        // Create an invoker for the implementation method (which is a regular static method).
        // Pass the receiver as the target for instance properties - the invoker will handle it.
        var implInvoker = new MethodInvoker( implMethod, this.Options, property.IsStatic ? null : this.Target );

        // Invoke with the value as argument.
        return (DelegateUserExpression) implInvoker.CreateInvokeExpression( new[] { CapturedUserExpression.Create( this.Compilation, value ) } );
    }
#endif

    public ref object? Value
        => ref RefHelper.Wrap(
            new DelegateUserExpression(
                context => this.CreatePropertyExpression( AspectReferenceTargetKind.Self, context ),
                (this.Options & InvokerOptions.NullabilityMask) == InvokerOptions.NullConditional ? this.Member.Type.ToNullable() : this.Member.Type,
                this.IsRef(),
                this.Member.Writeability != Writeability.None ) );

    protected virtual IFieldOrPropertyInvoker CreateInvoker( IFieldOrProperty fieldOrProperty, InvokerOptions options, IExpression? target )
        => new FieldOrPropertyInvoker( fieldOrProperty, options, target );

    public IFieldOrPropertyInvoker WithOptions( InvokerOptions options )
        => this.Options == options ? this : this.CreateInvoker( this.Member, options, this.Target );

    public IFieldOrPropertyInvoker WithObject( IExpression? target )
        => this.IsSameTarget( target ) ? this : this.CreateInvoker( this.Member, this.Options, target );

    public IFieldOrPropertyInvoker WithObject( object? target )
        => this.IsSameTarget( target ) ? this : this.WithObject( CapturedUserExpression.Create( this.Compilation, target ) );

    IFieldOrPropertyInvoker IFieldOrPropertyInvoker.With( InvokerOptions options ) => this.WithOptions( options );

    IFieldOrPropertyInvoker IFieldOrPropertyInvoker.With( object? target, InvokerOptions options ) => this.WithOptions( options ).WithObject( target );

    private DelegateUserExpression GetUserExpression()
        => new(
            context => this.CreatePropertyExpression( AspectReferenceTargetKind.PropertyGetAccessor, context ),
            this.Member.Type,
            this.IsRef() );

    private bool IsRef() => this.Member.DeclarationKind is DeclarationKind.Field || this.Member.RefKind is RefKind.Ref;

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
    {
        return this.GetUserExpression().ToTypedExpressionSyntax( syntaxGenerationContext, targetType );
    }
}