// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// An implementation of <see cref="MethodBuilderRestrictions"/> that locks parts of the signature of the method: its name, its return type, its
/// leading parameters and its leading type parameters.
/// </summary>
/// <remarks>
/// <para>
/// An extension that gives a pre-filled builder to user code, for instance the <c>configure</c> delegate of an interceptor, locks the parts of
/// the signature on which its own code depends. A change of a locked part throws an <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The parameters of a builder cannot be removed, so the locked leading parameters are always present. A parameter cannot be inserted before
/// or between them either, so they keep their positions.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class LockedSignatureRestrictions : MethodBuilderRestrictions
{
    /// <summary>
    /// Gets the number of leading parameters whose reference kind and position cannot change. Their name and their type can change.
    /// </summary>
    public int LockedLeadingParameterCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the return type, and the reference kind of the return parameter, cannot change.
    /// </summary>
    public bool IsReturnTypeLocked { get; init; }

    /// <summary>
    /// Gets the number of leading type parameters that cannot be renamed and whose constraints cannot be removed or relaxed. Constraints can be
    /// added to them. Type parameters can be added after them, unless <see cref="AreNewTypeParametersRefused"/> is <c>true</c>.
    /// </summary>
    public int LockedTypeParameterCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether type parameters cannot be added.
    /// </summary>
    public bool AreNewTypeParametersRefused { get; init; }

    /// <summary>
    /// Gets a value indicating whether the name of the method cannot change. The factory can still add a numeric suffix to make it unique.
    /// </summary>
    public bool IsNameLocked { get; init; }

    /// <inheritdoc />
    public override void ValidateName( IMethodBuilder method, string name )
    {
        if ( this.IsNameLocked )
        {
            throw CreateLockedException( method, "name" );
        }
    }

    /// <inheritdoc />
    public override void ValidateOperatorKind( IMethodBuilder method, OperatorKind operatorKind )
    {
        if ( this.IsNameLocked )
        {
            throw CreateLockedException( method, "name" );
        }
    }

    /// <inheritdoc />
    public override void ValidateReturnType( IMethodBuilder method, IType type )
    {
        if ( this.IsReturnTypeLocked )
        {
            throw CreateLockedException( method, "return type" );
        }
    }

    /// <inheritdoc />
    public override void ValidateAddParameter( IMethodBuilder method, int index, string name, IType type, RefKind refKind )
    {
        if ( index < this.LockedLeadingParameterCount )
        {
            throw CreateLockedException( method, "position of the leading parameters" );
        }
    }

    /// <inheritdoc />
    public override void ValidateParameterRefKind( IParameterBuilder parameter, RefKind refKind )
    {
        var method = (IMethod) parameter.DeclaringMember.AssertNotNull();

        if ( parameter.IsReturnParameter )
        {
            if ( this.IsReturnTypeLocked )
            {
                throw CreateLockedException( method, "reference kind of the return parameter" );
            }
        }
        else if ( parameter.Index < this.LockedLeadingParameterCount )
        {
            throw CreateLockedException( method, $"reference kind of the parameter '{parameter.Name}'" );
        }
    }

    /// <inheritdoc />
    public override void ValidateAddTypeParameter( IMethodBuilder method, string name )
    {
        if ( this.AreNewTypeParametersRefused )
        {
            throw CreateLockedException( method, "type parameters" );
        }
    }

    /// <inheritdoc />
    public override void ValidateTypeParameterName( ITypeParameterBuilder typeParameter, string name )
    {
        if ( typeParameter.Index < this.LockedTypeParameterCount )
        {
            throw CreateLockedException( (IMethod) typeParameter.ContainingDeclaration!, $"name of the type parameter '{typeParameter.Name}'" );
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A locked type parameter can gain constraints: <see cref="ITypeParameterBuilder.TypeKindConstraint"/> can be set when it is
    /// <see cref="TypeKindConstraint.None"/>, <see cref="ITypeParameterBuilder.HasDefaultConstructorConstraint"/> can become <c>true</c>, and
    /// <see cref="ITypeParameterBuilder.AllowsRefStruct"/> can become <c>false</c>. A change that removes or relaxes a constraint, and a change of
    /// <see cref="ITypeParameterBuilder.IsConstraintNullable"/> or <see cref="ITypeParameterBuilder.Variance"/>, is refused.
    /// </remarks>
    public override void ValidateTypeParameterConstraintChange( ITypeParameterBuilder typeParameter, string propertyName, object? value )
    {
        if ( typeParameter.Index >= this.LockedTypeParameterCount )
        {
            return;
        }

        var isAddition = propertyName switch
        {
            nameof(ITypeParameterBuilder.TypeKindConstraint) => typeParameter.TypeKindConstraint == TypeKindConstraint.None,
            nameof(ITypeParameterBuilder.HasDefaultConstructorConstraint) => value is true,
            nameof(ITypeParameterBuilder.AllowsRefStruct) => value is false,
            _ => false
        };

        if ( !isAddition )
        {
            var method = (IMethod) typeParameter.ContainingDeclaration!;

            throw new InvalidOperationException(
                $"The {propertyName} property of the type parameter '{typeParameter.Name}' of the method '{method.Name}' cannot be changed, because constraints can only be added to this type parameter." );
        }
    }
}
