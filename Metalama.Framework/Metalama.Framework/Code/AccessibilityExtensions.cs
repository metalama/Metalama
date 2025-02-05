// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code
{
    [CompileTime]
    public static class AccessibilityExtensions
    {
        private static bool IsSupersetOf( this AccessibilityFlags left, AccessibilityFlags right ) => (right & left) == right && left != right;

        private static bool IsSubsetOf( this AccessibilityFlags left, AccessibilityFlags right ) => (right & left) == left && left != right;

        private static bool IsSupersetOrEqual( this AccessibilityFlags left, AccessibilityFlags right ) => (right & left) == right;

        private static bool IsSubsetOrEqual( this AccessibilityFlags left, AccessibilityFlags right ) => (right & left) == left;

        private static AccessibilityFlags ToAccessibilityFlags( this Accessibility accessibility )
            => accessibility switch
            {
                Accessibility.Private => AccessibilityFlags.SameType,
                Accessibility.Internal => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfFriendAssembly
                                                                      | AccessibilityFlags.AnyTypeOfFriendAssembly,
                Accessibility.Protected => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfFriendAssembly
                                                                       | AccessibilityFlags.DerivedTypeOfAnyAssembly,
                Accessibility.PrivateProtected => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfFriendAssembly,
                Accessibility.ProtectedInternal => AccessibilityFlags.SameType | AccessibilityFlags.AnyTypeOfFriendAssembly
                                                                               | AccessibilityFlags.DerivedTypeOfFriendAssembly
                                                                               | AccessibilityFlags.DerivedTypeOfAnyAssembly,
                Accessibility.Public => AccessibilityFlags.Public,
                _ => throw new ArgumentOutOfRangeException( nameof(accessibility) )
            };

        /// <summary>
        /// Returns whether the left accessibility is strictly less restrictive than the right accessibility.
        /// </summary>
        /// <example>
        /// For example, <c>a.IsSupersetOf(Accessibility.Protected)</c> returns <see langword="true"/> for <c>a</c> being
        /// <see cref="Accessibility.ProtectedInternal"/> or <see cref="Accessibility.Public"/>.
        /// </example>
        public static bool IsSupersetOf( this Accessibility left, Accessibility right )
            => left.ToAccessibilityFlags().IsSupersetOf( right.ToAccessibilityFlags() );

        /// <summary>
        /// Returns whether the left accessibility is strictly more restrictive than the right accessibility.
        /// </summary>
        /// <example>
        /// For example, <c>a.IsSubsetOf(Accessibility.Protected)</c> returns <see langword="true"/> for <c>a</c> being
        /// <see cref="Accessibility.Private"/> or <see cref="Accessibility.PrivateProtected"/>.
        /// </example>
        public static bool IsSubsetOf( this Accessibility left, Accessibility right ) => left.ToAccessibilityFlags().IsSubsetOf( right.ToAccessibilityFlags() );

        /// <summary>
        /// Returns whether the left accessibility is less restrictive or equal than the right accessibility.
        /// </summary>
        /// <example>
        /// For example, <c>a.IsSupersetOf(Accessibility.Protected)</c> returns <see langword="true"/> for <c>a</c> being
        /// <see cref="Accessibility.Protected"/>, <see cref="Accessibility.ProtectedInternal"/> or <see cref="Accessibility.Public"/>.
        /// </example>
        public static bool IsSupersetOrEqual( this Accessibility left, Accessibility right )
            => left.ToAccessibilityFlags().IsSupersetOrEqual( right.ToAccessibilityFlags() );

        /// <summary>
        /// Returns whether the left accessibility is more restrictive or equal than the right accessibility.
        /// </summary>
        /// <example>
        /// For example, <c>a.IsSubsetOf(Accessibility.Protected)</c> returns <see langword="true"/> for <c>a</c> being
        /// <see cref="Accessibility.Protected"/>, <see cref="Accessibility.Private"/> or <see cref="Accessibility.PrivateProtected"/>.
        /// </example>
        public static bool IsSubsetOrEqual( this Accessibility left, Accessibility right )
            => left.ToAccessibilityFlags().IsSubsetOrEqual( right.ToAccessibilityFlags() );
    }
}