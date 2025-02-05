// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

        protected override bool TryGetConstantLength( out byte length )
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