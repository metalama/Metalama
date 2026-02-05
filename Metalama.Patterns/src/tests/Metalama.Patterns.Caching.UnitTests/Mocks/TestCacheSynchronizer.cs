// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A test implementation of <see cref="CacheSynchronizer"/> that captures sent messages.
/// </summary>
internal sealed class TestCacheSynchronizer : CacheSynchronizer
{
    public List<string> SentMessages { get; } = new();

    public TestCacheSynchronizer( CachingBackend underlyingBackend, CacheSynchronizerConfiguration configuration )
        : base( underlyingBackend, configuration ) { }

    protected override Task SendMessageAsync( string message, CancellationToken cancellationToken )
    {
        this.SentMessages.Add( message );

        return Task.CompletedTask;
    }

    public void ProcessMessage( string message )
    {
        this.OnMessageReceived( message );
    }
}
