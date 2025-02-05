// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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