// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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