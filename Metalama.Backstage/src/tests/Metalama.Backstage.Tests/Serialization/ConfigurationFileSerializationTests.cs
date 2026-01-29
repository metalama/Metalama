// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Telemetry.User;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Welcome;
using System;
using System.Collections.Immutable;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Serialization;

public sealed class ConfigurationFileSerializationTests : JsonSerializationTestsBase
{
    public ConfigurationFileSerializationTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void TelemetryConfiguration_Serialization()
    {
        var input = new TelemetryConfiguration
        {
            ExceptionReportingAction = ReportingAction.Yes,
            PerformanceProblemReportingAction = ReportingAction.No,
            UsageReportingAction = ReportingAction.Default,
            DeviceId = Guid.Parse( "a1b2c3d4-e5f6-7890-abcd-ef1234567890" ),
            LastUploadTime = new DateTime( 2025, 1, 15, 10, 30, 0, DateTimeKind.Utc ),
            Salt = 1234567890L,
            LastSaltChangeTime = new DateTime( 2025, 1, 1, 0, 0, 0, DateTimeKind.Utc ),
            LastMatomoPostTime = new DateTime( 2025, 1, 10, 12, 0, 0, DateTimeKind.Utc ),
            Version = 5
        };

        const string expectedJson = """
            {
              "ExceptionReportingAction": 1,
              "PerformanceProblemReportingAction": 2,
              "UsageReportingAction": 0,
              "DeviceId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
              "LastUploadTime": "2025-01-15T10:30:00Z",
              "Salt": 1234567890,
              "LastSaltChangeTime": "2025-01-01T00:00:00Z",
              "Issues": {},
              "Sessions": {},
              "LastMatomoPostTime": "2025-01-10T12:00:00Z",
              "version": 5
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(TelemetryConfiguration) ),
            json => JsonSerializer.Deserialize<TelemetryConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void TelemetryConfiguration_WithIssuesAndSessions_Serialization()
    {
        // Note: Dictionary ordering is not guaranteed, so we test round-trip consistency
        // rather than exact JSON matching for dictionaries
        var input = new TelemetryConfiguration
        {
            ExceptionReportingAction = ReportingAction.Default,
            Issues = ImmutableDictionary<string, ReportingStatus>.Empty
                .Add( "ISSUE001", ReportingStatus.Reported ),
            Sessions = ImmutableDictionary<string, DateTime>.Empty
                .Add( "session1", new DateTime( 2025, 1, 15, 10, 0, 0, DateTimeKind.Utc ) )
        };

        const string expectedJson = """
            {
              "ExceptionReportingAction": 0,
              "PerformanceProblemReportingAction": 0,
              "UsageReportingAction": 0,
              "DeviceId": null,
              "LastUploadTime": null,
              "Salt": null,
              "LastSaltChangeTime": null,
              "Issues": {
                "ISSUE001": 1
              },
              "Sessions": {
                "session1": "2025-01-15T10:00:00Z"
              },
              "LastMatomoPostTime": null,
              "version": null
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(TelemetryConfiguration) ),
            json => JsonSerializer.Deserialize<TelemetryConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void DiagnosticsConfiguration_Default_Serialization()
    {
        var input = new DiagnosticsConfiguration();

        // DiagnosticsConfiguration has complex nested structure; test basic serialization
        var json = this.JsonService.Serialize( input, typeof(DiagnosticsConfiguration) );
        this.Output.WriteLine( "DiagnosticsConfiguration JSON:" );
        this.Output.WriteLine( json );

        var deserialized = JsonSerializer.Deserialize<DiagnosticsConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );
        Assert.NotNull( deserialized.Logging );
        Assert.NotNull( deserialized.Debugging );
        Assert.NotNull( deserialized.CrashDumps );
        Assert.NotNull( deserialized.Profiling );
    }

