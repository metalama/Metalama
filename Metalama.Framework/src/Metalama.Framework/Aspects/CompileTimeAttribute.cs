// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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