// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Attribute that means that the target declaration (and all children declarations) can only be called from compile-time
/// code and, therefore, not from run-time code. See <see cref="RunTimeOrCompileTimeAttribute"/> for declarations
/// that can be called both from compile-time and run-time code.
/// </summary>
/// <param name="isTemplateOnly">Indicates whether the target declaration can only be used from templates, but not from other compile-time code.</param>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface
    | AttributeTargets.Assembly | AttributeTargets.ReturnValue | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
    | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.GenericParameter )]
public sealed class CompileTimeAttribute( bool isTemplateOnly, string? reason = null ) : ScopeAttribute
{
    public CompileTimeAttribute() : this( isTemplateOnly: false ) { }

    /// <summary>
    /// Gets a value indicating whether the target declaration can only be used from templates, but not from other compile-time code.
    /// </summary>
    public bool IsTemplateOnly { get; } = isTemplateOnly;

    public string? Reason { get; } = reason;
}