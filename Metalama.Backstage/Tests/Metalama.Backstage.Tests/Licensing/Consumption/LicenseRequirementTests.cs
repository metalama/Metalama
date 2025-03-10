// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseRequirementTests : LicenseConsumptionServiceTestsBase
{
    public LicenseRequirementTests( ITestOutputHelper logger ) : base( logger ) { }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework), true )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), true )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal), true )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForExtensionWithEligibleBuild( string licenseKeyName, bool expectedResult )
    {
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<ComponentName>" ) ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), false )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), false )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal), false )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForExtensionWithIneligibleBuild( string licenseKeyName, bool expectedResult )
    {
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<ComponentName>" ) ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework), true )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), true )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal), true )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForToolingWithEligibleBuild( string licenseKeyName, bool expectedResult )
    {
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( MetalamaToolingLicenseRequirement.Instance ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), false )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), false )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal), false )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForToolingWithIneligibleBuild( string licenseKeyName, bool expectedResult )
    {
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( MetalamaToolingLicenseRequirement.Instance ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), false )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpFramework), false )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimate), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessUnsigned), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNotAuditable), false )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaFree), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaStarter), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimatePersonal), false )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForToolingWithEligibleBuildAfterSubscriptionExpires( string licenseKeyName, bool expectedResult )
    {
        this.Time.Set( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 45 ) );
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( MetalamaToolingLicenseRequirement.Instance ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), false )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), true )]
    public void LicenseQualifiesForToolingWithEligibleBuildAfterSubscriptionExpires2( string licenseKeyName, bool expectedResult )
    {
        this.Time.Set( LicenseKeyProvider.ExpiredSubscriptionEndDate.AddDays( 45 ) );
        this.SetBuildDate( LicenseKeyProvider.ExpiredSubscriptionEndDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( MetalamaToolingLicenseRequirement.Instance ) );
    }
}