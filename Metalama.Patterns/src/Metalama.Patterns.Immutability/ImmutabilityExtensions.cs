// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Immutability.Configuration;

namespace Metalama.Patterns.Immutability;

/// <summary>
/// Provides extension methods for querying the immutability of types.
/// </summary>
/// <seealso cref="ImmutableAttribute"/>
/// <seealso cref="ImmutabilityKind"/>
/// <seealso href="@immutability"/>
[CompileTime]
public static class ImmutabilityExtensions
{
    /// <summary>
    /// Returns the <see cref="ImmutabilityKind"/> for a given type.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>
    /// The <see cref="ImmutabilityKind"/> of the type, determined by the following rules in order:
    /// <list type="bullet">
    /// <item><description><see cref="ImmutabilityKind.Deep"/> for intrinsic types (<c>bool</c>, <c>byte</c>, <c>int</c>,
    /// <c>string</c>, etc.), delegates, enums, pointers, and function pointers.</description></item>
    /// <item><description>The <see cref="ImmutabilityKind"/> configured via <see cref="ImmutableAttribute"/> or
    /// <see cref="Configuration.ImmutabilityConfigurationExtensions.ConfigureImmutability(Metalama.Framework.Fabrics.IQuery{Metalama.Framework.Code.INamedType},ImmutabilityKind)"/>.</description></item>
    /// <item><description>The result of a configured <see cref="Configuration.IImmutabilityClassifier"/> if one is set.</description></item>
    /// <item><description><see cref="ImmutabilityKind.Deep"/> for value types in the <c>System</c> namespace, except
    /// <c>ValueTuple</c>, <c>Span</c>, <c>ReadOnlySpan</c>, <c>Memory</c>, and <c>ReadOnlyMemory</c>.</description></item>
    /// <item><description><see cref="ImmutabilityKind.Shallow"/> for read-only structs.</description></item>
    /// <item><description><see cref="ImmutabilityKind.None"/> for all other types.</description></item>
    /// </list>
    /// </returns>
    public static ImmutabilityKind GetImmutabilityKind( this IType type )
    {
        if ( type is {
                SpecialType: SpecialType.Boolean or
                SpecialType.Byte or
                SpecialType.Char or
                SpecialType.Decimal or
                SpecialType.Double or
                SpecialType.Int16 or
                SpecialType.Int32 or
                SpecialType.Int64 or
                SpecialType.SByte or
                SpecialType.Single or
                SpecialType.String or
                SpecialType.UInt16 or
                SpecialType.UInt32 or
                SpecialType.UInt64
            } or { TypeKind: TypeKind.Delegate or TypeKind.Enum or TypeKind.Pointer or TypeKind.FunctionPointer } )
        {
            return ImmutabilityKind.Deep;
        }

        if ( type is not INamedType namedType )
        {
            return ImmutabilityKind.None;
        }

        var options = namedType.Definition.Enhancements().GetOptions<ImmutabilityOptions>();

        if ( options.Kind != null )
        {
            return options.Kind.Value;
        }

        if ( options.Classifier != null )
        {
            return options.Classifier.GetImmutabilityKind( namedType );
        }

        // A few hard-coded types. We could avoid hard-coding by having a concept of pluggable "IImmutabilityRule"
        // that could be overwritten, but this does not seem necessary for now.

        if ( namedType is
            {
                IsReferenceType: false,
                ContainingNamespace.FullName: "System"
            }
             && !IsNonImmutableSystemValueType( namedType ) )
        {
            return ImmutabilityKind.Deep;
        }

        if ( namedType.IsReadOnly )
        {
            return ImmutabilityKind.Shallow;
        }

        return ImmutabilityKind.None;
    }

    private static bool IsNonImmutableSystemValueType( INamedType namedType )
    {
        var name = namedType.Definition.Name;

        return name is "ValueTuple" or "Span" or "ReadOnlySpan" or "Memory" or "ReadOnlyMemory";
    }
}