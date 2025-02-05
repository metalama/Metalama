// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Diagnostics
{
    // Public because of TryMetalama.
    public sealed class NonLocalizedString : LocalizableString
    {
        private readonly string _message;
        private readonly object?[] _arguments;

        public NonLocalizedString( string message, object?[]? arguments = null )
        {
            this._message = message;
            this._arguments = arguments ?? [];
        }

        protected override string GetText( IFormatProvider? formatProvider )
        {
            try
            {
                return this._arguments.Length == 0
                    ? this._message
                    : string.Format( MetalamaStringFormatter.Instance, this._message, this._arguments );
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

            foreach ( var arg in this._arguments )
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

            if ( this._arguments.Length != otherLocalizedString._arguments.Length )
            {
                // Coverage: ignore.
                return false;
            }

            for ( var i = 0; i < this._arguments.Length; i++ )
            {
                if ( !Equals( this._arguments[i], otherLocalizedString._arguments[i] ) )
                {
                    return false;
                }
            }

            return true;
        }
    }
}