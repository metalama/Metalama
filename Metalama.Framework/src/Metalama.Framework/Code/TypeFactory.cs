// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Project;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides methods to obtain <see cref="IType"/> instances from reflection types, special types, or by constructing new types.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="GetType(Type)"/> or <see cref="GetNamedType(Type)"/> to convert a <see langword="typeof"/> expression to
/// <see cref="IType"/> or <see cref="INamedType"/>. Use <see cref="GetType(SpecialType)"/> for efficient access to well-known types
/// like <c>string</c> or <c>int</c>.
/// </para>
/// <para>
/// To create derived types from existing types, use methods on <see cref="IType"/> such as
/// <see cref="IType.MakeArrayType"/>, <see cref="IType.MakePointerType"/>, and <see cref="IType.ToNullable"/>.
/// </para>
/// <para>
/// To create tuple types, use <see cref="CreateTupleType(IEnumerable{IType})"/> or its overloads.
/// </para>
/// </remarks>
/// <seealso cref="IType"/>
/// <seealso cref="INamedType"/>
/// <seealso cref="ITupleType"/>
/// <seealso cref="IDeclarationFactory"/>
/// <seealso cref="ExpressionFactory"/>
/// <seealso href="@type-system"/>
[CompileTime]
public static class TypeFactory
{
    internal static IDeclarationFactory Implementation
    {
        get
        {
            var syntaxBuilder =
                MetalamaExecutionContext.CurrentInternal.SyntaxBuilder
                ?? throw new InvalidOperationException(
                    "TypeFactory is not available in this context. In BuildEligibility, TypeFactory can only be used inside eligibility delegates." );

            return ((ICompilationInternal) syntaxBuilder.Compilation).Factory;
        }
    }

    /// <summary>
    /// Gets an <see cref="IType"/> given a reflection <see cref="Type"/>.
    /// </summary>
    public static IType GetType( Type type ) => Implementation.GetTypeByReflectionType( type );

    /// <summary>
    /// Gets an <see cref="INamedType"/> given a reflection <see cref="Type"/>.
    /// </summary>
    public static INamedType GetNamedType( Type type )
        => Implementation.GetTypeByReflectionType( type ) as INamedType
           ?? throw new ArgumentOutOfRangeException( nameof(type), $"'{type}' is not a named type." );

    /// <summary>
    /// Get type based on its full name, as used in reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For nested types, this means using <c>+</c>, e.g. to get <see cref="System.Environment.SpecialFolder"/>, use <c>System.Environment+SpecialFolder</c>.
    /// </para>
    /// <para>
    /// For generic type definitions, this requires using <c>`</c>, e.g. to get <c>List&lt;T&gt;</c>, use <c>System.Collections.Generic.List`1</c>.
    /// </para>
    /// <para>
    /// Constructed generic types (e.g. <c>List&lt;int&gt;</c>) are not supported, for those, use <see cref="GenericExtensions.WithTypeArguments(IMethod, IType[])"/>.
    /// </para>
    /// </remarks>
    public static INamedType GetType( string typeName ) => Implementation.GetTypeByReflectionName( typeName );

    /// <summary>
    /// Gets a <see cref="INamedType"/> representing a given <see cref="SpecialType"/>.
    /// </summary>
    public static INamedType GetType( SpecialType type ) => Implementation.GetSpecialType( type );

    [Obsolete( "Use IType.ToNullable instead." )]
    public static IType ToNullableType( this IType type ) => type.ToNullable();

    [Obsolete( "Use INamedType.ToNullable instead." )]
    public static INamedType ToNullableType( this INamedType type ) => type.ToNullable();

    [Obsolete( "Use IArrayType.ToNullable instead." )]
    public static IArrayType ToNullableType( this IArrayType type ) => type.ToNullable();

    [Obsolete( "Use IDynamicType.ToNullable instead." )]
    public static IDynamicType ToNullableType( this IDynamicType type ) => type.ToNullable();

    [Obsolete( "Use IType.ToNonNullable instead." )]
    public static IType ToNonNullableType( this IType type ) => type.ToNonNullable();

    [Obsolete( "Use ITypeParameter.ToNonNullable instead." )]
    public static ITypeParameter ToNonNullableType( this ITypeParameter type ) => type.ToNonNullable();

    [Obsolete( "Use IArrayType.ToNonNullable instead." )]
    public static IArrayType ToNonNullableType( this IArrayType type ) => type.ToNonNullable();

    [Obsolete( "Use IDynamicType.ToNonNullable instead." )]
    public static IDynamicType ToNonNullableType( this IDynamicType type ) => type.ToNonNullable();

    [Obsolete( "Use INamedType.ToNonNullable instead." )]
    public static IType ToNonNullableType( this INamedType type ) => type.ToNonNullable();

    /// <summary>
    /// Creates a tuple type with the specified element types (given as <see cref="IType"/>) and default element names.
    /// </summary>
    public static ITupleType CreateTupleType( params IEnumerable<IType> elementTypes ) => Implementation.CreateTupleType( elementTypes );

    /// <summary>
    /// Creates a tuple type with the specified element types (given as reflection <see cref="Type"/>'s) and default element names.
    /// </summary>
    public static ITupleType CreateTupleType( params IEnumerable<Type> elementTypes ) => Implementation.CreateTupleType( elementTypes );

    /// <summary>
    /// Creates a tuple type with the specified element types (given as <see cref="IType"/>'s) and names.
    /// </summary>
    public static ITupleType CreateTupleType( params IEnumerable<(IType Type, string Name)> elements ) => Implementation.CreateTupleType( elements );

    /// <summary>
    /// Creates a tuple type with the specified element types (given as reflection <see cref="Type"/>'s) and names.
    /// </summary>
    public static ITupleType CreateTupleType( params IEnumerable<(Type Type, string Name)> elements ) => Implementation.CreateTupleType( elements );

    /// <summary>
    /// Creates a tuple type with the specified parameters as elements.
    /// </summary>
    public static ITupleType CreateTupleType( params IEnumerable<IParameter> elements ) => Implementation.CreateTupleType( elements );
}