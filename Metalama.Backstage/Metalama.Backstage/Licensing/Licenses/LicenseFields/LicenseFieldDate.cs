// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    [Serializable]
    internal class LicenseFieldDate : LicenseField
    {
        private static readonly DateTime _referenceDate = new( 2010, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        public override void Write( BinaryWriter writer )
        {
            var days = (ushort) ((DateTime) this.Value!).Subtract( _referenceDate ).TotalDays;
            writer.Write( days );
        }

        protected override bool TryGetConstantLength( out byte length )
        {
            length = sizeof(ushort);

            return true;
        }

        public override void Read( BinaryReader reader )
        {
            var days = reader.ReadUInt16();
            this.Value = _referenceDate.AddDays( days );
        }
    }
}