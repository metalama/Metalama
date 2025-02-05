// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable StringLiteralTypo

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

#pragma warning disable SA1203

namespace Metalama.Backstage.Testing;

[PublicAPI]
public sealed class TestLicenseKeyProvider
{
    private readonly Dictionary<string, string>? _testLicenseKeys;

    public TestLicenseKeyProvider( Assembly assembly )
    {
        var directory = AssemblyMetadataReader.GetInstance( assembly )["EngineeringDataDirectory"];
        
        // This is the same file name as in PostSharp.Engineering.BuildTools.Utilities.TestLicensesCache.FetchOnPrepareCompleted method,
        // and has to be changed when the content should change.
        const string testLicenseKeysCacheFileName = "TestLicenseKeys1.g.props";

        var dummyApplicationInfo = new TestApplicationInfo( "DummyForTestLicenseKeys", false, "1.0", DateTime.UtcNow );
        var servicesOptions = new BackstageInitializationOptions( dummyApplicationInfo );
        var serviceProviderBuilder = new SimpleServiceProviderBuilder();
        serviceProviderBuilder.AddBackstageServices( servicesOptions );
        var testLicenseKeysCacheFile = Path.Combine( directory, testLicenseKeysCacheFileName );

        if ( File.Exists( testLicenseKeysCacheFile ) )
        {
            var root = XElement.Load( testLicenseKeysCacheFile );

            this._testLicenseKeys = root.XPathSelectElements( "//PropertyGroup/*" )
                .Select( e => (e.Name.LocalName, e.Value) )
                .Where( p => p.LocalName.EndsWith( "LicenseKey", StringComparison.OrdinalIgnoreCase ) )
                .ToDictionary( p => p.LocalName[..^"LicenseKey".Length], p => p.Value );
        }
    }

    public string GetLicenseKey( string keyName )
        => this._testLicenseKeys?.TryGetValue( keyName, out var key ) ?? false
            ? key
            : throw new InvalidOperationException(
                $"Uknown test license key: '{keyName}'. Test license keys may not have been restored by PostSharp.Engineering. Licensing tests are only available on PostSharp-owned devices and will always fail otherwise." );

    public string PostSharpEssentials => this.GetLicenseKey( nameof(this.PostSharpEssentials) );

    public string PostSharpFramework => this.GetLicenseKey( nameof(this.PostSharpFramework) );

    public string PostSharpUltimate => this.GetLicenseKey( nameof(this.PostSharpUltimate) );

    public string PostSharpEnterprise => this.GetLicenseKey( nameof(this.PostSharpEnterprise) );

    public const string PostSharpUltimateOpenSourceRedistributionNamespace = "Oss";

    public string PostSharpUltimateOpenSourceRedistribution => this.GetLicenseKey( nameof(this.PostSharpUltimateOpenSourceRedistribution) );

    public string MetalamaFreePersonal => this.GetLicenseKey( nameof(this.MetalamaFreePersonal) );

    public string MetalamaFreeBusiness => this.GetLicenseKey( nameof(this.MetalamaFreeBusiness) );

    public string MetalamaStarterPersonal => this.GetLicenseKey( nameof(this.MetalamaStarterPersonal) );

    public string MetalamaStarterBusiness => this.GetLicenseKey( nameof(this.MetalamaStarterBusiness) );

    public string MetalamaProfessionalPersonal => this.GetLicenseKey( nameof(this.MetalamaProfessionalPersonal) );

    public string MetalamaProfessionalBusiness => this.GetLicenseKey( nameof(this.MetalamaProfessionalBusiness) );

    public string MetalamaUltimatePersonal => this.GetLicenseKey( nameof(this.MetalamaUltimatePersonal) );

    public string MetalamaUltimateBusiness => this.GetLicenseKey( nameof(this.MetalamaUltimateBusiness) );

    public string MetalamaUltimateBusinessNotAuditable => this.GetLicenseKey( nameof(this.MetalamaUltimateBusinessNotAuditable) );

    public const string MetalamaUltimateRedistributionNamespace = "RedistributionTests.TargetNamespace";

    public string MetalamaUltimateOpenSourceRedistribution => this.GetLicenseKey( nameof(this.MetalamaUltimateOpenSourceRedistribution) );

    public string MetalamaUltimateCommercialRedistribution => this.GetLicenseKey( nameof(this.MetalamaUltimateCommercialRedistribution) );

    public const string MetalamaUltimateProjectBoundProjectName = "ProjectBoundTestsProject";

    public string MetalamaUltimatePersonalProjectBound => this.GetLicenseKey( nameof(this.MetalamaUltimatePersonalProjectBound) );

    // This is to be used in Metalama Integration tests, where the test dependency has a random assembly name.
    public const string MetalamaUltimateOpenSourceRedistributionForIntegrationTestsNamespace = "dependency_XXXXXXXXXXXXXXXX";

    public string MetalamaUltimateOpenSourceRedistributionForIntegrationTests
        => this.GetLicenseKey( nameof(this.MetalamaUltimateOpenSourceRedistributionForIntegrationTests) );

    public DateTime SubscriptionExpirationDate { get; } = new( 2050, 1, 1, 0, 0, 0, DateTimeKind.Utc );
}