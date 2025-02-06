// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Immutability.Configuration;

namespace Metalama.Patterns.Immutability;

/// <summary>
/// Provides the <see cref="GetImmutabilityKind"/> method that returns the <see cref="ImmutabilityKind"/>
/// for a given type.
/// </summary>
[CompileTime]
public static class ImmutabilityExtensions
{
    /// <summary>
    /// Returns the <see cref="ImmutabilityKind"/> for a given type.
    /// </summary>
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
            } )
        {
            return ImmutabilityKind.Deep;
        }

        if ( namedType.IsReadOnly )
        {
            return ImmutabilityKind.Shallow;
        }

        return ImmutabilityKind.None;
    }
}