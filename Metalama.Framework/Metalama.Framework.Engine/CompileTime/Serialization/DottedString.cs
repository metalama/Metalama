// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    /// <summary>
    /// Encapsulates dotted strings such as namespaces and type names, so their
    /// serialization by <see cref="CompileTimeSerializer"/> can be optimized.
    /// </summary>
    internal readonly struct DottedString : IEquatable<DottedString>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DottedString"/> struct.
        /// </summary>
        /// <param name="value">Value.</param>
        private DottedString( string value )
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets an instance of <see cref="DottedString"/> representing a <c>null</c> string.
        /// </summary>
        public static readonly DottedString Null = new( null! );

        /// <summary>
        /// Gets a value indicating whether the current <see cref="DottedString"/> represents a <c>null</c> string.
        /// </summary>
        public bool IsNull => this.Value == null;

        /// <summary>
        /// Gets the string encapsulated by the current <see cref="DottedString"/>.
        /// </summary>
        private string Value { get; }

        /// <summary>
        /// Converts a <see cref="DottedString"/> into a <see cref="string"/>.
        /// </summary>
        /// <param name="dottedString">A <see cref="DottedString"/>.</param>
        /// <returns>The <see cref="string"/> encapsulated by <paramref name="dottedString"/>.</returns>
        public static implicit operator string( DottedString dottedString ) => dottedString.ToString();

        /// <summary>
        /// Converts a <see cref="string"/> into a <see cref="DottedString"/>.
        /// </summary>
        /// <param name="str">A <see cref="string"/>.</param>
        /// <returns>A <see cref="DottedString"/> encapsulating <paramref name="str"/>.</returns>
        public static implicit operator DottedString( string str ) => new( str );

        /// <summary>
        /// Determines whether the current<see cref="DottedString"/> is equal to another one.
        /// </summary>
        /// <param name="other">Another <see cref="DottedString"/>.</param>
        /// <returns><c>true</c> if the current <see cref="DottedString"/> equals <c>other</c>, otherwise <c>false</c>.</returns>
        public bool Equals( DottedString other ) => string.Equals( this.Value, other.Value, StringComparison.Ordinal );

        /// <inheritdoc />
        public override int GetHashCode() => this.Value == null ? 0 : this.GetHashCode();

        /// <inheritdoc />
        public override bool Equals( object? obj )
        {
            if ( obj is DottedString other )
            {
                return this.Equals( other );
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public override string ToString() => this.Value;

        /// <summary>
        /// Operator.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==( DottedString left, DottedString right ) => left.Equals( right );

        /// <summary>
        /// Operator.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=( DottedString left, DottedString right ) => !(left == right);
    }
}