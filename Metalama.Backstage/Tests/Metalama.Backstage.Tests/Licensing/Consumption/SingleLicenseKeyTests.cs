// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public class SingleLicenseKeyTests : LicenseConsumptionManagerTestsBase
{
    public SingleLicenseKeyTests( ITestOutputHelper logger )
        : base( logger ) { }

    private void TestOneLicense( string licenseKeyName, LicenseRequirementTestEnum requestedRequirement, bool expectedCanConsume )
        => this.TestOneLicense( licenseKeyName, requestedRequirement, "Foo", expectedCanConsume );

    private void TestOneLicense(
        string licenseKeyName,
        LicenseRequirementTestEnum requestedRequirement,
        string requiredNamespace,
        bool expectedCanConsume )
    {
        var license = this.CreateLicense( licenseKeyName );
        var manager = this.CreateConsumptionService( license );
        TestConsumption( manager, requestedRequirement, requiredNamespace, expectedCanConsume );
    }

    private void TestOneLicenseSource( ILicenseSource licenseSource, LicenseRequirementTestEnum requestedRequirement, bool expectedCanConsume )
        => this.TestOneLicenseSource( licenseSource, requestedRequirement, "Foo", expectedCanConsume );

    private void TestOneLicenseSource(
        ILicenseSource licenseSource,
        LicenseRequirementTestEnum requestedRequirement,
        string requiredNamespace,
        bool expectedCanConsume )
    {
        var manager = this.CreateConsumptionManager( licenseSource );
        TestConsumption( manager, requestedRequirement, requiredNamespace, expectedCanConsume );
    }

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpFramework), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEnterprise), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterPersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimatePersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreePersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreeBusiness), true )]
    public void NamespaceUnlimitedLicenseAllowsMetalamaFreeFeatures( string licenseKeyName, bool expectedCanConsume )
        => this.TestOneLicense( LicenseKeyProvider.GetLicenseKey( licenseKeyName ), LicenseRequirementTestEnum.Free, expectedCanConsume );

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpFramework), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEnterprise), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterPersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimatePersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreePersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreeBusiness), false )]
    public void NamespaceUnlimitedLicenseAllowsMetalamaStarterFeatures( string licenseKeyName, bool expectedCanConsume )
        => this.TestOneLicense( LicenseKeyProvider.GetLicenseKey( licenseKeyName ), LicenseRequirementTestEnum.Starter, expectedCanConsume );

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpFramework), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEnterprise), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterPersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterBusiness), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalPersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimatePersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreePersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreeBusiness), false )]
    public void NamespaceUnlimitedLicenseAllowsMetalamaProfessionalFeatures( string licenseKeyName, bool expectedCanConsume )
        => this.TestOneLicense( LicenseKeyProvider.GetLicenseKey( licenseKeyName ), LicenseRequirementTestEnum.Professional, expectedCanConsume );

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEssentials), false )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpFramework), false )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpUltimate), true )]
    [InlineData( nameof(LicenseKeyProvider.PostSharpEnterprise), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterPersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaStarterBusiness), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalPersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaProfessionalBusiness), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimatePersonal), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateBusiness), true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreePersonal), false )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaFreeBusiness), false )]
    public void NamespaceUnlimitedLicenseAllowsMetalamaUltimateFeatures( string licenseKeyName, bool expectedCanConsume )
        => this.TestOneLicense( LicenseKeyProvider.GetLicenseKey( licenseKeyName ), LicenseRequirementTestEnum.Ultimate, expectedCanConsume );

    [Theory]
    [InlineData(
        nameof(LicenseKeyProvider.PostSharpUltimateOpenSourceRedistribution),
        TestLicenseKeyProvider.PostSharpUltimateOpenSourceRedistributionNamespace,
        true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateOpenSourceRedistribution), TestLicenseKeyProvider.MetalamaUltimateRedistributionNamespace, true )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateCommercialRedistribution), TestLicenseKeyProvider.MetalamaUltimateRedistributionNamespace, true )]
    public void NamespaceLimitedLicenseAllowsMetalamaInSameNamespace( string licenseKeyName, string requiredNamespace, bool expectedCanConsume )
        => this.TestOneLicense(
            LicenseKeyProvider.GetLicenseKey( licenseKeyName ),
            LicenseRequirementTestEnum.Ultimate,
            requiredNamespace,
            expectedCanConsume );

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.PostSharpUltimateOpenSourceRedistribution) )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateOpenSourceRedistribution) )]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimateCommercialRedistribution) )]
    public void RedistributionLicenseAllowsMetalamaInArbitraryNamespace( string licenseKeyName )
        => this.TestOneLicense(
            LicenseKeyProvider.GetLicenseKey( licenseKeyName ),
            LicenseRequirementTestEnum.Ultimate,
            "Foo",
            true );

    [Theory]
    [InlineData( nameof(LicenseKeyProvider.MetalamaUltimatePersonalProjectBound) )]
    public void ProjectBoundLicenseForbidsMetalamaInArbitraryNamespace( string licenseKeyName )
        => this.TestOneLicense(
            LicenseKeyProvider.GetLicenseKey( licenseKeyName ),
            LicenseRequirementTestEnum.Ultimate,
            "Foo",
            false );

    [Theory]
    [InlineData( LicenseRequirementTestEnum.Free, true )]
    [InlineData( LicenseRequirementTestEnum.Starter, false )]
    [InlineData( LicenseRequirementTestEnum.Professional, false )]
    [InlineData( LicenseRequirementTestEnum.Ultimate, false )]
    public void UnsignedMetalamaFreeLicenseAllowsRequirements( LicenseRequirementTestEnum requestedRequirement, bool expectedCanConsume )
        => this.TestOneLicense( TestLicenseFactory.CreateMetalamaFreeLicense( this.ServiceProvider ), requestedRequirement, expectedCanConsume );

    [Theory]
    [InlineData( LicenseRequirementTestEnum.Free, true )]
    [InlineData( LicenseRequirementTestEnum.Starter, true )]
    [InlineData( LicenseRequirementTestEnum.Professional, true )]
    [InlineData( LicenseRequirementTestEnum.Ultimate, true )]
    public void UnsignedMetalamaEvaluationLicenseAllowsRequirements( LicenseRequirementTestEnum requestedRequirement, bool expectedCanConsume )
        => this.TestOneLicense( TestLicenseFactory.CreateMetalamaEvaluationLicense( this.ServiceProvider ), requestedRequirement, expectedCanConsume );

    [Theory]
    [InlineData( LicenseRequirementTestEnum.Free, true )]
    [InlineData( LicenseRequirementTestEnum.Starter, true )]
    [InlineData( LicenseRequirementTestEnum.Professional, true )]
    [InlineData( LicenseRequirementTestEnum.Ultimate, true )]
    public void UnattendedLicenseAllowsRequirements( LicenseRequirementTestEnum requestedRequirement, bool expectedCanConsume )
        => this.TestOneLicenseSource( TestLicenseFactory.CreateUnattendedLicenseSource(), requestedRequirement, expectedCanConsume );
}