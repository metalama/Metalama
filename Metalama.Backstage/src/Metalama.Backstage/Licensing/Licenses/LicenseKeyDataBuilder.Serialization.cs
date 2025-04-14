// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using Metalama.Backstage.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Metalama.Backstage.Licensing.Licenses
{
    public partial class LicenseKeyDataBuilder
    {
        /// <summary>
        /// Since PostSharp 6.5.17, 6.8.10, and 6.9.3 the <see cref="MinPostSharpVersion" /> is no longer checked.
        /// For compatibility with previous version, all licenses with features introduced since these versions
        /// should have the <see cref="MinPostSharpVersion" />
        /// set to <see cref="_minPostSharpVersionValidationRemovedPostSharpVersion" />.
        /// </summary>
        private static readonly Version _minPostSharpVersionValidationRemovedPostSharpVersion = new( 6, 9, 3 );

        public static LicenseKeyDataBuilder Deserialize( BinaryReader reader )
        {
            LicenseFieldIndex index;

            var data = new LicenseKeyDataBuilder()
            {
                Version = reader.ReadByte(),
                LicenseId = reader.ReadInt32(),
                LicenseType = (LicenseType) reader.ReadByte(),
                Product = (LicenseProduct) reader.ReadByte()
            };

            while ( (index = (LicenseFieldIndex) reader.ReadByte()) != LicenseFieldIndex.End )
            {
                LicenseField licenseField;

                var shouldReadFieldLength = index.IsPrefixedByLength();

                switch ( index )
                {
                    case LicenseFieldIndex.SignatureKeyId:
                    case LicenseFieldIndex.GraceDays:
                    case LicenseFieldIndex.GracePercent:
                    case LicenseFieldIndex.DevicesPerUser:
                    case LicenseFieldIndex.ServicingPhase:
                    case LicenseFieldIndex.Generation:
                        licenseField = new LicenseFieldByte();

                        break;

                    case LicenseFieldIndex.Auditable:
                    case LicenseFieldIndex.AllowInheritance:
                    case LicenseFieldIndex.LicenseServerEligible:
                        licenseField = new LicenseFieldBool();

                        break;

                    case LicenseFieldIndex.Licensee:
                    case LicenseFieldIndex.Namespace:
                    case LicenseFieldIndex.MinPostSharpVersion:
                    case LicenseFieldIndex.OriginVersion:
                        shouldReadFieldLength = false;
                        licenseField = new LicenseFieldString();

                        break;

                    case LicenseFieldIndex.UserNumber:
                        licenseField = new LicenseFieldInt16();

                        break;

                    case LicenseFieldIndex.PublicKeyToken:
                    case LicenseFieldIndex.Signature:
                        shouldReadFieldLength = false;
                        licenseField = new LicenseFieldBytes();

                        break;

                    case LicenseFieldIndex.ValidFrom:
                    case LicenseFieldIndex.ValidTo:
                    case LicenseFieldIndex.SubscriptionEndDate:
                        licenseField = new LicenseFieldDate();

                        break;

                    case LicenseFieldIndex.LicenseeHash:
#pragma warning disable CS0618 // Type or member is obsolete
                    case LicenseFieldIndex.Features:
#pragma warning restore CS0618 // Type or member is obsolete
                        licenseField = new LicenseFieldInt64();

                        break;

                    default:
                        if ( !index.IsPrefixedByLength() )
                        {
                            throw new InvalidLicenseException( "Unexpected license field." );
                        }

                        // This is an unknown field.
                        // We read its data to
                        // - Validate that we do understand the must-understand fields.
                        // - Keep the license integrity, e.g. for cloning or signature verification.
                        shouldReadFieldLength = false;
                        licenseField = new LicenseFieldBytes();

                        break;
                }

                if ( shouldReadFieldLength )
                {
                    // If this is known constant-length field that is prefixed by length, read the length.
                    int l = reader.ReadByte();

                    if ( licenseField.TryGetConstantLength( out var expected ) && l != expected )
                    {
                        throw new InvalidLicenseException( $"Unexpected length of field {index}. Expected {expected}, got {l}." );
                    }
                }

                licenseField.Read( reader );
                data._fields.Add( index, licenseField );
            }

            // this works only for base streams that implement Length and Position but since we always use MemoryStream it is ok.
            if ( reader.BaseStream.Length > reader.BaseStream.Position )
            {
                throw new InvalidLicenseException( "License is too long." );
            }

            return data;
        }

        /// <summary>
        /// Sets the <see cref="MinPostSharpVersion"/> to <see cref="_minPostSharpVersionValidationRemovedPostSharpVersion"/>
        /// if the license key data is not backward compatible with PostSharp versions
        /// prior to <see cref="_minPostSharpVersionValidationRemovedPostSharpVersion"/>.
        /// </summary>
        private void SetMinPostSharpVersionIfRequired()
        {
            // Returns <c>true</c> if the licensed product has been present prior to PostSharp 6.5.17/6.8.10/6.9.3.
            static bool IsPostSharpProduct( LicenseProduct product )
            {
                switch ( product )
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    case LicenseProduct.PostSharp20:
                    case LicenseProduct.PostSharpUltimate1:
#pragma warning restore CS0618 // Type or member is obsolete
                    case LicenseProduct.PostSharpUltimate:
                    case LicenseProduct.PostSharpFramework:
                    case LicenseProduct.PostSharpDiagnosticsLibrary:
                    case LicenseProduct.PostSharpModelLibrary:
                    case LicenseProduct.PostSharpThreadingLibrary:
                    case LicenseProduct.PostSharpCachingLibrary:
                        return true;

                    default:
                        return false;
                }
            }

            if ( IsPostSharpProduct( this.Product ) && this._fields.Keys.Any( i => i.IsPrefixedByLength() ) )
            {
                if ( this.MinPostSharpVersion == null )
                {
                    this.SetFieldValue<LicenseFieldString>(
                        LicenseFieldIndex.MinPostSharpVersion,
                        _minPostSharpVersionValidationRemovedPostSharpVersion.ToString() );
                }
                else if ( this.MinPostSharpVersion != _minPostSharpVersionValidationRemovedPostSharpVersion )
                {
                    throw new InvalidOperationException(
                        $"The license contains products or fields introduced " +
                        $"after PostSharp {_minPostSharpVersionValidationRemovedPostSharpVersion}. " +
                        $"However, the {nameof(this.MinPostSharpVersion)} property is not null " +
                        $"or '{_minPostSharpVersionValidationRemovedPostSharpVersion}' as expected." );
                }
            }
        }

        /// <summary>
        /// Serializes the current license key data into a string and sets the value to the <see cref="LicenseString"/> property.
        /// </summary>
        internal string SerializeToLicenseString()
        {
            var memoryStream = new MemoryStream();

            using ( var binaryWriter = new BinaryWriter( memoryStream ) )
            {
                this.Write( binaryWriter, true );
                binaryWriter.Write( (byte) LicenseFieldIndex.End );
            }

            string prefix;

            if ( this.LicenseGuid.HasValue )
            {
                prefix = Base32.ToBase32String( this.LicenseGuid.Value.ToByteArray(), 0 );
            }
            else
            {
                prefix = this.LicenseId.ToString( CultureInfo.InvariantCulture );
            }

            this.LicenseString = prefix + "-" + Base32.ToBase32String( memoryStream.ToArray(), 0 );

            return this.LicenseString;
        }

        /// <summary>
        /// Serializes the current license key data into a string.
        /// </summary>
        /// <returns>A string representing the current license key data.</returns>
        public string Serialize()
        {
            this.SetMinPostSharpVersionIfRequired();
            this.SerializeToLicenseString();

            return this.LicenseString!;
        }

        /// <summary>
        /// Signs the current license.
        /// </summary>
        public string SignAndSerialize( LicensingAuthority authority )
        {
            this.SetMinPostSharpVersionIfRequired();
            this.Sign( authority );
            this.SerializeToLicenseString();

            return this.LicenseString!;
        }

        /// <summary>
        /// Signs the current license.
        /// </summary>
        private void Sign( LicensingAuthority authority )
        {
            // It's critical to set SignatureKeyId before getting the signed buffer
            // because the field is a part of the signed buffer.
            this.SignatureKeyId = authority.SignKeyId;
            var signedBuffer = this.GetSignedBuffer();
            authority.Sign( signedBuffer, out var signature );
            this.Signature = signature;
        }

        public static bool TryDeserialize(
            string licenseKey,
            [MaybeNullWhen( false )] out LicenseKeyDataBuilder data,
            [MaybeNullWhen( true )] out string errorMessage )
        {
            try
            {
                Guid? licenseGuid = null;

                // Parse the license key prefix.
#pragma warning disable CA1307
                var firstDash = licenseKey.IndexOf( '-' );
#pragma warning restore CA1307

                if ( firstDash < 0 )
                {
                    throw new InvalidLicenseException( $"License header not found for license {{{licenseKey}}}." );
                }

                var prefix = licenseKey.Substring( 0, firstDash );

                if ( !int.TryParse( prefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var licenseId ) )
                {
                    // If this is not an integer, this may be a GUID.
                    licenseGuid = new Guid( Base32.FromBase32String( prefix ) );
                }

                var licenseBytes = Base32.FromBase32String( licenseKey.Substring( firstDash + 1 ) );

                using var memoryStream = new MemoryStream( licenseBytes );
                using var reader = new BinaryReader( memoryStream );

                data = Deserialize( reader );

                if ( data.LicenseId != licenseId )
                {
                    throw new InvalidLicenseException( $"The license id in the body ({licenseId}) does not match the header for license {{{licenseKey}}}." );
                }

                data.LicenseGuid = licenseGuid;
                data.LicenseString = licenseKey;
                errorMessage = null;

                return true;
            }
            catch ( Exception e )
            {
                data = null;
                errorMessage = e.Message;

                return false;
            }
        }
    }
}