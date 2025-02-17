// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                case LicenseFieldIndex.Signature:
                    if ( includeAll )
                    {
                        goto default;
                    }

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
        if ( data.LicenseType == LicenseType.Anonymous )
        {
            return false;
        }

        if ( data is { LicenseType: LicenseType.Community or LicenseType.Evaluation } )
        {
            return false;
        }
#pragma warning restore CS0618

        return true;
    }
}