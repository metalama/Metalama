// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Threading;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// A durable alternative to <see cref="System.Lazy{T}"/>, that is, one that is safe to be held across
    /// compilations. The factory delegate is released as soon as the value has been computed.
    /// </summary>
    /// <typeparam name="T">The type of the lazily computed value, which must itself be durable.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="System.Lazy{T}"/> is not durable, because it holds its factory delegate and a delegate holds its
    /// closure. A lambda that mentions a compilation produces a closure object that holds it, and whatever holds the
    /// delegate holds the compilation. That capture is invisible in the source, which is what makes it the most
    /// expensive mistake of this kind.
    /// </para>
    /// <para>
    /// This type closes the gap in two ways. The constructor parameter is marked with
    /// <see cref="DurableAttribute"/>, so the analyzer examines what the factory captures at every call site. And the
    /// factory is set to <c>null</c> once the value has been computed, so nothing it captured is retained beyond the
    /// first evaluation.
    /// </para>
    /// <para>
    /// The value is computed at most once. Like the default mode of <see cref="System.Lazy{T}"/>, this type uses lock
    /// based synchronization, so a factory that acquires the same lock from another thread deadlocks.
    /// </para>
    /// </remarks>
    [Durable]
    [PublicAPI]
    public sealed class DurableLazy<T>
    {
        /// <remarks>
        /// The declared type is <see cref="object"/>, which does not constrain what may be stored, so the member
        /// carries <see cref="DurableAttribute"/> to state that only a durable value is ever assigned to it. The one
        /// assignment is <c>new object()</c>, which reaches nothing.
        /// </remarks>
        [Durable]
        private readonly object _lock = new();

        /// <remarks>
        /// Not read-only, because it is cleared once the value has been computed. That is the point of the type: the
        /// closure of the factory must not outlive the single evaluation that needs it.
        /// </remarks>
        [Durable]
        private Func<T>? _factory;

        private T? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DurableLazy{T}"/> class.
        /// </summary>
        /// <param name="factory">
        /// The function that computes the value. It must capture only durable values, which the analyzer verifies at
        /// the call site.
        /// </param>
        public DurableLazy( [Durable] Func<T> factory )
        {
            this._factory = factory ?? throw new ArgumentNullException( nameof(factory) );
        }

        /// <summary>
        /// Gets a value indicating whether the value has already been computed.
        /// </summary>
        public bool IsValueCreated => Volatile.Read( ref this._factory ) == null;

        /// <summary>
        /// Gets the value, computing it on the first access.
        /// </summary>
        public T Value
        {
            get
            {
                if ( Volatile.Read( ref this._factory ) == null )
                {
                    return this._value!;
                }

                lock ( this._lock )
                {
                    var factory = this._factory;

                    if ( factory != null )
                    {
                        this._value = factory();

                        // Written last, and with a barrier, so that a reader that observes the null factory also
                        // observes the value.
                        Volatile.Write( ref this._factory, null );
                    }

                    return this._value!;
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
            => this.IsValueCreated ? this._value?.ToString() ?? "null" : "Value is not created.";
    }
}
