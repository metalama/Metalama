// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Testing;
using System;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing
{
    public abstract class LicensingTestsBase : TestsBase
    {
        protected override void OnAfterServicesCreated( Services services )
        {
            base.OnAfterServicesCreated( services );
            this.UserDeviceDetection.IsInteractiveDevice = true;
        }

        protected static TestLicenseKeyProvider LicenseKeyProvider { get; } = new();

        private protected LicensingTestsBase( ITestOutputHelper logger, bool isTelemetryEnabled = false ) : base(
            logger,
            new TestApplicationInfo(
                "Licensing Test App",
                false,
                "1.0",
                new DateTime( 2021, 1, 1, 0, 0, 0, DateTimeKind.Utc ) ) { IsTelemetryEnabled = isTelemetryEnabled } ) { }
    }
}