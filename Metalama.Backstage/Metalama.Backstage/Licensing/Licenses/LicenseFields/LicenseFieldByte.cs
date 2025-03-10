// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal sealed class LicenseFieldByte : LicenseField
    {
        public override void Write( BinaryWriter writer )
        {
            writer.Write( (byte) this.Value! );
        }

        internal override bool TryGetConstantLength( out byte length )
        {
            length = sizeof(byte);

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            this.Value = reader.ReadByte();
        }
    }
}