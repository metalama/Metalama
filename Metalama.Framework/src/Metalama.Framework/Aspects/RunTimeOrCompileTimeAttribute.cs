// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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