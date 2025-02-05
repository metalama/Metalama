// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Attribute that means that the target declaration (and all children declarations) can be called both from compile-time
    /// and run-time code, even if the generic type arguments are not run-time-or-compile-time.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Interface )]
    public sealed class ForcedGenericRunTimeOrCompileTimeAttribute : ScopeAttribute;
}