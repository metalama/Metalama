// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

// Covers the combinations of the repository's metalama.json and the user's (global) telemetry.json, as resolved by
// ITelemetryService.OpenContext. The critical guarantee is that a repository-wide opt-out performs and activates no
// telemetry whatsoever. See #1701.
public sealed class TelemetryContextTests : TestsBase
{
    private const string _repoRoot = @"C:\repo";
    private const string _projectDirectory = @"C:\repo\src\project";

    public TelemetryContextTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo { IsTelemetryEnabled = true } ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services ) => services.AddTelemetryServices();

    private ITelemetryService TelemetryService => this.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

    // The device identifier is created only when telemetry is activated; it stays null while telemetry is dormant.
    private Guid? DeviceId => this.ConfigurationManager!.Get<TelemetryConfiguration>().DeviceId;

    private void CreateRepository( bool? telemetryEnabled )
    {
        this.FileSystem.CreateDirectory( Path.Combine( _repoRoot, ".git" ) );

        if ( telemetryEnabled != null )
        {
            this.FileSystem.WriteAllText(
                Path.Combine( _repoRoot, "metalama.json" ),
                $$"""{ "telemetry": { "enabled": {{(telemetryEnabled.Value ? "true" : "false")}} } }""" );
        }
    }

    [Fact]
    public void NoMetalamaJsonAndGlobalDefault_UsageEnabled()
    {
        this.CreateRepository( telemetryEnabled: null );

        Assert.NotEqual(
            TelemetryConsent.No,
            this.TelemetryService.GetPolicy( _projectDirectory ).GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void RepositoryOptInAndGlobalDefault_UsageEnabled()
    {
        this.CreateRepository( telemetryEnabled: true );

        Assert.NotEqual(
            TelemetryConsent.No,
            this.TelemetryService.GetPolicy( _projectDirectory ).GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void RepositoryOptOut_AllScenariosDisabled()
    {
        this.CreateRepository( telemetryEnabled: false );

        var policy = this.TelemetryService.GetPolicy( _projectDirectory );

        Assert.Equal( TelemetryConsent.No, policy.GetConsent( TelemetryScenario.Usage ) );
        Assert.Equal( TelemetryConsent.No, policy.GetConsent( TelemetryScenario.Exception ) );
        Assert.Equal( TelemetryConsent.No, policy.GetConsent( TelemetryScenario.Performance ) );
    }

    [Fact]
    public void RepositoryOptOut_PerformsNoTelemetryAndNeverActivates()
    {
        // This is the critical guarantee: a repository-wide opt-out collects/sends no telemetry and never even
        // activates telemetry (no device identifier is ever written), regardless of the global telemetry.json.
        this.CreateRepository( telemetryEnabled: false );

        var context = this.TelemetryService.OpenContext( this.TelemetryService.GetPolicy( _projectDirectory ) );

        using ( var session = context.StartUsageSession( "TestUsage", "Project1" ) )
        {
            Assert.False( session.ShouldCollectMetrics );
        }

        context.ReportException( new InvalidOperationException( "test" ) );

        Assert.Null( this.DeviceId );
    }

    [Fact]
    public void NonOptedOutRepository_UsageSessionActivatesTelemetry()
    {
        // The contrast to the opt-out case: a non-opted-out usage session does activate telemetry (lazily).
        this.CreateRepository( telemetryEnabled: null );

        Assert.Null( this.DeviceId );

        var context = this.TelemetryService.OpenContext( this.TelemetryService.GetPolicy( _projectDirectory ) );

        using ( var session = context.StartUsageSession( "TestUsage", "Project1" ) )
        {
            Assert.True( session.ShouldCollectMetrics );
        }

        Assert.NotNull( this.DeviceId );
    }

    [Fact]
    public void RepositoryOptIn_DoesNotOverrideEnvironmentVariableOptOut()
    {
        // The environment variable keeps absolute priority: an explicit repository opt-in cannot re-enable telemetry.
        this.EnvironmentVariableProvider.Environment[Backstage.Telemetry.TelemetryConfigurationService.OptOutEnvironmentVariable] = "1";
        this.CreateRepository( telemetryEnabled: true );

        Assert.Equal(
            TelemetryConsent.No,
            this.TelemetryService.GetPolicy( _projectDirectory ).GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void InProductOptOut_DisablesUsageWithoutMetalamaJson()
    {
        // `metalama telemetry disable` (SetStatus(false)) disables usage even when there is no metalama.json.
        this.CreateRepository( telemetryEnabled: null );
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.No );

        Assert.Equal(
            TelemetryConsent.No,
            this.TelemetryService.GetPolicy( _projectDirectory ).GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void ToolingPolicy_NoWorkingDirectory_Disabled()
    {
        // With no working directory, there is no repository context, so the tooling policy disables telemetry.
        var policy = this.TelemetryService.GetToolingPolicy();

        Assert.Equal( TelemetryConsent.No, policy.GetConsent( TelemetryScenario.Usage ) );
        Assert.Equal( TelemetryConsent.No, policy.GetConsent( TelemetryScenario.Exception ) );
    }

    [Fact]
    public void ToolingPolicy_WorkingDirectoryOutsideRepository_Disabled()
    {
        // The working directory exists but is not inside a git repository (no .git found while walking up): the tooling
        // policy disables telemetry.
        this.EnvironmentVariableProvider.CurrentDirectory = _projectDirectory;

        Assert.Equal(
            TelemetryConsent.No,
            this.TelemetryService.GetToolingPolicy().GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void ToolingPolicy_WorkingDirectoryInsideRepository_HonorsRepositoryPolicy()
    {
        // When the working directory is inside a git repository, the tooling policy honors that repository's policy: a
        // non-opted-out repository enables usage.
        this.CreateRepository( telemetryEnabled: null );
        this.EnvironmentVariableProvider.CurrentDirectory = _projectDirectory;

        Assert.NotEqual(
            TelemetryConsent.No,
            this.TelemetryService.GetToolingPolicy().GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void ToolingPolicy_WorkingDirectoryInsideOptedOutRepository_Disabled()
    {
        // The repository opt-out (metalama.json) is honored through the tooling policy too: inside an opted-out
        // repository, telemetry is disabled even though the working directory is in a git repository.
        this.CreateRepository( telemetryEnabled: false );
        this.EnvironmentVariableProvider.CurrentDirectory = _projectDirectory;

        Assert.Equal(
            TelemetryConsent.No,
            this.TelemetryService.GetToolingPolicy().GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void AdHocPolicy_No_DisablesOtherwiseEnabledContext()
    {
        // A caller-supplied policy fully replaces the default: returning No disables a context that the default
        // (no metalama.json) would have enabled.
        this.CreateRepository( telemetryEnabled: null );

        var context = this.TelemetryService.OpenContext( new FixedPolicy( TelemetryConsent.No ) );

        Assert.Equal( TelemetryConsent.No, context.Policy.GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void AdHocPolicy_Yes_EnablesEvenWhenRepositoryOptedOut()
    {
        // Pure replace: the ad-hoc policy fully substitutes the default, so the metalama.json opt-out is NOT consulted.
        this.CreateRepository( telemetryEnabled: false );

        var context = this.TelemetryService.OpenContext( new FixedPolicy( TelemetryConsent.Yes ) );

        Assert.NotEqual( TelemetryConsent.No, context.Policy.GetConsent( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void ComposedPolicy_HonorsMetalamaJsonOptOut()
    {
        // The intended VSX pattern: wrap the default policy with host consent so metalama.json is still honored even
        // though the ad-hoc policy replaces the default.
        this.CreateRepository( telemetryEnabled: false );

        var composed = new ComposedPolicy( hostConsent: true, this.TelemetryService.GetPolicy( _projectDirectory ) );
        var context = this.TelemetryService.OpenContext( composed );

        Assert.Equal( TelemetryConsent.No, context.Policy.GetConsent( TelemetryScenario.Usage ) );
    }

    // ---- Reason propagation: ITelemetryPolicy.GetConsentAndReason resolves WHY telemetry is on/off and the reason must
    // propagate from the metalama.json resolution (TelemetryService) and the process/per-category gates
    // (ITelemetryConfigurationService) through the policy. See #1701. ----

    [Fact]
    public void Reason_EnabledRepository_IsNone()
    {
        this.CreateRepository( telemetryEnabled: null );

        var (consent, reason) = this.TelemetryService.GetPolicy( _projectDirectory ).GetConsentAndReason( TelemetryScenario.Usage );

        Assert.NotEqual( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.None, reason );
    }

    [Fact]
    public void Reason_RepositoryOptOut_PropagatesToEveryScenario()
    {
        this.CreateRepository( telemetryEnabled: false );

        var policy = this.TelemetryService.GetPolicy( _projectDirectory );

        // The metalama.json opt-out is a repository-wide veto, so the reason is the same for every scenario.
        foreach ( var scenario in new[] { TelemetryScenario.Usage, TelemetryScenario.Exception, TelemetryScenario.Performance } )
        {
            var (consent, reason) = policy.GetConsentAndReason( scenario );

            Assert.Equal( TelemetryConsent.No, consent );
            Assert.Equal( TelemetryDisabledReason.RepositoryOptOut, reason );
        }
    }

    [Fact]
    public void Reason_EnvironmentVariableOptOut()
    {
        // The repository does not opt out, so the reason comes from the env-var gate resolved by the configuration service.
        this.EnvironmentVariableProvider.Environment[Backstage.Telemetry.TelemetryConfigurationService.OptOutEnvironmentVariable] = "1";
        this.CreateRepository( telemetryEnabled: true );

        var (consent, reason) = this.TelemetryService.GetPolicy( _projectDirectory ).GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.EnvironmentVariableOptOut, reason );
    }

    [Fact]
    public void Reason_UnsupportedApplication()
    {
        // Must be set before the service provider (and thus the telemetry configuration service) is created.
        this.ApplicationInfo = new TestApplicationInfo { IsTelemetryEnabled = false };
        this.CreateRepository( telemetryEnabled: null );

        var (consent, reason) = this.TelemetryService.GetPolicy( _projectDirectory ).GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.UnsupportedApplication, reason );
    }

    [Fact]
    public void Reason_UnattendedProcess()
    {
        this.ApplicationInfo = new TestApplicationInfo { IsTelemetryEnabled = true, IsUnattendedProcess = true };
        this.CreateRepository( telemetryEnabled: null );

        var (consent, reason) = this.TelemetryService.GetPolicy( _projectDirectory ).GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.UnattendedProcess, reason );
    }

    [Fact]
    public void Reason_UserOptOut_IsPerScenario()
    {
        // The per-category telemetry.json opt-out is independent per scenario, so the reason must be too: a disabled
        // category reports UserOptOut while an enabled one reports None.
        this.CreateRepository( telemetryEnabled: null );
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Usage, TelemetryConsent.No );
        this.TelemetryConfigurationService.SetConsent( TelemetryScenario.Exception, TelemetryConsent.Yes );

        var policy = this.TelemetryService.GetPolicy( _projectDirectory );

        var usage = policy.GetConsentAndReason( TelemetryScenario.Usage );
        Assert.Equal( TelemetryConsent.No, usage.Consent );
        Assert.Equal( TelemetryDisabledReason.UserOptOut, usage.Reason );

        var exception = policy.GetConsentAndReason( TelemetryScenario.Exception );
        Assert.NotEqual( TelemetryConsent.No, exception.Consent );
        Assert.Equal( TelemetryDisabledReason.None, exception.Reason );
    }

    [Fact]
    public void Reason_NullPolicy_IsNoRepositoryContext()
    {
        var (consent, reason) = NullTelemetryPolicy.NoContext.GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.NoRepositoryContext, reason );
    }

    [Fact]
    public void Reason_GetPolicyWithoutDirectory_IsNoRepositoryContext()
    {
        var (consent, reason) = this.TelemetryService.GetPolicy( null ).GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.NoRepositoryContext, reason );
    }

    [Fact]
    public void Reason_ToolingPolicyOutsideRepository_IsNoRepositoryContext()
    {
        // The working directory is not inside a git repository, so the tooling policy has no repository context.
        this.EnvironmentVariableProvider.CurrentDirectory = _projectDirectory;

        var (consent, reason) = this.TelemetryService.GetToolingPolicy().GetConsentAndReason( TelemetryScenario.Usage );

        Assert.Equal( TelemetryConsent.No, consent );
        Assert.Equal( TelemetryDisabledReason.NoRepositoryContext, reason );
    }

    // A policy that returns a fixed action for every scenario, used to prove that a caller-supplied policy replaces the
    // default outright.
    private sealed class FixedPolicy : ITelemetryPolicy
    {
        private readonly TelemetryConsent _consent;

        public FixedPolicy( TelemetryConsent consent )
        {
            this._consent = consent;
        }

        public bool HasRepositoryContext => throw new NotImplementedException();

        public TelemetryConsent GetConsent( TelemetryScenario scenario ) => this._consent;

        public (TelemetryConsent Consent, TelemetryDisabledReason Reason) GetConsentAndReason( TelemetryScenario scenario )
            => throw new NotImplementedException();

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;
    }

    // Wraps an inner policy with a host consent flag (the VSX pattern). When consent is granted it delegates to the
    // inner default policy — which still honors metalama.json — and forwards its warnings.
    private sealed class ComposedPolicy : ITelemetryPolicy
    {
        private readonly bool _hostConsent;
        private readonly ITelemetryPolicy _inner;

        public ComposedPolicy( bool hostConsent, ITelemetryPolicy inner )
        {
            this._hostConsent = hostConsent;
            this._inner = inner;
        }

        public bool HasRepositoryContext => throw new NotImplementedException();

        public TelemetryConsent GetConsent( TelemetryScenario scenario ) => this._hostConsent ? this._inner.GetConsent( scenario ) : TelemetryConsent.No;

        public (TelemetryConsent Consent, TelemetryDisabledReason Reason) GetConsentAndReason( TelemetryScenario scenario )
            => throw new NotImplementedException();

        public ImmutableArray<TelemetryContextWarning> Warnings => this._inner.Warnings;
    }
}