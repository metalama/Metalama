// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            var date = (DateTime) this.Value!;

            if ( date < _referenceDate )
            {
                throw new InvalidOperationException( $"The date must be later than '{_referenceDate}'." );
            }

            var days = (ushort) date.Subtract( _referenceDate ).TotalDays;
            writer.Write( days );
        }

        internal override bool TryGetConstantLength( out byte length )
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