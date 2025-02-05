// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Evaluation
{
    public abstract class EvaluationLicenseRegistrationTestsBase : LicensingTestsBase
    {
        protected static readonly DateTime TestStart = new( 2020, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        private protected EvaluationLicenseRegistrationTestsBase( ITestOutputHelper logger ) : base( logger ) { }

        protected void AssertEvaluationEligible()
        {
            Assert.True( this.LicenseRegistrationService.TryRegisterTrialEdition( out _ ) );
            var expectedStart = this.Time.UtcNow.Date;
            var expectedEnd = expectedStart + LicensingConstants.EvaluationPeriod;

            var licenses = LicensingConfigurationModel.Create( this.ServiceProvider );

            Assert.NotNull( licenses.LicenseProperties );
            Assert.Equal( LicenseType.Evaluation, licenses.LicenseProperties!.LicenseType );
            Assert.Equal( expectedStart, licenses.LicenseProperties!.ValidFrom!.Value.Date );
            Assert.Equal( expectedEnd, licenses.LicenseProperties!.ValidTo!.Value.Date );
            Assert.Equal( expectedEnd, licenses.LicenseProperties!.SubscriptionEndDate );
        }

        protected void AssertEvaluationNotEligible( string reason )
        {
            Assert.False( this.LicenseRegistrationService.TryRegisterTrialEdition( out _ ), reason );
        }
    }
}