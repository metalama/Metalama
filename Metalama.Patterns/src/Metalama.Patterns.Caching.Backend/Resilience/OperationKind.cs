// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Enumeration of operation kinds for <see cref="IRetryPolicy.TryAsync"/> and <see cref="IExceptionHandlingPolicy"/>.
/// </summary>
[PublicAPI]
public enum OperationKind
{
    /// <summary>
    /// An operation of an unspecified type.
    /// </summary>
    None,

    /// <summary>
    /// <see cref="CachingBackend.InvalidateDependencyAsync"/>.
    /// </summary>
    InvalidateDependency,

    /// <summary>
    /// Garbage collection.
    /// </summary>
    Collect,

    /// <summary>
    /// Clean up.
    /// </summary>
    CleanUp,

    /// <summary>
    /// <see cref="CachingBackend.SetItemAsync"/>.
    /// </summary>
    SetItem,

    /// <summary>
    /// <see cref="CachingBackend.ContainsItemAsync"/>.
    /// </summary>
    ContainsItem,

    /// <summary>
    /// <see cref="CachingBackend.RemoveItemAsync"/>.
    /// </summary>
    RemoveItem,

    /// <summary>
    /// <see cref="CachingBackend.ContainsDependencyAsync"/>.
    /// </summary>
    ContainsDependency,

    /// <summary>
    /// <see cref="CachingBackend.GetItemAsync"/>.
    /// </summary>
    GetItem,

    /// <summary>
    /// A background (non-blocking) operation.
    /// </summary>
    Background
}