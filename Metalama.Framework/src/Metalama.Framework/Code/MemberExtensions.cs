// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Eligibility;
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
    /// Determines whether an explicit declaration of a member can be written in source code, which is a condition for
    /// an aspect to override the member.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <returns><c>true</c> if an explicit declaration of the member can be written in source code; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// The method returns <c>false</c> in two cases. The first case is a member that the C# compiler adds to a record
    /// even when the record declares this member itself: <c>Equals(object)</c>, the <c>Equals</c> overload whose
    /// parameter is a base record, and the <c>==</c> and <c>!=</c> operators. A declaration of one of these members is
    /// a duplicate, which the C# compiler reports as an error. The second case is a compiler-generated field, such as
    /// the backing field of an auto-property or the field that captures a primary constructor parameter, because the
    /// name of such a field is not a valid C# identifier. For an accessor, the method returns the same value as for
    /// the property or the event that contains the accessor.
    /// </para>
    /// <para>
    /// The method returns <c>true</c> for any other member. In particular, it returns <c>true</c> for the record
    /// members that the C# compiler generates only when the record does not declare them: the <c>Equals</c> overload
    /// whose parameter is the declaring record, <c>GetHashCode</c>, <c>ToString</c>, <c>PrintMembers</c>,
    /// <c>Deconstruct</c>, <c>EqualityContract</c> and the copy constructor. When an aspect overrides one of these
    /// members, <see cref="meta.Proceed"/> runs the implementation that the C# compiler would have generated. The copy
    /// constructor is the exception: no advice can target it, because <see cref="EligibilityExtensions.MustNotBeRecordCopyConstructor"/>
    /// excludes it from every advice that could.
    /// </para>
    /// </remarks>
    public static bool CanBeDeclaredExplicitly( this IMember member )
    {
        if ( !member.IsImplicitlyDeclared )
        {
            return true;
        }

        switch ( member.DeclarationKind )
        {
            // The name of a compiler-generated field is not a valid C# identifier.
            case DeclarationKind.Field:
                return false;

            case DeclarationKind.Method:
                var method = (IMethod) member;

                // An accessor can be declared explicitly if, and only if, the property or the event that contains it can.
                if ( method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
                    or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise )
                {
                    return ((IMember) method.ContainingDeclaration!).CanBeDeclaredExplicitly();
                }

                return !IsRecordMemberAddedUnconditionally( method );

            default:
                return true;
        }
    }

    private static bool IsRecordMemberAddedUnconditionally( IMethod method )
    {
        if ( method.DeclaringType is not { IsRecord: true } )
        {
            return false;
        }

        return method switch
        {
            // Equals(object) and, in a derived record, the Equals overload whose parameter is the base record.
            { Name: nameof(object.Equals), Parameters: [var parameter] } => !parameter.Type.Equals(
                method.DeclaringType,
                TypeComparison.Default ),
            { OperatorKind: OperatorKind.Equality or OperatorKind.Inequality } => true,
            _ => false
        };
    }

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