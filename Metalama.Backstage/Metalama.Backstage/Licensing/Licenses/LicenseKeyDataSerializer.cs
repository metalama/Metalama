// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using System.IO;

namespace Metalama.Backstage.Licensing.Licenses;

internal static class LicenseKeyDataSerializer
{
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
#pragma warning restore CS0618 // Type or member is obsolete
        {
            return false;
        }

        if ( data.LicenseId == 0 && (data.LicenseType == LicenseType.Essentials ||
                                     data.LicenseType == LicenseType.Evaluation) )
        {
            return false;
        }

        if ( data.Product == LicensedProduct.MetalamaFree )
        {
            return false;
        }

        return true;
    }
}