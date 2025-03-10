// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldGuid : LicenseField
    {
        private const byte _sizeOfGuidByteArray = 16;

        public override void Write( BinaryWriter writer )
        {
            var guid = (Guid) this.Value!;
            writer.Write( guid.ToByteArray() );
        }

        internal override bool TryGetConstantLength( out byte length )
        {
            length = _sizeOfGuidByteArray;

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            this.Value = new Guid( reader.ReadBytes( _sizeOfGuidByteArray ) );
        }

        public override string ToString()
        {
            if ( this.Value == null )
            {
                return "null";
            }

            return ((Guid) this.Value).ToString();
        }
    }
}