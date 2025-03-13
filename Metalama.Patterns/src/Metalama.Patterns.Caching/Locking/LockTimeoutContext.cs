// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Locking;

/// <summary>
/// Context object for the <see cref="CachingProfile.OnLockTimeout"/> delegate.
/// </summary>
[PublicAPI]
public sealed class LockTimeoutContext
{
    public string Key { get; }

    public ILockHandle LockHandle { get; }

    public CachingBackend Backend { get; }

    public ICachingService CachingService { get; }

    internal LockTimeoutContext( string key, ILockHandle lockHandle, CachingBackend backend, ICachingService cachingService )
    {
        this.Key = key;
        this.LockHandle = lockHandle;
        this.Backend = backend;
        this.CachingService = cachingService;
    }
}