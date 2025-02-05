// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Exposes methods that return instances of the <see cref="IType"/> interface.
/// </summary>
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
}