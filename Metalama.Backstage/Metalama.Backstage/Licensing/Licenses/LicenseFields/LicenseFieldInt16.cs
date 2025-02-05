// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldInt16 : LicenseField
    {
        public override void Write( BinaryWriter writer )
        {
            writer.Write( (short) this.Value! );
        }

        protected override bool TryGetConstantLength( out byte length )
        {
            length = sizeof(short);

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            this.Value = reader.ReadInt16();
        }
    }
}