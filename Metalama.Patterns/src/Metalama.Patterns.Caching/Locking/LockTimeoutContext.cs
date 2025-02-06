// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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