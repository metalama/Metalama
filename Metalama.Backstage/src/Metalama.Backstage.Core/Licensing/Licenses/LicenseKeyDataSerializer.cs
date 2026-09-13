// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses;

internal static class LicenseKeyDataSerializer
{
    public const int CurrentVersion = 2;

    public static void Write( this ILicenseKeyData data, BinaryWriter writer, bool includeAll )
    {
        writer.Write( data.Version );
        writer.Write( data.LicenseId );
        writer.Write( (byte) data.LicenseType );
        writer.Write( (byte) data.Product );

        foreach ( var pair in data.Fields )
        {
            switch ( pair.Key )
            {
                case LicenseFieldIndex.Signature when !includeAll:
                    continue;

                default:
                    writer.Write( (byte) pair.Key );

                    if ( pair.Key.IsPrefixedByLength() )
                    {
                        pair.Value.WriteConstantLength( writer );
                    }

                    pair.Value.Write( writer );

                    break;
            }
        }
    }

    public static byte[] GetSignedBuffer( this ILicenseKeyData data )
    {
        // Write the license to a buffer without the key.
        var memoryStream = new MemoryStream();

        using ( var binaryWriter = new BinaryWriter( memoryStream ) )
        {
            Write( data, binaryWriter, false );
        }

        var signedBuffer = memoryStream.ToArray();

        return signedBuffer;
    }

    public static bool RequiresSignature( this ILicenseKeyData data )
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if ( data is { LicenseType: LicenseType.Community or LicenseType.Evaluation or LicenseType.Anonymous } )
        {
            return false;
        }
#pragma warning restore CS0618

        return true;
    }
}