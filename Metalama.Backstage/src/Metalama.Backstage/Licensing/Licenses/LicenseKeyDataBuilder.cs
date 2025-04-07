// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing.Licenses;

[PublicAPI( "Used by the license generator web page and service." )]
public partial class LicenseKeyDataBuilder : ILicenseKeyData
{
    public static readonly string? CurrentVersion = AssemblyMetadataReader.GetInstance( typeof(License).Assembly ).PackageVersion;

    private readonly ImmutableSortedDictionary<LicenseFieldIndex, LicenseField>.Builder _fields;

    IReadOnlyDictionary<LicenseFieldIndex, LicenseField> ILicenseKeyData.Fields => this._fields;

    public LicenseKeyDataBuilder()
    {
        this._fields = ImmutableSortedDictionary<LicenseFieldIndex, LicenseField>.Empty.ToBuilder();
    }

    internal LicenseKeyDataBuilder( ImmutableSortedDictionary<LicenseFieldIndex, LicenseField> fields )
    {
        this._fields = fields.ToBuilder();
    }

    public LicenseKeyData Build()
        => new( this.Version, this._fields.ToImmutable() )
        {
            Product = this.Product,
            LicenseId = this.LicenseId,
            LicenseType = this.LicenseType,
            LicenseGuid = this.LicenseGuid,
            LicenseString = this.LicenseString
        };

    private object? GetFieldValue( LicenseFieldIndex index )
    {
        if ( this._fields.TryGetValue( index, out var licenseField ) )
        {
            return licenseField.Value;
        }
        else
        {
            return null;
        }
    }

    // Used for testing
    private void SetUnknownFieldValue( bool mustUnderstand, object value )
    {
        var index = (LicenseFieldIndex) (mustUnderstand ? 128 : 253);

        switch ( value.GetType().Name )
        {
            case nameof(Boolean):
                this.SetFieldValue<LicenseFieldBool>( index, value );

                break;

            case nameof(Byte):
                this.SetFieldValue<LicenseFieldByte>( index, value );

                break;

            case nameof(Int16):
                this.SetFieldValue<LicenseFieldInt16>( index, value );

                break;

            case nameof(Int32):
                this.SetFieldValue<LicenseFieldInt32>( index, value );

                break;

            case nameof(Int64):
                this.SetFieldValue<LicenseFieldInt64>( index, value );

                break;

            case nameof(DateTime):
                this.SetFieldValue<LicenseFieldDate>( index, ((DateTime) value).Date );
                this.SetFieldValue<LicenseFieldDateTime>( index, value );

                break;

            case nameof(String):
                this.SetFieldValue<LicenseFieldString>( index, value );

                break;

            case nameof(Byte) + "[]":
                this.SetFieldValue<LicenseFieldBytes>( index, value );

                break;

            case nameof(Guid):
                this.SetFieldValue<LicenseFieldGuid>( index, value );

                break;

            default:
                throw new ArgumentException( $"License fields of type {value.GetType()} are not supported." );
        }
    }

    private void SetFieldValue<T>( LicenseFieldIndex index, object? value )
        where T : LicenseField, new()
    {
        if ( value == null )
        {
            this._fields.Remove( index );
        }
        else
        {
            this._fields[index] = new T { Value = value };
        }
    }

    public string? LicenseString { get; set; }

    /// <summary>
    /// Gets the license version.
    /// </summary>
    public byte Version { get; private init; } = LicenseKeyDataSerializer.CurrentVersion;

    public Guid? LicenseGuid { get; set; }

    /// <summary>
    /// Gets or sets the type of license.
    /// </summary>
    public LicenseType LicenseType { get; set; }

    /// <summary>
    /// Gets or sets the licensed product.
    /// </summary>
    public LicenseProduct Product { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the current license.
    /// </summary>
    public int LicenseId { get; set; }

    /// <summary>
    /// Gets or sets the licensed namespace.
    /// </summary>
    public string? Namespace
    {
        get => (string?) this.GetFieldValue( LicenseFieldIndex.Namespace );
        set => this.SetFieldValue<LicenseFieldString>( LicenseFieldIndex.Namespace, value );
    }

    /// <summary>
    /// Gets or sets a value indicating whether the license usage can be audited.
    /// </summary>
    public bool? Auditable
    {
        get => (bool?) this.GetFieldValue( LicenseFieldIndex.Auditable );
        set => this.SetFieldValue<LicenseFieldBool>( LicenseFieldIndex.Auditable, value );
    }

    /// <summary>
    /// Gets or sets the licensed public key token.
    /// </summary>
    [Obsolete( "Redistribution keys are no longer supported." )]
    public byte[]? PublicKeyToken
    {
        get => (byte[]?) this.GetFieldValue( LicenseFieldIndex.PublicKeyToken );
        set => this.SetFieldValue<LicenseFieldBytes>( LicenseFieldIndex.PublicKeyToken, value );
    }

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public byte[]? Signature
    {
        get => (byte[]?) this.GetFieldValue( LicenseFieldIndex.Signature );
        set => this.SetFieldValue<LicenseFieldBytes>( LicenseFieldIndex.Signature, value );
    }

    /// <summary>
    /// Gets the identifier of the signature.
    /// </summary>
    public byte? SignatureKeyId
    {
        get => (byte?) this.GetFieldValue( LicenseFieldIndex.SignatureKeyId );
        internal set => this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.SignatureKeyId, value );
    }

