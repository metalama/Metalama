// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Attribute that means that the target declaration (and all children declarations) can be called both from compile-time
    /// and run-time code. See <see cref="CompileTimeAttribute"/> for declarations that cannot be called from run-time code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can use this attribute on classes that must be included in the compile-time project and therefore made
    /// available to your aspects.
    /// </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface
        | AttributeTargets.Assembly )]
    public sealed class RunTimeOrCompileTimeAttribute : ScopeAttribute;
}