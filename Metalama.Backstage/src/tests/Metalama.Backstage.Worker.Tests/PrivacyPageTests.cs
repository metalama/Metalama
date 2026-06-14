// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Pages;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Worker.Tests;

public sealed class PrivacyPageTests : TestsBase
{
    public PrivacyPageTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnGetReflectsCurrentTelemetryStatus( bool enabled )
    {
        this.TelemetryConfigurationService.SetStatus( enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService );
        model.OnGet();

        Assert.Equal( enabled, model.IsTelemetryEnabled );
    }

    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void OnPostUpdatesTelemetryStatus( bool enabled )
    {
        // Start from the opposite state to make sure the post actually changes it.
        this.TelemetryConfigurationService.SetStatus( !enabled );

        var model = new PrivacyPageModel( this.TelemetryConfigurationService ) { IsTelemetryEnabled = enabled };
        model.OnPost();

        Assert.Equal( enabled, this.TelemetryConfigurationService.IsEnabled( TelemetryScenario.Usage ) );
        Assert.True( model.IsSaved );
    }
}
