// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Metalama.Testing.Hooks;

/// <summary>
/// The default implementation of <see cref="ITestFaultInjector"/>. A test arms a named injection point with the
/// exception to throw; when the code under test reaches that point, the exception is thrown. Injection points that
/// have not been armed are no-ops.
/// </summary>
[PublicAPI]
public sealed class TestFaultInjector : ITestFaultInjector
{
    /// <summary>
    /// The value of <c>count</c> that makes an injection point throw on every call.
    /// </summary>
    public const int Always = int.MaxValue;

    private readonly ConcurrentDictionary<string, ArmedFault> _armedFaults = new( StringComparer.Ordinal );

    /// <summary>
    /// Arms the named injection point, so that the next calls to <see cref="ITestFaultInjector.InjectFault"/> with
    /// that name throw.
    /// </summary>
    /// <param name="injectionPointName">The name of the injection point to arm.</param>
    /// <param name="exceptionFactory">A factory of the exception to throw, defaulting to an <see cref="InvalidOperationException"/>.</param>
    /// <param name="count">
    /// The number of calls that must throw, after which the injection point becomes a no-op again. It defaults to
    /// <see cref="Always"/>.
    /// </param>
    /// <remarks>
    /// A finite <paramref name="count"/> is what lets a test model a transient condition, such as a race between
    /// two processes that the losing one resolves by trying again. A fault that always throws can only model a
    /// permanent failure, so it exercises the branch that gives up and never the branch that recovers.
    /// </remarks>
    public void ArmFault( string injectionPointName, Func<Exception>? exceptionFactory = null, int count = Always )
    {
        if ( count <= 0 )
        {
            throw new ArgumentOutOfRangeException( nameof(count), "The number of calls that must throw must be greater than zero." );
        }

        this._armedFaults[injectionPointName] = new ArmedFault(
            exceptionFactory ?? ( () => new InvalidOperationException( $"Injected fault at '{injectionPointName}'." ) ),
            count );
    }

    /// <summary>
    /// Disarms the named injection point, so that subsequent calls to <see cref="ITestFaultInjector.InjectFault"/>
    /// with that name are no-ops again.
    /// </summary>
    /// <param name="injectionPointName">The name of the injection point to disarm.</param>
    public void DisarmFault( string injectionPointName ) => this._armedFaults.TryRemove( injectionPointName, out _ );

    /// <summary>
    /// Gets the number of times the named injection point has thrown.
    /// </summary>
    /// <param name="injectionPointName">The name of the injection point.</param>
    /// <returns>The number of injected faults.</returns>
    /// <remarks>
    /// A test asserts on this to establish that the code under test reached the point the expected number of
    /// times, rather than inferring it from the outcome.
    /// </remarks>
    public int GetInjectedFaultCount( string injectionPointName )
        => this._armedFaults.TryGetValue( injectionPointName, out var armedFault ) ? armedFault.InjectedCount : 0;

    /// <inheritdoc />
    public void InjectFault( string injectionPointName )
    {
        if ( this._armedFaults.TryGetValue( injectionPointName, out var armedFault ) && armedFault.TryConsume() )
        {
            throw armedFault.ExceptionFactory();
        }
    }

    /// <summary>
    /// One armed injection point, and how many times it has left to throw.
    /// </summary>
    private sealed class ArmedFault
    {
        private int _remainingCount;
        private int _injectedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArmedFault"/> class.
        /// </summary>
        /// <param name="exceptionFactory">A factory of the exception to throw.</param>
        /// <param name="count">The number of calls that must throw.</param>
        public ArmedFault( Func<Exception> exceptionFactory, int count )
        {
            this.ExceptionFactory = exceptionFactory;
            this._remainingCount = count;
        }

        /// <summary>
        /// Gets a factory of the exception to throw.
        /// </summary>
        public Func<Exception> ExceptionFactory { get; }

        /// <summary>
        /// Gets the number of times this injection point has thrown.
        /// </summary>
        public int InjectedCount => Volatile.Read( ref this._injectedCount );

        /// <summary>
        /// Consumes one of the calls that must throw.
        /// </summary>
        /// <returns><see langword="true"/> if the caller must throw.</returns>
        /// <remarks>
        /// The count is decremented with a compare-and-swap rather than with <see cref="Interlocked.Decrement(ref int)"/>,
        /// so that several threads reaching an injection point armed for one call produce exactly one exception.
        /// </remarks>
        public bool TryConsume()
        {
            while ( true )
            {
                var remaining = Volatile.Read( ref this._remainingCount );

                if ( remaining == Always )
                {
                    Interlocked.Increment( ref this._injectedCount );

                    return true;
                }

                if ( remaining == 0 )
                {
                    return false;
                }

                if ( Interlocked.CompareExchange( ref this._remainingCount, remaining - 1, remaining ) == remaining )
                {
                    Interlocked.Increment( ref this._injectedCount );

                    return true;
                }
            }
        }
    }
}
