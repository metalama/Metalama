// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldString : LicenseField
    {
        public override void Write( BinaryWriter writer )
        {
            var bytes = Encoding.UTF8.GetBytes( ((string) this.Value!).Normalize() );

            if ( bytes.Length > 255 )
            {
                throw new InvalidOperationException( "Cannot have strings of more than 255 characters." );
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
            var bytes = reader.ReadBytes( reader.ReadByte() );
            this.Value = Encoding.UTF8.GetString( bytes );
        }
    }
}