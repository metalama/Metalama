// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using Accessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    public static class AccessibilityHelper
    {
        private static AccessibilityFlags ToAccessibilityFlags( this Accessibility accessibility )
            => accessibility switch
            {
                Accessibility.Private => AccessibilityFlags.SameType,
                Accessibility.Internal => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfFriendAssembly
                                                                      | AccessibilityFlags.AnyTypeOfFriendAssembly,
                Accessibility.Protected => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfAnyAssembly
                                                                       | AccessibilityFlags.DerivedTypeOfAnyAssembly,
                Accessibility.ProtectedAndInternal => AccessibilityFlags.SameType | AccessibilityFlags.DerivedTypeOfFriendAssembly,
                Accessibility.ProtectedOrInternal => AccessibilityFlags.SameType | AccessibilityFlags.AnyTypeOfFriendAssembly
                                                                                 | AccessibilityFlags.DerivedTypeOfAnyAssembly,
                Accessibility.Public => AccessibilityFlags.SameType | AccessibilityFlags.AnyType | AccessibilityFlags.AnyTypeOfFriendAssembly
                                        | AccessibilityFlags.DerivedTypeOfAnyAssembly | AccessibilityFlags.DerivedTypeOfFriendAssembly,
                _ => throw new ArgumentOutOfRangeException( nameof(accessibility) )
            };

        public static AccessibilityFlags GetResultingAccessibility( this ISymbol symbol )
        {
            var accessibility = AccessibilityFlags.Public;

            for ( var s = symbol; s != null && s.DeclaredAccessibility != Accessibility.NotApplicable; s = s.ContainingSymbol )
            {
                accessibility &= symbol.DeclaredAccessibility.ToAccessibilityFlags();
            }

            return accessibility;
        }
    }
}