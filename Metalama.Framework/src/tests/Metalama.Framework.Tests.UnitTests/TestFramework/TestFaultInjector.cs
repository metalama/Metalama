// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Testing;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Tests.UnitTests.TestFramework
{
    /// <summary>
    /// Test implementation of <see cref="ITestFaultInjector"/>. Tests arm a named injection point with the exception to
    /// throw; when engine code reaches that point (via <see cref="ITestFaultInjector.InjectFault"/>), the exception is
    /// thrown. Unarmed injection points are no-ops, mirroring <c>TestSynchronizationProvider</c>. See #1701.
    /// </summary>
    internal sealed class TestFaultInjector : ITestFaultInjector
    {
        private readonly ConcurrentDictionary<string, Func<Exception>> _armedFaults = new( StringComparer.Ordinal );

        /// <summary>
        /// Arms the named injection point so that the next call to <see cref="ITestFaultInjector.InjectFault"/> with that
        /// name throws. The <paramref name="exceptionFactory"/> defaults to an <see cref="InvalidOperationException"/>.
        /// </summary>
        public void ArmFault( string injectionPointName, Func<Exception>? exceptionFactory = null )
            => this._armedFaults[injectionPointName] = exceptionFactory ?? ( () => new InvalidOperationException( $"Injected fault at '{injectionPointName}'." ) );

        /// <summary>
        /// Disarms the named injection point. Subsequent calls to <see cref="ITestFaultInjector.InjectFault"/> with that
        /// name are no-ops again.
        /// </summary>
        public void DisarmFault( string injectionPointName ) => this._armedFaults.TryRemove( injectionPointName, out _ );

        void ITestFaultInjector.InjectFault( string injectionPointName )
        {
            if ( this._armedFaults.TryGetValue( injectionPointName, out var exceptionFactory ) )
            {
                throw exceptionFactory();
            }
        }
    }
}
