// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Diagnostics
{
    // Public because of TryMetalama.
    public sealed class NonLocalizedString : LocalizableString
    {
        private readonly string _message;

        public NonLocalizedString( string message, object?[]? arguments = null )
        {
            this._message = message;
            this.Arguments = arguments ?? [];
        }

        /// <summary>
        /// Gets the arguments that the message is formatted with, so that a caller can verify what they hold.
        /// </summary>
        /// <remarks>
        /// A diagnostic formats its message lazily, so these arguments live as long as the diagnostic. The
        /// design-time pipeline keeps a diagnostic for far longer than the run that reported it, which is why
        /// <see cref="DurableDiagnostic"/> asserts in a debug build that they reach no compilation.
        /// </remarks>
        internal object?[] Arguments { get; }

        protected override string GetText( IFormatProvider? formatProvider )
        {
            try
            {
                return this.Arguments.Length == 0
                    ? this._message
                    : string.Format( MetalamaStringFormatter.Instance, this._message, this.Arguments );
            }
            catch ( FormatException e )
            {
                return $"(Formatting exception when formatting the message \"{this._message}\": {e.Message})";
            }
        }

        protected override int GetHash()
        {
            var hashCode = default(HashCode);
            hashCode.Add( this._message );

            foreach ( var arg in this.Arguments )
            {
                hashCode.Add( arg );
            }

            return hashCode.ToHashCode();
        }

        protected override bool AreEqual( object? other )
        {
            if ( other is not NonLocalizedString otherLocalizedString )
            {
                return false;
            }

            if ( !this._message.Equals( otherLocalizedString._message, StringComparison.Ordinal ) )
            {
                // Coverage: ignore.
                return false;
            }

            if ( this.Arguments.Length != otherLocalizedString.Arguments.Length )
            {
                // Coverage: ignore.
                return false;
            }

            for ( var i = 0; i < this.Arguments.Length; i++ )
            {
                if ( !Equals( this.Arguments[i], otherLocalizedString.Arguments[i] ) )
                {
                    return false;
                }
            }

            return true;
        }
    }
}