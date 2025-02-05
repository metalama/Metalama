// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal sealed class SerializationBinaryReader
    {
        private readonly BinaryReader _reader;
        private readonly List<string> _strings = new();
        private readonly List<string> _dottedStrings = new();

        public SerializationBinaryReader( BinaryReader reader )
        {
            this._reader = reader;
        }

        public byte ReadByte()
        {
            return this._reader.ReadByte();
        }

        public string? ReadString()
        {
            int header = this.ReadCompressedInteger();

            switch ( header )
            {
                case -1:
                    return null;

                case < 0:
                    {
                        var index = -header - SerializationBinaryWriter.FirstStringIndex;

                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if ( index < 0 || index > this._strings.Count )
                        {
                            throw new CompileTimeSerializationException( "Invalid serialized stream: invalid string identifier." );
                        }

                        return this._strings[index];
                    }

                default:
                    {
                        var bytes = this._reader.ReadBytes( header );
                        var value = Encoding.UTF8.GetString( bytes, 0, bytes.Length );
                        this._strings.Add( value );

                        return value;
                    }
            }
        }

        public DottedString ReadDottedString()
        {
            int header = this.ReadCompressedInteger();

            switch ( header )
            {
                case -1:
                    return DottedString.Null;

                case < 0:
                    {
                        var index = -header - SerializationBinaryWriter.FirstStringIndex;

                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if ( index < 0 || index > this._dottedStrings.Count )
                        {
                            throw new CompileTimeSerializationException( "Invalid serialized stream: invalid string identifier." );
                        }

                        return this._dottedStrings[index];
                    }

                default:
                    {
                        var bytes = this._reader.ReadBytes( header );
                        var value = Encoding.UTF8.GetString( bytes, 0, bytes.Length );
                        var parent = this.ReadDottedString();

                        if ( !parent.IsNull )
                        {
                            value = parent + "." + value;
                        }

                        this._dottedStrings.Add( value );

                        return value;
                    }
            }
        }

        public Integer ReadCompressedInteger()
        {
            var header = this._reader.ReadByte();
            var isNegative = (header & 0x80) != 0;
            ulong value;

            switch ( header & 0x70 )
            {
                // Unsigned
                case 0x00:
                    value = (ulong) (header & 0x0F);

                    break;

                case 0x10:
                    value = ((uint) (header & 0x0F) << 8) | this._reader.ReadByte();

                    break;

                case 0x20:
                    value = ((uint) (header & 0x0F) << 16) | this._reader.ReadUInt16();

                    break;

                case 0x30:
                    value = ((ulong) (header & 0x0F) << 32) | this._reader.ReadUInt32();

                    break;

                case 0x40:
                    value = this._reader.ReadUInt64();

                    break;

                default:
                    throw new CompileTimeSerializationException( "Cannot decode a compressed integer." );
            }

            return new Integer( value, isNegative );
        }

        public double ReadDouble()
        {
            return this._reader.ReadDouble();
        }

        public float ReadSingle()
        {
            return this._reader.ReadSingle();
        }

        public sbyte ReadSByte()
        {
            return this._reader.ReadSByte();
        }
    }
}