    /// <summary>
    /// Gets or sets the allowed number of users.
    /// </summary>
    public short? UserNumber
    {
        get => (short?) this.GetFieldValue( LicenseFieldIndex.UserNumber );
        set => this.SetFieldValue<LicenseFieldInt16>( LicenseFieldIndex.UserNumber, value );
    }

    /// <summary>
    /// Gets or sets the date from which the license is valid.
    /// </summary>
    public DateTime? ValidFrom
    {
        get => (DateTime?) this.GetFieldValue( LicenseFieldIndex.ValidFrom );
        set => this.SetFieldValue<LicenseFieldDate>( LicenseFieldIndex.ValidFrom, value );
    }

    /// <summary>
    /// Gets or sets the date to which the license is valid.
    /// </summary>
    public DateTime? ValidTo
    {
        get => (DateTime?) this.GetFieldValue( LicenseFieldIndex.ValidTo );
        set => this.SetFieldValue<LicenseFieldDate>( LicenseFieldIndex.ValidTo, value );
    }

    public DateTime? SubscriptionEndDate
    {
        get => (DateTime?) this.GetFieldValue( LicenseFieldIndex.SubscriptionEndDate );
        set => this.SetFieldValue<LicenseFieldDate>( LicenseFieldIndex.SubscriptionEndDate, value );
    }

    /// <summary>
    /// Gets or sets the full name of the licensee.
    /// </summary>
    public string? Licensee
    {
        get => (string?) this.GetFieldValue( LicenseFieldIndex.Licensee );
        set => this.SetFieldValue<LicenseFieldString>( LicenseFieldIndex.Licensee, value );
    }

    /// <summary>
    /// Gets or sets the hash of the licensee name.
    /// </summary>
    public long? LicenseeHash
    {
        get => (long?) this.GetFieldValue( LicenseFieldIndex.LicenseeHash );
        set => this.SetFieldValue<LicenseFieldInt64>( LicenseFieldIndex.LicenseeHash, value );
    }

    /// <summary>
    /// Gets or sets the number of days in the grace period.
    /// </summary>
    public byte GraceDays
    {
        get => (byte?) this.GetFieldValue( LicenseFieldIndex.GraceDays ) ?? 30;
        set => this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.GraceDays, value );
    }

    /// <summary>
    /// Gets or sets the number of percents of additional users allowed during the grace period.
    /// </summary>
    public byte? GracePercent
    {
        get => (byte?) this.GetFieldValue( LicenseFieldIndex.GracePercent );
        set => this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.GracePercent, value );
    }

    /// <summary>
    /// Gets or sets the number of authorized devices per user.
    /// </summary>
    public byte? DevicesPerUser
    {
        get => (byte?) this.GetFieldValue( LicenseFieldIndex.DevicesPerUser );
        set => this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.DevicesPerUser, value );
    }

    [Obsolete( "Redistribution keys are no longer supported." )]
    public bool? AllowInheritance
    {
        get => (bool?) this.GetFieldValue( LicenseFieldIndex.AllowInheritance );
        set => this.SetFieldValue<LicenseFieldBool>( LicenseFieldIndex.AllowInheritance, value );
    }

    public bool? LicenseServerEligible
    {
        get => (bool?) this.GetFieldValue( LicenseFieldIndex.LicenseServerEligible );
        set => this.SetFieldValue<LicenseFieldBool>( LicenseFieldIndex.LicenseServerEligible, value );
    }

    public string? OriginVersion
    {
        get => (string?) this.GetFieldValue( LicenseFieldIndex.OriginVersion );
        init => this.SetFieldValue<LicenseFieldString>( LicenseFieldIndex.OriginVersion, value );
    }

    public ServicingPhase ServicingPhase
    {
        get
            => this.GetFieldValue( LicenseFieldIndex.ServicingPhase ) switch
            {
                null => ServicingPhase.Current,
                byte supportLevel => (ServicingPhase) supportLevel,
                _ => throw new InvalidCastException( "Invalid support level." )
            };

        set
        {
            if ( value != ServicingPhase.Current )
            {
                this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.ServicingPhase, (byte) value );
            }
            else
            {
                this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.ServicingPhase, null );
            }
        }
    }

    public LicenseGeneration Generation
    {
        get
            => this.GetFieldValue( LicenseFieldIndex.Generation ) switch
            {
                null => 0,
                byte generation => (LicenseGeneration) generation,
                _ => throw new InvalidCastException( "Invalid generation." )
            };

        set
        {
            if ( value != LicenseGeneration.None )
            {
                this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.Generation, (byte) value );
            }
            else
            {
                this.SetFieldValue<LicenseFieldByte>( LicenseFieldIndex.Generation, null );
            }
        }
    }

    public Version? MinPostSharpVersion
    {
        get
        {
            var minPostSharpVersionString = (string?) this.GetFieldValue( LicenseFieldIndex.MinPostSharpVersion );

            return minPostSharpVersionString == null ? null : System.Version.Parse( minPostSharpVersionString );
        }
    }

    // Used for testing
    internal object? UnknownMustUnderstandField
    {
        get => null;
        set => this.SetUnknownFieldValue( true, value! );
    }

    // Used for testing
    internal object? UnknownOptionalField
    {
        get => null;
        set => this.SetUnknownFieldValue( false, value! );
    }
}