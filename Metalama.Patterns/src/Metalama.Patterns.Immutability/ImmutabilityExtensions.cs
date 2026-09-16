// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
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
    /// <item><description><see cref="ImmutabilityKind.Deep"/> for a union declared with the <c>union</c> keyword whose
    /// every case type is deeply immutable, and <see cref="ImmutabilityKind.Shallow"/> for any other such
    /// union.</description></item>
    /// <item><description><see cref="ImmutabilityKind.Shallow"/> for read-only structs.</description></item>
    /// <item><description><see cref="ImmutabilityKind.None"/> for all other types.</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// The rule for a union rests on the definition that <see cref="ImmutabilityKind.Shallow"/> gives, which is about
    /// instance fields and automatic property setters, and not on the type being declared <c>readonly</c>. A union
    /// declaration satisfies that definition whether or not it carries the modifier, because its only instance field is
    /// the read-only backing field of the synthesized get-only <c>Value</c> property, and the language reports an error
    /// for an instance field, an automatic property or a field-like event declared in a union.
    /// </para>
    /// <para>
    /// The rule applies to the declaration form only. A class or a struct that carries the
    /// <c>System.Runtime.CompilerServices.UnionAttribute</c> attribute is also a union, its state is unconstrained, and
    /// it is classified by the rules that apply to any other class or struct. A union read from a referenced assembly
    /// is reported as the attribute form, because the compiled form of a union does not record whether the source used
    /// the <c>union</c> keyword, so a union of another project is classified by those rules as well.
    /// </para>
    /// </remarks>
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

        if ( namedType.Facets.Union is { UnionKind: UnionKind.Declaration } union )
        {
            return union.Cases.All( c => IsDeeplyImmutableUnionCaseType( c.Type ) ) ? ImmutabilityKind.Deep : ImmutabilityKind.Shallow;
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

    /// <summary>
    /// Determines whether a case type of a union declaration allows the union to be classified as
    /// <see cref="ImmutabilityKind.Deep"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four forms of case type are rejected without evaluating <see cref="GetImmutabilityKind"/>, because for each of
    /// them the classification of the case type does not describe the value that the union holds. An interface and a
    /// type parameter stand for a run-time type that is not known here. A nullable value type is a
    /// <c>System.Nullable&lt;T&gt;</c>, which the rule on the value types of the <c>System</c> namespace classifies as
    /// deeply immutable whatever its type argument is. A case type that is itself a union declaration would make the
    /// classification recursive, and a union may list a union among its cases.
    /// </para>
    /// </remarks>
    private static bool IsDeeplyImmutableUnionCaseType( IType caseType )
        => caseType switch
        {
            { TypeKind: TypeKind.Interface or TypeKind.TypeParameter } => false,
            { IsNullable: true, IsReferenceType: false } => false,
            INamedType { IsUnion: true } => false,
            _ => caseType.GetImmutabilityKind() == ImmutabilityKind.Deep
        };
}