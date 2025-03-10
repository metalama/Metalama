// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal class LicenseFieldDateTime : LicenseField
    {
        public override void Write( BinaryWriter writer )
        {
            var data = ((DateTime) this.Value!).ToUniversalTime().ToBinary();
            writer.Write( data );
        }

        internal override bool TryGetConstantLength( out byte length )
        {
            length = sizeof(long);

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            var data = reader.ReadInt64();
            this.Value = DateTime.FromBinary( data ).ToLocalTime();
        }
    }
}