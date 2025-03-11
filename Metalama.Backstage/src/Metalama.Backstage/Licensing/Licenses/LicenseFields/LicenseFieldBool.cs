// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldBool : LicenseField
    {
        private bool _isBuggy;

        public override void Write( BinaryWriter writer )
        {
            if ( this._isBuggy )
            {
                writer.Write( (bool) this.Value! ? 1 : 0 );
            }
            else
            {
                writer.Write( (byte) ((bool) this.Value! ? 1 : 0) );
            }
        }

        internal override bool TryGetConstantLength( out byte length )
        {
            if ( this._isBuggy )
            {
                throw new InvalidOperationException( "Boolean license fields requiring the length to be serialized should no longer be buggy." );
            }

            length = sizeof(byte);

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            this.Value = reader.ReadByte() != 0;

            // Check if the field was emitted as a int as a result of a bug.
            this._isBuggy = false;

            if ( reader.BaseStream.Length > reader.BaseStream.Position )
            {
                if ( reader.ReadByte() == 0 )
                {
                    reader.ReadByte();
                    reader.ReadByte();
                    this._isBuggy = true;
                }
                else
                {
                    reader.BaseStream.Seek( -1, SeekOrigin.Current );
                }
            }
        }
    }
}