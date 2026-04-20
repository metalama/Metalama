// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Specifies the caller's intent regarding <see cref="IInitializable.Initialize"/> invocation.
/// Values are mutually exclusive (not a flags enum).
/// </summary>
/// <remarks>
/// This value is carried by <see cref="InitializationContext.Intent"/> and indicates whether
/// the caller has committed to invoking <see cref="IInitializable.Initialize"/> after construction.
/// </remarks>
/// <seealso cref="InitializationContext"/>
public enum CallerIntent : byte
{
    /// <summary>
    /// No guarantee that <see cref="IInitializable.Initialize"/> will be called.
    /// Used by non-instrumented callers (generated overload).
    /// </summary>
    None = 0,

    /// <summary>
    /// The caller will call <see cref="IInitializable.Initialize"/> after construction
    /// (e.g., after an object initializer completes).
    /// </summary>
    WillInitialize = 1
}