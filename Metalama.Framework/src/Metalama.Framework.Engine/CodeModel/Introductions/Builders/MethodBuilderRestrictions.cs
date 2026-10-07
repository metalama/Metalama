// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Validates the changes that the code that receives a method builder makes to the method, its parameters, its type parameters and their
/// attributes. The creator of the builder derives a class from this one and gives an instance to the builder.
/// </summary>
/// <remarks>
/// <para>
/// The builder calls the <c>Validate*</c> method that corresponds to a change before it makes the change, and only when the new value differs
/// from the current one. A method refuses the change by throwing an exception, normally an <see cref="InvalidOperationException"/>, so the code
/// that makes the change receives the error at the statement that makes it. The default implementation of every method accepts the change.
/// </para>
/// <para>
/// <see cref="Extensibility.CallSites.ExtensionTransformationFactory.CreateMethodBuilder"/> gives the restrictions to the builder after the
/// initial signature is set, so the initialization of the builder is never validated. <see cref="LockedSignatureRestrictions"/> is an
/// implementation that locks parts of the signature.
/// </para>
/// </remarks>
[PublicAPI]
public abstract class MethodBuilderRestrictions
{
    /// <summary>
    /// Validates a change of the name of the method.
    /// </summary>
    public virtual void ValidateName( IMethodBuilder method, string name ) { }

    /// <summary>
    /// Validates a change of the accessibility of the method.
    /// </summary>
    public virtual void ValidateAccessibility( IMethodBuilder method, Accessibility accessibility ) { }

    /// <summary>
    /// Validates a change of <see cref="IMemberOrNamedType.IsStatic"/>.
    /// </summary>
    public virtual void ValidateIsStatic( IMethodBuilder method, bool isStatic ) { }

    /// <summary>
    /// Validates a change of a Boolean modifier of the method other than <see cref="IMemberOrNamedType.IsStatic"/>, for instance
    /// <see cref="IMember.IsVirtual"/>, <see cref="IMember.IsAsync"/> or <see cref="IMethod.IsReadOnly"/>.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="propertyName">The name of the property of <see cref="IMethodBuilder"/> that changes.</param>
    /// <param name="value">The new value.</param>
    public virtual void ValidateModifier( IMethodBuilder method, string propertyName, bool value ) { }

    /// <summary>
    /// Validates a change of <see cref="IMethod.OperatorKind"/>, which also changes the name of the method.
    /// </summary>
    public virtual void ValidateOperatorKind( IMethodBuilder method, OperatorKind operatorKind ) { }

    /// <summary>
    /// Validates a change of the return type of the method.
    /// </summary>
    public virtual void ValidateReturnType( IMethodBuilder method, IType type ) { }

    /// <summary>
    /// Validates the addition of a parameter, at the end of the list or at a given position.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="index">The position of the new parameter. The parameters at this position and after it move by one position.</param>
    /// <param name="name">The name of the new parameter.</param>
    /// <param name="type">The type of the new parameter.</param>
    /// <param name="refKind">The reference kind of the new parameter.</param>
    public virtual void ValidateAddParameter( IMethodBuilder method, int index, string name, IType type, RefKind refKind ) { }

    /// <summary>
    /// Validates a change of the name of a parameter.
    /// </summary>
    public virtual void ValidateParameterName( IParameterBuilder parameter, string name ) { }

    /// <summary>
    /// Validates a change of the type of a parameter. A change of the return type calls <see cref="ValidateReturnType"/> instead.
    /// </summary>
    public virtual void ValidateParameterType( IParameterBuilder parameter, IType type ) { }

    /// <summary>
    /// Validates a change of the reference kind of a parameter, including the return parameter.
    /// </summary>
    public virtual void ValidateParameterRefKind( IParameterBuilder parameter, RefKind refKind ) { }

    /// <summary>
    /// Validates a change of the default value of a parameter.
    /// </summary>
    public virtual void ValidateParameterDefaultValue( IParameterBuilder parameter, TypedConstant? defaultValue ) { }

    /// <summary>
    /// Validates a change of the <c>params</c> modifier of a parameter.
    /// </summary>
    public virtual void ValidateParameterIsParams( IParameterBuilder parameter, bool isParams ) { }

    /// <summary>
    /// Validates a change of the <c>this</c> modifier of a parameter.
    /// </summary>
    public virtual void ValidateParameterIsThis( IParameterBuilder parameter, bool isThis ) { }

    /// <summary>
    /// Validates the addition of a type parameter at the end of the list of type parameters.
    /// </summary>
    public virtual void ValidateAddTypeParameter( IMethodBuilder method, string name ) { }

    /// <summary>
    /// Validates a change of a type parameter: its name, its variance, one of its constraints, or the addition of a type constraint.
    /// </summary>
    /// <param name="typeParameter">The type parameter.</param>
    /// <param name="propertyName">The name of the property of <see cref="ITypeParameterBuilder"/> that changes, or
    /// <see cref="ITypeParameter.TypeConstraints"/> when a type constraint is added.</param>
    public virtual void ValidateTypeParameterChange( ITypeParameterBuilder typeParameter, string propertyName ) { }

    /// <summary>
    /// Validates the addition of an attribute to the method, to one of its parameters, to its return parameter or to one of its type parameters.
    /// </summary>
    public virtual void ValidateAddAttribute( IDeclarationBuilder declaration, AttributeConstruction attribute ) { }

    /// <summary>
    /// Validates the removal of the attributes of a type from the method, from one of its parameters, from its return parameter or from one of
    /// its type parameters.
    /// </summary>
    public virtual void ValidateRemoveAttributes( IDeclarationBuilder declaration, INamedType attributeType ) { }

    /// <summary>
    /// Creates the exception that refuses a change of a locked part of a method, with a message that names the part and the method.
    /// </summary>
    /// <param name="method">The method that is changed, or that declares the parameter or type parameter that is changed.</param>
    /// <param name="part">The part that cannot change, for instance <c>return type</c> or <c>reference kind of the parameter 'x'</c>.</param>
    protected static InvalidOperationException CreateLockedException( IMethod method, string part )
        => new( $"The {part} of the method '{method.Name}' cannot be changed." );
}
