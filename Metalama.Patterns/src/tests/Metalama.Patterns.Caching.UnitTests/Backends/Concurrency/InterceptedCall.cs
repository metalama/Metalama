// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// A call intercepted by an <see cref="InterceptingMemoryCache"/>.
/// </summary>
/// <param name="Operation">The intercepted operation.</param>
/// <param name="Key">The full cache key.</param>
/// <param name="ThreadName">The name of the calling thread, or <see langword="null"/> when the thread has no name.</param>
internal sealed record InterceptedCall( InterceptedOperation Operation, string Key, string? ThreadName );
