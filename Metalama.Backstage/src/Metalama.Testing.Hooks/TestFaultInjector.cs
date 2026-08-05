// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;

namespace Metalama.Testing.Hooks;

/// <summary>
/// The default implementation of <see cref="ITestFaultInjector"/>. A test arms a named injection point with the
/// exception to throw; when the code under test reaches that point, the exception is thrown. Injection points that
/// have not been armed are no-ops.
/// </summary>
[PublicAPI]
public sealed class TestFaultInjector : ITestFaultInjector
{
    private readonly ConcurrentDictionary<string, Func<Exception>> _armedFaults = new( StringComparer.Ordinal );

    /// <summary>
    /// Arms the named injection point, so that the next call to <see cref="ITestFaultInjector.InjectFault"/> with that
    /// name throws.
    /// </summary>
    /// <param name="injectionPointName">The name of the injection point to arm.</param>
    /// <param name="exceptionFactory">A factory of the exception to throw, defaulting to an <see cref="InvalidOperationException"/>.</param>
    public void ArmFault( string injectionPointName, Func<Exception>? exceptionFactory = null )
        => this._armedFaults[injectionPointName] =
            exceptionFactory ?? ( () => new InvalidOperationException( $"Injected fault at '{injectionPointName}'." ) );

    /// <summary>
    /// Disarms the named injection point, so that subsequent calls to <see cref="ITestFaultInjector.InjectFault"/>
    /// with that name are no-ops again.
    /// </summary>
    /// <param name="injectionPointName">The name of the injection point to disarm.</param>
    public void DisarmFault( string injectionPointName ) => this._armedFaults.TryRemove( injectionPointName, out _ );

    /// <inheritdoc />
    public void InjectFault( string injectionPointName )
    {
        if ( this._armedFaults.TryGetValue( injectionPointName, out var exceptionFactory ) )
        {
            throw exceptionFactory();
        }
    }
}
