// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for the <see cref="IMember"/> interface.
/// </summary>
public static class MemberExtensions
{
    /// <summary>
    /// Determines whether a member can be overridden, i.e. whether it is <c>virtual</c>, <c>abstract</c>, or <c>override</c> but not <c>sealed</c>.
    /// </summary>
    public static bool IsOverridable( this IMember member )
        => (member.IsVirtual || member.IsAbstract || member.IsOverride)
           && member is { IsSealed: false, DeclaringType: { IsReferenceType: true, IsSealed: false } };

    /// <summary>
    /// Determines whether a member or type can be accessed from a given type.
    /// </summary>
    public static bool IsAccessibleFrom( this IMemberOrNamedType accessedMember, INamedType accessingType )
        => ((ICompilationInternal) accessedMember.Compilation).Helpers.IsAccessibleFrom( accessedMember, accessingType );

    /// <summary>
    /// Determines whether a member or type can be accessed from an external assembly.
    /// </summary>
    public static bool IsAccessibleFromOutsideAssembly( this IDeclaration declaration, bool honorInternalVisibleToAttributes = true )
        => ((ICompilationInternal) declaration.Compilation).Helpers.IsAccessibleFromOutsideAssembly( declaration, honorInternalVisibleToAttributes );

    /// <summary>
    /// Determines whether a <see cref="IMember"/> or <see cref="INamedType"/> can be implemented (i.e. derived from or overridden) from an
    /// outside assembly. When the declaration is an <see cref="IParameter"/>, considers the parent member. Returns <c>false</c> for other
    /// kinds of declarations.
    /// </summary>
    public static bool CanBeImplementedFromOutsideAssembly( this IDeclaration declaration, bool honorInternalVisibleToAttributes = true )
        => declaration switch
        {
            IMember member => member.IsOverridable() && member.DeclaringType.CanBeImplementedFromOutsideAssembly( honorInternalVisibleToAttributes ),
            INamedType namedType => namedType is { TypeKind: TypeKind.Class or TypeKind.RecordClass, IsSealed: false }
                                    && namedType.Constructors.Any( c => c.IsAccessibleFromOutsideAssembly( honorInternalVisibleToAttributes ) ),
            IParameter { DeclaringMember: { } declaringMember } => declaringMember.CanBeImplementedFromOutsideAssembly( honorInternalVisibleToAttributes ),
            _ => false
        };
}