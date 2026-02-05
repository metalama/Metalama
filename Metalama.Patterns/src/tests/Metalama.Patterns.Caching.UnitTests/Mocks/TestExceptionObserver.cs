// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A test implementation of <see cref="ICachingExceptionObserver"/> that collects exceptions.
/// </summary>
internal sealed class TestExceptionObserver : ICachingExceptionObserver
{
    public List<CachingExceptionInfo> Exceptions { get; } = new();

    public void OnException( CachingExceptionInfo exceptionInfo )
    {
        this.Exceptions.Add( exceptionInfo );
    }
}
