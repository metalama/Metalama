// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable StringLiteralTypo

using JetBrains.Annotations;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;

#pragma warning disable SA1203

namespace Metalama.Backstage.Testing;

[PublicAPI]
public sealed class TestLicenseKeyProvider
{
    private readonly ConcurrentDictionary<string, string> _cachedLicenses = new();

    public LicensingAuthority Authority { get; } = LicensingAuthority.GetTestAuthority();

    public const string NamespaceConstraint = "TestNamespace";

    private string GenerateLicenseKey( int id, Action<LicenseKeyDataBuilder> action, bool sign = true, bool endSubscription = true )
    {
        var builder = new LicenseKeyDataBuilder { LicenseId = id };

        if ( endSubscription )
        {
            builder.SubscriptionEndDate = this.DefaultSubscriptionExpirationDate;
        }

        action( builder );

        // Ensure we always return the same license key for the same input because subsequent signing of the same thing
        // do not return the same signature.
        var hash = HashUtilities.HashToString( builder.GetSignedBuffer() ) + sign;

        return this._cachedLicenses.GetOrAdd(
            hash,
            _ =>
            {
                if ( sign && builder.RequiresSignature() )
                {
                    return builder.SignAndSerialize( this.Authority );
                }
                else
                {
                    return builder.SerializeToLicenseString();
                }
            } );
    }

    private string GenerateLicenseKey(
        int id,
        LicenseProduct product,
        LicenseType type = LicenseType.Business,
        LicenseGeneration generation = LicenseGeneration.Current,
        ServicingPhase servicing = ServicingPhase.Default,
        bool sign = true,
        bool endSubscription = true )
        => this.GenerateLicenseKey(
            id,
            license =>
            {
                license.Product = product;
                license.LicenseType = type;

                if ( generation != LicenseGeneration.None )
                {
                    license.Generation = generation;
                }

                if ( servicing != ServicingPhase.Default )
                {
                    license.ServicingPhase = servicing;
                }
            },
            sign,
            endSubscription );

    public string PostSharpEssentials => this.GenerateLicenseKey( 1, LicenseProduct.PostSharpUltimate, LicenseType.Community, endSubscription: false );

    public string PostSharpFramework => this.GenerateLicenseKey( 2, LicenseProduct.PostSharpFramework );

    public string PostSharpUltimate => this.GenerateLicenseKey( 3, LicenseProduct.PostSharpUltimate );

    public string PostSharpUltimateWithLongTermSupport => this.GenerateLicenseKey( 3, LicenseProduct.PostSharpUltimate, servicing: ServicingPhase.LongTerm );

    public const string PostSharpUltimateOpenSourceRedistributionNamespace = "Oss";

    public string MetalamaProfessionalPersonal => this.GenerateLicenseKey( 4, LicenseProduct.MetalamaProfessional, LicenseType.Personal );

    public string MetalamaProfessionalBusiness => this.GenerateLicenseKey( 5, LicenseProduct.MetalamaProfessional );

    public string MetalamaProfessionalBusinessNoGeneration
        => this.GenerateLicenseKey( 5, LicenseProduct.MetalamaProfessional, generation: LicenseGeneration.None );

    public string MetalamaProfessionalBusinessUnsigned => this.GenerateLicenseKey( 5, LicenseProduct.MetalamaProfessional, sign: false );

    public string MetalamaEnterprise => this.GenerateLicenseKey( 5, LicenseProduct.MetalamaEnterprise, servicing: ServicingPhase.LongTerm );

#pragma warning disable CA1822
    public string InvalidLicenseKey => "001-invalid";
#pragma warning restore CA1822

    public string MetalamaCommunity => this.GenerateLicenseKey( 6, LicenseProduct.MetalamaCommunity, LicenseType.Community, endSubscription: false );

    [Obsolete]
    public string MetalamaUltimatePersonal => this.GenerateLicenseKey( 7, LicenseProduct.MetalamaUltimate, LicenseType.Personal, LicenseGeneration.None );

    [Obsolete]
    public string MetalamaUltimateBusiness => this.GenerateLicenseKey( 8, LicenseProduct.MetalamaUltimate, generation: LicenseGeneration.None );

    public string MetalamaProfessionalBusinessNotAuditable
        => this.GenerateLicenseKey(
            9,
            key =>
            {
                key.Product = LicenseProduct.MetalamaProfessional;
                key.LicenseType = LicenseType.Business;
                key.Auditable = false;
                key.Generation = LicenseGeneration.Current;
            } );

    public string NotYetValid
        => this.GenerateLicenseKey(
            10,
            builder =>
            {
                builder.Product = LicenseProduct.MetalamaProfessional;
                builder.LicenseType = LicenseType.Evaluation;
                builder.ValidFrom = new DateTime( 2060, 1, 1 );
                builder.ValidTo = new DateTime( 2061, 1, 1 );
                builder.Generation = LicenseGeneration.Current;
            } );

    public string NoLongerValid
        => this.GenerateLicenseKey(
            11,
            builder =>
            {
                builder.Product = LicenseProduct.MetalamaProfessional;
                builder.LicenseType = LicenseType.Evaluation;
                builder.ValidFrom = new DateTime( 2010, 1, 1 );
                builder.ValidTo = new DateTime( 2011, 1, 1 );
                builder.Generation = LicenseGeneration.Current;
            } );

    public string ExpiredSubscription
        => this.GenerateLicenseKey(
            12,
            builder =>
            {
                builder.Product = LicenseProduct.MetalamaProfessional;
                builder.LicenseType = LicenseType.Business;
                builder.SubscriptionEndDate = this.ExpiredSubscriptionEndDate;
                builder.Generation = LicenseGeneration.Current;
            } );

    public string ExpiredSubscriptionLegacyGeneration
        => this.GenerateLicenseKey(
            13,
            builder =>
            {
                builder.Product = LicenseProduct.MetalamaProfessional;
                builder.LicenseType = LicenseType.Business;
                builder.SubscriptionEndDate = this.ExpiredSubscriptionEndDate;
            } );

    public string MetalamaProfessionalEvaluationNamespaceConstrained
        => this.GenerateLicenseKey(
            9,
            key =>
            {
                key.Product = LicenseProduct.MetalamaProfessional;
                key.LicenseType = LicenseType.Evaluation;
                key.Auditable = false;
                key.Generation = LicenseGeneration.Current;
                key.Namespace = NamespaceConstraint;
            } );

    [Obsolete]
    public string MetalamaStarter => this.GenerateLicenseKey( 12, LicenseProduct.MetalamaStarter, generation: LicenseGeneration.None );

    [Obsolete]
    public string MetalamaFree => this.GenerateLicenseKey( 13, LicenseProduct.MetalamaFree, generation: LicenseGeneration.None );

    public DateTime ExpiredSubscriptionEndDate { get; } = new( 2025, 1, 1, 0, 0, 0, DateTimeKind.Utc );

    public DateTime DefaultSubscriptionExpirationDate { get; } = new( 2050, 1, 1, 0, 0, 0, DateTimeKind.Utc );

    public string GetLicenseKey( string licenseKeyName )
    {
        var propertyInfo = this.GetType().GetProperty( licenseKeyName )
                           ?? throw new ArgumentOutOfRangeException();

        return (string) propertyInfo.GetValue( this, null )!;
    }
}