    [Fact]
    public void LoggingConfiguration_Serialization()
    {
        // Use single dictionary entry to avoid ordering issues
        var input = new LoggingConfiguration
        {
            Processes = ImmutableDictionary<string, bool>.Empty
                .Add( "Compiler", true ),
            TraceCategories = ImmutableDictionary<string, bool>.Empty
                .Add( "Pipeline", true ),
            StopLoggingAfterHours = 4.5
        };

        const string expectedJson = """
            {
              "processes": {
                "Compiler": true
              },
              "trace": {
                "Pipeline": true
              },
              "stopLoggingAfterHours": 4.5
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void DebuggerConfiguration_Serialization()
    {
        // Use single dictionary entry to avoid ordering issues
        var input = new DebuggerConfiguration
        {
            Processes = ImmutableDictionary<string, bool>.Empty
                .Add( "Compiler", true )
        };

        const string expectedJson = """
            {
              "processes": {
                "Compiler": true
              }
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CrashDumpConfiguration_Serialization()
    {
        var input = new CrashDumpConfiguration
        {
            Processes = ImmutableDictionary<string, bool>.Empty
                .Add( "Compiler", true ),
            ExceptionTypes = ImmutableArray.Create( "*", "System.InvalidOperationException" )
        };

        const string expectedJson = """
            {
              "processes": {
                "Compiler": true
              },
              "exceptionTypes": [
                "*",
                "System.InvalidOperationException"
              ]
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void ProfilingConfiguration_Serialization()
    {
        var input = new ProfilingConfiguration
        {
            Kind = "performance",
            Processes = ImmutableDictionary<string, bool>.Empty
                .Add( "Compiler", true )
        };

        const string expectedJson = """
            {
              "kind": "performance",
              "processes": {
                "Compiler": true
              }
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CleanUpConfiguration_Serialization()
    {
        var input = new CleanUpConfiguration
        {
            LastCleanUpTime = new DateTime( 2025, 1, 15, 10, 30, 0, DateTimeKind.Utc ),
            Version = 1
        };

        const string expectedJson = """
            {
              "LastCleanUpTime": "2025-01-15T10:30:00Z",
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(CleanUpConfiguration) ),
            json => JsonSerializer.Deserialize<CleanUpConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void WelcomeConfiguration_Serialization()
    {
        var input = new WelcomeConfiguration
        {
            IsFirstStart = false,
            WelcomePageDisplayed = true,
            Version = 2
        };

        const string expectedJson = """
            {
              "IsFirstStart": false,
              "IsFirstTimeEvaluationLicenseRegistrationPending": true,
              "WelcomePageDisplayed": true,
              "IsWelcomePagePending": true,
              "version": 2
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(WelcomeConfiguration) ),
            json => JsonSerializer.Deserialize<WelcomeConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void ToastNotificationsConfiguration_Serialization()
    {
        var input = new ToastNotificationsConfiguration
        {
            Pauses = ImmutableDictionary<string, DateTime>.Empty
                .Add( "pause1", new DateTime( 2025, 1, 20, 12, 0, 0, DateTimeKind.Utc ) ),
            Notifications = ImmutableDictionary<string, ToastNotificationConfiguration>.Empty
                .Add(
                    "notification1",
                    new ToastNotificationConfiguration
                    {
                        SnoozeUntil = new DateTime( 2025, 2, 1, 0, 0, 0, DateTimeKind.Utc ),
                        Disabled = false
                    } ),
            Version = 1
        };

        const string expectedJson = """
            {
              "pauses": {
                "pause1": "2025-01-20T12:00:00Z"
              },
              "notifications": {
                "notification1": {
                  "snoozeUntil": "2025-02-01T00:00:00Z",
                  "disabled": false
                }
              },
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(ToastNotificationsConfiguration) ),
            json => JsonSerializer.Deserialize<ToastNotificationsConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void ToastNotificationConfiguration_Serialization()
    {
        var input = new ToastNotificationConfiguration
        {
            SnoozeUntil = new DateTime( 2025, 2, 1, 0, 0, 0, DateTimeKind.Utc ),
            Disabled = true
        };

        const string expectedJson = """
            {
              "snoozeUntil": "2025-02-01T00:00:00Z",
              "disabled": true
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void LicensingConfiguration_Serialization()
    {
        var input = new LicensingConfiguration
        {
            LastEvaluationStartDate = new DateTime( 2025, 1, 1, 0, 0, 0, DateTimeKind.Utc ),
            LegacyLicense = "LEGACY-LICENSE-KEY",
            Licenses = ImmutableArray.Create<string?>( "LICENSE-KEY-1", "LICENSE-KEY-2" ),
            CommunityLicenseReason = CommunityLicenseReason.OpenSource,
            Version = 3
        };

        const string expectedJson = """
            {
              "lastEvaluationStartDate": "2025-01-01T00:00:00Z",
              "license": "LEGACY-LICENSE-KEY",
              "licenses": [
                "LICENSE-KEY-1",
                "LICENSE-KEY-2"
              ],
              "CommunityLicenseReason": 3,
              "version": 3
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(LicensingConfiguration) ),
            json => JsonSerializer.Deserialize<LicensingConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void IdeExtensionsStatusConfiguration_Serialization()
    {
        var input = new IdeExtensionsStatusConfiguration
        {
            IsVisualStudioExtensionInstalled = true,
            Version = 1
        };

        const string expectedJson = """
            {
              "vs": true,
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(IdeExtensionsStatusConfiguration) ),
            json => JsonSerializer.Deserialize<IdeExtensionsStatusConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void LicenseAuditConfiguration_Serialization()
    {
        var input = new LicenseAuditConfiguration
        {
            LastAuditTimes = ImmutableDictionary<long, DateTime>.Empty
                .Add( 12345L, new DateTime( 2025, 1, 15, 10, 0, 0, DateTimeKind.Utc ) ),
            LastMatomoAuditTime = new DateTime( 2025, 1, 14, 8, 0, 0, DateTimeKind.Utc ),
            Version = 1
        };

        const string expectedJson = """
            {
              "LastAuditTimes": {
                "12345": "2025-01-15T10:00:00Z"
              },
              "LastMatomoAuditTime": "2025-01-14T08:00:00Z",
              "IsFirstMatomoAudit": true,
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(LicenseAuditConfiguration) ),
            json => JsonSerializer.Deserialize<LicenseAuditConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void RssClientConfiguration_Serialization()
    {
        // Note: LastFetchTime is internal so it won't be serialized by ToJson()
        var input = new RssClientConfiguration
        {
            PreferredFeed = RssFeed.Posts,
            Version = 1
        };

        const string expectedJson = """
            {
              "PreferredFeed": 1,
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(RssClientConfiguration) ),
            json => JsonSerializer.Deserialize<RssClientConfiguration>( json, this.JsonOptions ) );
    }

    [Fact]
    public void UserInfo_Serialization()
    {
        var input = new UserInfo
        {
            EmailAddress = "test@example.com",
            Version = 1
        };

        const string expectedJson = """
            {
              "emailAddress": "test@example.com",
              "version": 1
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(UserInfo) ),
            json => JsonSerializer.Deserialize<UserInfo>( json, this.JsonOptions ) );
    }

    [Fact]
    public void UserInfo_Empty_Serialization()
    {
        var input = new UserInfo();

        const string expectedJson = """
            {
              "emailAddress": null,
              "version": null
            }
            """;

        this.TestSerialization(
            input,
            expectedJson,
            obj => this.JsonService.Serialize( obj, typeof(UserInfo) ),
            json => JsonSerializer.Deserialize<UserInfo>( json, this.JsonOptions ) );
    }

    [Fact]
    public void LoggingConfiguration_CaseInsensitive_Keys()
    {
        // Test that case-insensitive dictionary keys work correctly
        var input = new LoggingConfiguration
        {
            Processes = ImmutableDictionary<string, bool>.Empty
                .WithComparers( StringComparer.OrdinalIgnoreCase )
                .Add( "Compiler", true )
                .Add( "DesignTime", false )
        };

        // Serialize
        var json = JsonSerializer.Serialize( input, this.JsonOptions );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( json );

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<LoggingConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );

        // Verify case-insensitive lookup works
        Assert.True( deserialized.Processes.ContainsKey( "compiler" ) ); // lowercase
        Assert.True( deserialized.Processes.ContainsKey( "COMPILER" ) ); // uppercase
        Assert.True( deserialized.Processes.ContainsKey( "Compiler" ) ); // original case
        Assert.True( deserialized.Processes["compiler"] );
        Assert.False( deserialized.Processes["designtime"] );
    }

    [Fact]
    public void TelemetryConfiguration_CaseInsensitive_Sessions()
    {
        // Test that TelemetryConfiguration.Sessions uses case-insensitive keys
        var input = new TelemetryConfiguration
        {
            Sessions = ImmutableDictionary<string, DateTime>.Empty
                .WithComparers( StringComparer.OrdinalIgnoreCase )
                .Add( "Session1", new DateTime( 2025, 1, 15, 10, 0, 0, DateTimeKind.Utc ) )
        };

        // Serialize
        var json = JsonSerializer.Serialize( input, this.JsonOptions );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( json );

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<TelemetryConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );

        // Verify case-insensitive lookup works
        Assert.True( deserialized.Sessions.ContainsKey( "session1" ) ); // lowercase
        Assert.True( deserialized.Sessions.ContainsKey( "SESSION1" ) ); // uppercase
    }

    [Fact]
    public void ToastNotificationsConfiguration_Pauses_UsesDefaultComparer()
    {
        // Test that Pauses (without case-insensitive converter) uses default comparer
        var input = new ToastNotificationsConfiguration
        {
            Pauses = ImmutableDictionary<string, DateTime>.Empty
                .Add( "Pause1", new DateTime( 2025, 1, 20, 12, 0, 0, DateTimeKind.Utc ) )
        };

        // Serialize
        var json = JsonSerializer.Serialize( input, this.JsonOptions );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( json );

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<ToastNotificationsConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );

        // Pauses uses default (case-sensitive) comparer - should NOT find lowercase
        Assert.True( deserialized.Pauses.ContainsKey( "Pause1" ) );
        Assert.False( deserialized.Pauses.ContainsKey( "pause1" ) );
    }
}
