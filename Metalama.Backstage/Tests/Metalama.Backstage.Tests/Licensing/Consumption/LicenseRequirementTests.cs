// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Testing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), true )]
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
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), false )]
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
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), true )]
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
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaToolingLicenseRequirement() ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), false )]
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
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaToolingLicenseRequirement() ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaCommunity), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), true )]
    [InlineData( nameof(TestLicenseKeyProvider.InvalidLicenseKey), false )]
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
    public void LicenseQualifiesForToolingWithEligibleBuildAfterSubscriptionExpires( string licenseKeyName, bool expectedResult )
    {
        this.Time.Set( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 45 ) );
        this.SetBuildDate( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaToolingLicenseRequirement() ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscription), true )]
    [InlineData( nameof(TestLicenseKeyProvider.ExpiredSubscriptionLegacyGeneration), true )]
    public void LicenseQualifiesForToolingWithEligibleBuildAfterSubscriptionExpires2( string licenseKeyName, bool expectedResult )
    {
        this.Time.Set( LicenseKeyProvider.ExpiredSubscriptionEndDate.AddDays( 45 ) );
        this.SetBuildDate( LicenseKeyProvider.ExpiredSubscriptionEndDate.AddDays( -1 ) );

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaToolingLicenseRequirement() ) );
    }

    [Theory]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNoGeneration), ServicingPhase.Default, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNoGeneration), ServicingPhase.Extended, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusinessNoGeneration), ServicingPhase.LongTerm, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), ServicingPhase.Default, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), ServicingPhase.Extended, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness), ServicingPhase.LongTerm, false )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), ServicingPhase.Default, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), ServicingPhase.Extended, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaEnterprise), ServicingPhase.LongTerm, true )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimateWithLongTermSupport), ServicingPhase.Default, true )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimateWithLongTermSupport), ServicingPhase.Extended, true )]
    [InlineData( nameof(TestLicenseKeyProvider.PostSharpUltimateWithLongTermSupport), ServicingPhase.LongTerm, true )]
#pragma warning disable CS0612 // Type or member is obsolete
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), ServicingPhase.Default, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), ServicingPhase.Extended, true )]
    [InlineData( nameof(TestLicenseKeyProvider.MetalamaUltimateBusiness), ServicingPhase.LongTerm, true )]
#pragma warning restore CS0612 // Type or member is obsolete
    public void LicenseQualifiesForServicingPhase( string licenseKeyName, ServicingPhase servicingPhase, bool expectedResult )
    {
        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer();
        Assert.Equal( expectedResult, consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<Component>", servicingPhase ) ) );
    }

    [Theory]
    [InlineData(
        nameof(TestLicenseKeyProvider.MetalamaProfessionalBusiness),
        ServicingPhase.LongTerm,
        "Metalama Enterprise, PostSharp Framework with long-term support, PostSharp Ultimate with long-term support" )]
    [InlineData(
        nameof(TestLicenseKeyProvider.MetalamaCommunity),
        ServicingPhase.Extended,
        "Metalama Professional, Metalama Enterprise, PostSharp Framework, PostSharp Ultimate" )]
    [InlineData(
        nameof(TestLicenseKeyProvider.MetalamaCommunity),
        ServicingPhase.Default,
        "Metalama Professional, Metalama Enterprise, PostSharp Framework, PostSharp Ultimate, Metalama Starter (legacy), Metalama Ultimate (legacy)" )]
    public void ErrorMessageContainsExpectedProductList( string licenseKeyName, ServicingPhase servicingPhase, string expectedProductList )
    {
        var messages = new List<LicensingMessage>();

        var licenseKey = LicenseKeyProvider.GetLicenseKey( licenseKeyName );
        var license = this.CreateInstrumentedLicenseWrapper( licenseKey );
        var consumer = this.CreateConsumptionService( license ).CreateConsumer( reportMessage: messages.Add );
        Assert.False( consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<Component>", servicingPhase ), reportMessage: messages.Add ) );

        var match = Regex.Match( messages[0].Text, "It requires one of the following products\\: ([^\\.]*)\\." );

        Assert.True( match.Success );
        var productList = match.Groups[1].Value;
        this.Logger.WriteLine( productList );
        Assert.Equal( expectedProductList, productList.Trim() );
    }
}