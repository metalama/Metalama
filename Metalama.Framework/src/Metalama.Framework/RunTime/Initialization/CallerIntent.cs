// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Specifies the caller's intent regarding <c>OnInitialized</c> invocation.
/// Values are mutually exclusive (not a flags enum).
/// </summary>
/// <remarks>
/// <para>
/// This value is carried by <see cref="InitializationContext.Intent"/> and determines whether
/// the constructor should self-invoke <c>OnInitialized</c> or whether the caller will do so.
/// </para>
/// <para>
/// The source transformer chooses between <see cref="WillInitialize"/> and <see cref="CallInitialize"/>
/// based on whether the call site uses an object initializer.
/// </para>
/// </remarks>
/// <seealso cref="InitializationContext"/>
public enum CallerIntent : byte
{
    /// <summary>
    /// No guarantee that <c>OnInitialized</c> will be called.
    /// Used by non-instrumented callers (generated overload).
    /// </summary>
    None = 0,

    /// <summary>
    /// The caller will call <c>.OnInitialized()</c> after construction
    /// (e.g., after an object initializer completes).
    /// The constructor should not self-invoke.
    /// </summary>
    WillInitialize = 1,

    /// <summary>
    /// The constructor should self-invoke <c>OnInitialized</c> at the end of its body.
    /// Used at call sites without object initializers, where the source transformer
    /// can guarantee all properties are set by the constructor.
    /// </summary>
    CallInitialize = 2
}
