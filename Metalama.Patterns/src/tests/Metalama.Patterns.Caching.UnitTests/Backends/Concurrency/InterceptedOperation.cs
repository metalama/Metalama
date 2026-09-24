// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Identifies an operation of an <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> that an
/// <see cref="InterceptingMemoryCache"/> can intercept.
/// </summary>
/// <remarks>
/// The extension methods of <see cref="Microsoft.Extensions.Caching.Memory.CacheExtensions"/> are implemented with the
/// members of the interface. <c>Get</c> calls <c>TryGetValue</c>. <c>Set</c> calls <c>CreateEntry</c> and stores the
/// entry when the entry is disposed. <c>GetOrCreate</c> calls <c>TryGetValue</c> and, when the key is missing,
/// <c>CreateEntry</c>, the factory, and the disposal of the entry. None of these methods takes a lock.
/// </remarks>
internal enum InterceptedOperation
{
    /// <summary>
    /// Before the call to <c>TryGetValue</c> is forwarded.
    /// </summary>
    TryGetValueBefore,

    /// <summary>
    /// After the call to <c>TryGetValue</c> has returned. The gate records the value that was read.
    /// </summary>
    TryGetValueAfter,

    /// <summary>
    /// Before the call to <c>CreateEntry</c> is forwarded.
    /// </summary>
    CreateEntry,

    /// <summary>
    /// Before a created entry is stored, which happens when the entry is disposed.
    /// </summary>
    CommitBefore,

    /// <summary>
    /// After a created entry has been stored.
    /// </summary>
    CommitAfter,

    /// <summary>
    /// Before the call to <c>Remove</c> is forwarded.
    /// </summary>
    RemoveBefore,

    /// <summary>
    /// After the call to <c>Remove</c> has returned.
    /// </summary>
    RemoveAfter
}
