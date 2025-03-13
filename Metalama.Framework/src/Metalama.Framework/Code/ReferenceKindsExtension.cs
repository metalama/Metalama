// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Globalization;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for <see cref="ReferenceKinds"/>.
/// </summary>
[PublicAPI]
public static class ReferenceKindsExtension
{
    /// <summary>
    /// Same as <c>ToString</c> excepts that it does not use obsolete names.
    /// </summary>
    public static string ToDisplayString( this ReferenceKinds kinds )
    {
        // We have some non-standard handling because of obsolete members.
        // The behavior of Enum.ToString is random where there are synonyms.

        switch ( kinds )
        {
            case ReferenceKinds.All:
                return "All";

            case ReferenceKinds.None:
                return "None";
        }

        List<string> values = new();
        var consideredKinds = ReferenceKinds.None;

        void ConsiderKind( ReferenceKinds kind, string name )
        {
            if ( kinds.IsDefined( kind ) )
            {
                values.Add( name );
                consideredKinds |= kind;
            }
        }

        ConsiderKind( ReferenceKinds.Default, nameof(ReferenceKinds.Default) );
        ConsiderKind( ReferenceKinds.BaseType, nameof(ReferenceKinds.BaseType) );
        ConsiderKind( ReferenceKinds.TypeArgument, nameof(ReferenceKinds.TypeArgument) );
        ConsiderKind( ReferenceKinds.TypeOf, nameof(ReferenceKinds.TypeOf) );
        ConsiderKind( ReferenceKinds.ParameterType, nameof(ReferenceKinds.ParameterType) );
        ConsiderKind( ReferenceKinds.TypeConstraint, nameof(ReferenceKinds.TypeConstraint) );
        ConsiderKind( ReferenceKinds.ObjectCreation, nameof(ReferenceKinds.ObjectCreation) );
        ConsiderKind( ReferenceKinds.MemberType, nameof(ReferenceKinds.MemberType) );
        ConsiderKind( ReferenceKinds.LocalVariableType, nameof(ReferenceKinds.LocalVariableType) );
        ConsiderKind( ReferenceKinds.ReturnType, nameof(ReferenceKinds.ReturnType) );
        ConsiderKind( ReferenceKinds.PointerType, nameof(ReferenceKinds.PointerType) );
        ConsiderKind( ReferenceKinds.TupleElementType, nameof(ReferenceKinds.TupleElementType) );
        ConsiderKind( ReferenceKinds.Invocation, nameof(ReferenceKinds.Invocation) );
        ConsiderKind( ReferenceKinds.Assignment, nameof(ReferenceKinds.Assignment) );
        ConsiderKind( ReferenceKinds.OverrideMember, nameof(ReferenceKinds.OverrideMember) );
        ConsiderKind( ReferenceKinds.ArrayElementType, nameof(ReferenceKinds.ArrayElementType) );
        ConsiderKind( ReferenceKinds.UsingNamespace, nameof(ReferenceKinds.UsingNamespace) );
        ConsiderKind( ReferenceKinds.NameOf, nameof(ReferenceKinds.NameOf) );
        ConsiderKind( ReferenceKinds.BaseConstructor, nameof(ReferenceKinds.BaseConstructor) );
        ConsiderKind( ReferenceKinds.AttributeType, nameof(ReferenceKinds.AttributeType) );
        ConsiderKind( ReferenceKinds.InterfaceMemberImplementation, nameof(ReferenceKinds.InterfaceMemberImplementation) );
        ConsiderKind( ReferenceKinds.ArrayCreation, nameof(ReferenceKinds.ArrayCreation) );
        ConsiderKind( ReferenceKinds.CastType, nameof(ReferenceKinds.CastType) );
        ConsiderKind( ReferenceKinds.IsType, nameof(ReferenceKinds.IsType) );

        if ( consideredKinds != kinds )
        {
            // If we forgot something, fallback to the integer value, this is at least deterministic.
            return ((int) kinds).ToString( CultureInfo.InvariantCulture );
        }

        return string.Join( " | ", values );
    }

    /// <summary>
    /// Determines if a <see cref="ReferenceKinds"/> contains all required flags.
    /// </summary>
    public static bool IsDefined( this ReferenceKinds kinds, ReferenceKinds requiredKinds ) => (kinds & requiredKinds) == requiredKinds;

    public static ReferenceKinds Union( this IEnumerable<ReferenceKinds> kinds )
    {
        var union = ReferenceKinds.None;

        foreach ( var kind in kinds )
        {
            union |= kind;
        }

        return union;
    }
}