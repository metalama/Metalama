// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO;

namespace Metalama.Backstage.Licensing.Licenses.LicenseFields
{
    internal abstract class LicenseField
    {
        public object? Value { get; set; }

        public abstract void Write( BinaryWriter writer );

        internal abstract bool TryGetConstantLength( out byte length );

        /// <summary>
        /// Writes the length of the field if it has a constant length.
        /// </summary>
        /// <remarks>
        /// For fields with dynamic length, the length is written as part of the payload
        /// to the same position.
        /// </remarks>
        /// <param name="writer"></param>
        public void WriteConstantLength( BinaryWriter writer )
        {
            if ( !this.TryGetConstantLength( out var length ) )
            {
                return;
            }

            writer.Write( length );
        }

        public abstract void Read( BinaryReader reader );

        public override string ToString()
        {
            return this.Value?.ToString() ?? "null";
        }
    }
}