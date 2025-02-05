// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

        protected override bool TryGetConstantLength( out byte length )
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