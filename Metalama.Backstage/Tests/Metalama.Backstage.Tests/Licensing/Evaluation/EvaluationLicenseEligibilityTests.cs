// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Evaluation
{
    public class EvaluationLicenseEligibilityTests : EvaluationLicenseRegistrationTestsBase
    {
        public EvaluationLicenseEligibilityTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public void EvaluationLicenseRegistersInCleanEnvironment()
        {
            this.Time.Set( TestStart, true );
            this.AssertEvaluationEligible();
        }

        private void TestRepetitiveRegistration( TimeSpan retryAfter, bool unregisterBeforeRetry, bool expectedEligibility )
        {
            this.Time.Set( TestStart, true );
            this.AssertEvaluationEligible();

            if ( unregisterBeforeRetry )
            {
                LicensingConfigurationModel.Create( this.ServiceProvider ).RemoveLicense();
            }

            var license = new UserProfileLicenseSource( this.ServiceProvider ).GetLicense(
                m => Assert.Fail( $"Unexpected message from license provider: '{m}'" ) );

            if ( unregisterBeforeRetry )
            {
                Assert.Null( license );
            }
            else
            {
                Assert.NotNull( license );
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
        public void ImmediateRepetitiveEvaluationLicenseRegistrationSucceeds()
        {
            // This is allowed to avoid race conditions first time user experience, where the evaluation license is registered automatically.
            this.TestRepetitiveRegistration( TimeSpan.Zero, false, true );
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
            Assert.True( this.LicenseRegistrationService.TryRegisterTrialEdition( out _ ) );

            var gotPropertyChanged = new TaskCompletionSource<bool>();
            this.LicenseRegistrationService.PropertyChanged += ( _, _ ) => gotPropertyChanged.TrySetResult( true );

            Assert.True( this.LicenseRegistrationService.TryRegisterFreeEdition( out _ ) );

            Assert.Equal( gotPropertyChanged.Task, await Task.WhenAny( gotPropertyChanged.Task, Task.Delay( 30000 ) ) );
        }
    }
}