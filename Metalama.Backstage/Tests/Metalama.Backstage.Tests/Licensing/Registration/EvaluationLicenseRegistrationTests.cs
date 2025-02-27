// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Registration
{
    public sealed class EvaluationLicenseRegistrationTests : LicensingTestsBase
    {
        public EvaluationLicenseRegistrationTests( ITestOutputHelper logger )
            : base( logger ) { }

        private static readonly DateTime _testStart = new( 2020, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        private void AssertEvaluationEligible()
        {
            Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );
            var expectedStart = this.Time.UtcNow.Date;
            var expectedEnd = expectedStart + LicensingConstants.EvaluationPeriod;

            var licenseProperties = this.LicenseRegistrationService.RegisteredLicenses.Single();
            Assert.NotNull( licenseProperties );
            Assert.Equal( LicenseType.Evaluation, licenseProperties.LicenseType );
            Assert.Equal( expectedStart, licenseProperties.ValidFrom!.Value.Date );
            Assert.Equal( expectedEnd, licenseProperties.ValidTo!.Value.Date );
            Assert.Equal( expectedEnd, licenseProperties.SubscriptionEndDate );
        }

        private void AssertEvaluationNotEligible( string reason )
        {
            Assert.False( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess, reason );
        }

        [Fact]
        public void EvaluationLicenseRegistersInCleanEnvironment()
        {
            this.Time.Set( _testStart, true );
            this.AssertEvaluationEligible();
        }

        private void TestRepetitiveRegistration( TimeSpan retryAfter, bool unregisterBeforeRetry, bool expectedEligibility )
        {
            this.Time.Set( _testStart, true );
            this.AssertEvaluationEligible();

            if ( unregisterBeforeRetry )
            {
                this.LicenseRegistrationService.RemoveLicenses();
            }

            var license = this.LicenseRegistrationService.RegisteredLicenses;

            if ( unregisterBeforeRetry )
            {
                Assert.Empty( license );
            }
            else
            {
                Assert.Single( license );
            }

            this.Time.Set( this.Time.UtcNow + retryAfter, true );

            if ( expectedEligibility )
            {
                this.AssertEvaluationEligible();
            }
            else
            {
                this.AssertEvaluationNotEligible( "Evaluation license requested recently." );
            }
        }

        [Fact]
        public void RepetitiveEvaluationLicenseRegistrationFails()
        {
            this.TestRepetitiveRegistration( TimeSpan.Zero, false, false );
        }

        [Fact]
        public void ImmediateRepetitiveEvaluationLicenseRegistrationFailsAfterUnregistration()
        {
            this.TestRepetitiveRegistration( TimeSpan.Zero, true, false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationWithinRunningEvaluationFails()
        {
            this.TestRepetitiveRegistration( new TimeSpan( LicensingConstants.EvaluationPeriod.Ticks / 2 ), false, false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationWithinRunningEvaluationFailsAfterUnregistration()
        {
            this.TestRepetitiveRegistration( new TimeSpan( LicensingConstants.EvaluationPeriod.Ticks / 2 ), true, false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationWithinNoEvaluationPeriodFails()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + new TimeSpan( LicensingConstants.NoEvaluationPeriod.Ticks / 2 ),
                false,
                false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationWithinNoEvaluationPeriodFailsAfterUnregistration()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + new TimeSpan( LicensingConstants.NoEvaluationPeriod.Ticks / 2 ),
                true,
                false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationAtTheEndOfNoEvaluationPeriodFails()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + LicensingConstants.NoEvaluationPeriod.Subtract( TimeSpan.FromMinutes( 1 ) ),
                false,
                false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationAtTheEndOfNoEvaluationPeriodFailsAfterUnregistration()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + LicensingConstants.NoEvaluationPeriod.Subtract( TimeSpan.FromMinutes( 1 ) ),
                true,
                false );
        }

        [Fact]
        public void EvaluationLicenseRegistrationAfterNoEvaluationPeriodSucceeds()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + LicensingConstants.NoEvaluationPeriod +
                TimeSpan.FromDays( 1 ),
                false,
                true );
        }

        [Fact]
        public void EvaluationLicenseRegistrationAfterNoEvaluationPeriodSucceedsAfterUnregistration()
        {
            this.TestRepetitiveRegistration(
                LicensingConstants.EvaluationPeriod + LicensingConstants.NoEvaluationPeriod +
                TimeSpan.FromDays( 1 ),
                true,
                true );
        }

        [Fact]
        public async Task NotifyPropertyChanged()
        {
            Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

            var gotPropertyChanged = new TaskCompletionSource<bool>();
            this.LicenseRegistrationService.PropertyChanged += ( _, _ ) => gotPropertyChanged.TrySetResult( true );

            Assert.True( this.LicenseRegistrationService.RegisterCommunityEdition( CommunityLicenseReason.Individual ).IsSuccess );

            Assert.Equal( gotPropertyChanged.Task, await Task.WhenAny( gotPropertyChanged.Task, Task.Delay( 30000 ) ) );
        }
    }
}