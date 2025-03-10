// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        internal override bool TryGetConstantLength( out byte length )
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