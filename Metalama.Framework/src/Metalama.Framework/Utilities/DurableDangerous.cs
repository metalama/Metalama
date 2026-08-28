// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// Wraps a value that is asserted to be durable, that is, safe to be held across compilations, without the
    /// assertion being verified. The analyzer treats this type as durable whatever <typeparamref name="T"/> is.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the escape hatch for a member whose durability holds in fact but cannot be established from its
    /// declared type, most commonly because the type comes from an assembly that cannot be annotated. The assertion
    /// is the responsibility of whoever writes it.
    /// </para>
    /// <para>
    /// The <c>Dangerous</c> suffix follows the convention used elsewhere in this codebase for a value whose safety
    /// the reader must establish rather than assume. Prefer this wrapper over a diagnostic suppression, because it
    /// appears in the signature of the member, survives refactoring and can be found by a search for the type name.
    /// </para>
    /// <para>
    /// Prefer, in order: making the member genuinely durable, for example by storing a
    /// <see cref="Metalama.Framework.Code.SerializableDeclarationId"/> or an
    /// <see cref="Metalama.Framework.Code.IDurableRef{T}"/>; applying <see cref="DurableAttribute"/> to the member so
    /// that its assignments are verified; and only then this wrapper.
    /// </para>
    /// </remarks>
    [PublicAPI]
    public readonly struct DurableDangerous<T> : IEquatable<DurableDangerous<T>>
    {
        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DurableDangerous{T}"/> struct.
        /// </summary>
        /// <param name="value">The value asserted to be durable.</param>
        public DurableDangerous( T value )
        {
            this.Value = value;
        }

        /// <summary>
        /// Unwraps the value.
        /// </summary>
        /// <param name="value">The wrapper.</param>
        public static implicit operator T( DurableDangerous<T> value ) => value.Value;

        /// <summary>
        /// Wraps a value, asserting that it is durable.
        /// </summary>
        /// <param name="value">The value asserted to be durable.</param>
        public static implicit operator DurableDangerous<T>( T value ) => new( value );

        /// <inheritdoc />
        public bool Equals( DurableDangerous<T> other ) => EqualityComparer<T>.Default.Equals( this.Value, other.Value );

        /// <inheritdoc />
        public override bool Equals( object? obj ) => obj is DurableDangerous<T> other && this.Equals( other );

        /// <inheritdoc />
        public override int GetHashCode() => this.Value == null ? 0 : EqualityComparer<T>.Default.GetHashCode( this.Value );

        /// <summary>
        /// Determines whether two wrappers hold equal values.
        /// </summary>
        public static bool operator ==( DurableDangerous<T> left, DurableDangerous<T> right ) => left.Equals( right );

        /// <summary>
        /// Determines whether two wrappers hold values that are not equal.
        /// </summary>
        public static bool operator !=( DurableDangerous<T> left, DurableDangerous<T> right ) => !left.Equals( right );

        /// <inheritdoc />
        public override string? ToString() => this.Value?.ToString();
    }
}
