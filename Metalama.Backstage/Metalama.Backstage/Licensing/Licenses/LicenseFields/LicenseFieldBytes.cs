// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Utilities;
using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldBytes : LicenseField
    {
        public override void Write( BinaryWriter writer )
        {
            var bytes = (byte[]) this.Value!;

            if ( bytes.Length > 255 )
            {
                throw new InvalidOperationException( "Cannot have buffers of more than 255 bytes." );
            }

            writer.Write( (byte) bytes.Length );
            writer.Write( bytes );
        }

        protected override bool TryGetConstantLength( out byte length )
        {
            // The length of this field is variable.
            length = 0;

            return false;
        }

        public override void Read( BinaryReader reader )
        {
            this.Value = reader.ReadBytes( reader.ReadByte() );
        }

        public override string ToString()
        {
            if ( this.Value == null )
            {
                return "null";
            }

            return HexHelper.FormatBytes( (byte[]) this.Value );
        }
    }
}