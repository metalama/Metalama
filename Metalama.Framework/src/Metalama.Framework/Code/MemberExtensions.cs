// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for the <see cref="IMember"/> interface.
/// </summary>
/// <seealso cref="IMember"/>
/// <seealso cref="IMemberOrNamedType"/>
/// <seealso cref="DeclarationExtensions"/>
public static class MemberExtensions
{
    /// <summary>
    /// Determines whether a member can be overridden, i.e. whether it is <c>virtual</c>, <c>abstract</c>, or <c>override</c> but not <c>sealed</c>.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <returns><c>true</c> if the member can be overridden; otherwise, <c>false</c>.</returns>
    public static bool IsOverridable( this IMember member )
        => (member.IsVirtual || member.IsAbstract || member.IsOverride)
           && member is { IsSealed: false, DeclaringType: { IsReferenceType: true, IsSealed: false } };

    /// <summary>
    /// Determines whether a member or type can be accessed from a given type.
    /// </summary>
    /// <param name="accessedMember">The member or type being accessed.</param>
    /// <param name="accessingType">The type from which access is attempted.</param>
    /// <returns><c>true</c> if the member or type is accessible from the accessing type; otherwise, <c>false</c>.</returns>
    public static bool IsAccessibleFrom( this IMemberOrNamedType accessedMember, INamedType accessingType )
        => ((ICompilationInternal) accessedMember.Compilation).Helpers.IsAccessibleFrom( accessedMember, accessingType );

    /// <summary>
    /// Determines whether a member or type can be accessed from an external assembly.
    /// </summary>
    /// <param name="declaration">The declaration to check.</param>
    /// <param name="honorInternalVisibleToAttributes">Whether to consider <c>InternalsVisibleTo</c> attributes when determining accessibility.</param>
    /// <returns><c>true</c> if the declaration is accessible from outside the assembly; otherwise, <c>false</c>.</returns>
    public static bool IsAccessibleFromOutsideAssembly( this IDeclaration declaration, bool honorInternalVisibleToAttributes = true )
        => ((ICompilationInternal) declaration.Compilation).Helpers.IsAccessibleFromOutsideAssembly( declaration, honorInternalVisibleToAttributes );

    /// <summary>
    /// Determines whether an <see cref="IMember"/> or <see cref="INamedType"/> can be implemented (i.e. derived from or overridden) from an
    /// outside assembly. When the declaration is an <see cref="IParameter"/>, considers the parent member. Returns <c>false</c> for other
    /// kinds of declarations.
    /// </summary>
    /// <param name="declaration">The declaration to check.</param>
    /// <param name="honorInternalVisibleToAttributes">Whether to consider <c>InternalsVisibleTo</c> attributes when determining accessibility.</param>
    /// <returns><c>true</c> if the declaration can be implemented from outside the assembly; otherwise, <c>false</c>.</returns>
    public static bool CanBeImplementedFromOutsideAssembly( this IDeclaration declaration, bool honorInternalVisibleToAttributes = true )
        => declaration switch
        {
            IMember member => member.IsOverridable() && member.DeclaringType.CanBeImplementedFromOutsideAssembly( honorInternalVisibleToAttributes ),
            INamedType namedType => namedType is { TypeKind: TypeKind.Class, IsSealed: false }
                                    && namedType.Constructors.Any( c => c.IsAccessibleFromOutsideAssembly( honorInternalVisibleToAttributes ) ),
            IParameter { DeclaringMember: { } declaringMember } => declaringMember.CanBeImplementedFromOutsideAssembly( honorInternalVisibleToAttributes ),
            _ => false
        };

    /// <summary>
    /// Determines whether an <see cref="IMember"/> has a receiver expression, i.e. either <c>this</c> or a receiver parameter.
    /// </summary>
    /// <param name="member">A member.</param>
    /// <returns><c>true</c> if <paramref name="member"/> is an instance member or a classic extension method.</returns>
    public static bool HasReceiver( this IMember member )
        => !member.IsStatic || (member.DeclarationKind == DeclarationKind.Method && member is IMethod { Parameters.Count: > 0 } method
                                                                                 && method.Parameters[0].IsThis);
}