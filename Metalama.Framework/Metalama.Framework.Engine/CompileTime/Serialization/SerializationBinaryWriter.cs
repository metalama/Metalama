// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal sealed class SerializationBinaryWriter
    {
        // We use 2 as the first index for cached strings or dotted strings because indexes are stored as negative values, and
        // 0 represents an empty string, and -1 represents a null string.
        public const int FirstStringIndex = 2;

        private readonly BinaryWriter _writer;
        private readonly Dictionary<string, int> _strings = new( 64, StringComparer.Ordinal );
        private readonly Dictionary<string, int> _dottedStrings = new( 64, StringComparer.Ordinal );

        public SerializationBinaryWriter( BinaryWriter writer )
        {
            this._writer = writer;
        }

        public void WriteCompressedInteger( Integer integer )
        {
            var value = integer.AbsoluteValue;
            var isNegative = integer.IsNegative;
            var signBit = (byte) (isNegative ? 0x80 : 0);

            // For unsigned compressed integers, the top 3 bits of the header are used to store the integer lengths.
            if ( (value & 0x0f) == value )
            {
                this._writer.Write( (byte) (signBit | (byte) value) );
            }
            else if ( (value & 0x0fff) == value )
            {
                this._writer.Write( (byte) (0x10 | signBit | (byte) (value >> 8)) );
                this._writer.Write( (byte) (value & 0xff) );
            }
            else if ( (value & 0x0fffff) == value )
            {
                this._writer.Write( (byte) (0x20 | signBit | (byte) (value >> 16)) );
                this._writer.Write( (ushort) (value & 0xffff) );
            }
            else if ( (value & 0x0fffffffff) == value )
            {
                this._writer.Write( (byte) (0x30 | signBit | (byte) (value >> 32)) );
                this._writer.Write( (uint) (value & 0xffffffff) );
            }
            else
            {
                this._writer.Write( (byte) (0x40 | signBit) );
                this._writer.Write( value );
            }
        }

        public void WriteByte( byte value )
        {
            this._writer.Write( value );
        }

        public void WriteDouble( double value )
        {
            this._writer.Write( value );
        }

        public void WriteString( string? value )
        {
            if ( value == null )
            {
                this.WriteCompressedInteger( -1 );
            }
            else if ( this._strings.TryGetValue( value, out var id ) )
            {
                this.WriteCompressedInteger( -id );
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes( value );
                this.WriteCompressedInteger( bytes.Length );
                this._writer.Write( bytes );

                this._strings.Add( value, this._strings.Count + FirstStringIndex );
            }
        }

        public void WriteSByte( sbyte value )
        {
            this._writer.Write( value );
        }

        public void WriteDottedString( string? value )
        {
            if ( value == null )
            {
                this.WriteCompressedInteger( -1 );
            }
            else if ( this._dottedStrings.TryGetValue( value, out var id ) )
            {
                this.WriteCompressedInteger( -id );
            }
            else
            {
                var lastDot = value.LastIndexOf( '.' );
                string name;
                string? scope;

                if ( lastDot < 0 )
                {
                    name = value;
                    scope = null;
                }
                else
                {
                    name = value.Substring( lastDot + 1 );
                    scope = value.Substring( 0, lastDot );
                }

                var bytes = Encoding.UTF8.GetBytes( name );
                this.WriteCompressedInteger( bytes.Length );
                this._writer.Write( bytes );

                this.WriteDottedString( scope );

                this._dottedStrings.Add( value, this._dottedStrings.Count + FirstStringIndex );
            }
        }

        public void WriteSingle( float value )
        {
            this._writer.Write( value );
        }
    }
}