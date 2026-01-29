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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Serialization;

[JsonSourceGenerationOptions( WriteIndented = true )]
[JsonSerializable( typeof(TelemetryConfiguration) )]
[JsonSerializable( typeof(LicensingConfiguration) )]
[JsonSerializable( typeof(DiagnosticsConfiguration) )]
[JsonSerializable( typeof(LoggingConfiguration) )]
[JsonSerializable( typeof(DebuggerConfiguration) )]
[JsonSerializable( typeof(CrashDumpConfiguration) )]
[JsonSerializable( typeof(ProfilingConfiguration) )]
[JsonSerializable( typeof(CleanUpConfiguration) )]
[JsonSerializable( typeof(WelcomeConfiguration) )]
[JsonSerializable( typeof(ToastNotificationsConfiguration) )]
[JsonSerializable( typeof(ToastNotificationConfiguration) )]
[JsonSerializable( typeof(IdeExtensionsStatusConfiguration) )]
[JsonSerializable( typeof(RssClientConfiguration) )]
[JsonSerializable( typeof(LicenseAuditConfiguration) )]
[JsonSerializable( typeof(UserInfo) )]
[JsonSerializable( typeof(ReportingAction) )]
[JsonSerializable( typeof(ReportingStatus) )]
[JsonSerializable( typeof(RssFeed) )]
[JsonSerializable( typeof(ImmutableDictionary<string, bool>) )]
[JsonSerializable( typeof(ImmutableDictionary<string, DateTime>) )]
[JsonSerializable( typeof(ImmutableDictionary<string, ReportingStatus>) )]
[JsonSerializable( typeof(ImmutableDictionary<string, ToastNotificationConfiguration>) )]
[JsonSerializable( typeof(ImmutableDictionary<long, DateTime>) )]
[JsonSerializable( typeof(ImmutableArray<string>) )]
[JsonSerializable( typeof(ImmutableArray<string?>) )]
// Dictionary types needed for ImmutableDictionary converter deserialization
[JsonSerializable( typeof(Dictionary<string, bool>) )]
[JsonSerializable( typeof(Dictionary<string, DateTime>) )]
[JsonSerializable( typeof(Dictionary<string, ReportingStatus>) )]
[JsonSerializable( typeof(Dictionary<string, ToastNotificationConfiguration>) )]
[JsonSerializable( typeof(Dictionary<long, DateTime>) )]
internal partial class BackstageJsonContext : JsonSerializerContext
{
    private static BackstageJsonContext? _indented;
    private static BackstageJsonContext? _compact;

    /// <summary>
    /// Gets the context configured for indented output (used for writing configuration files).
    /// </summary>
    public static BackstageJsonContext Indented => _indented ??= new BackstageJsonContext( CreateOptions( writeIndented: true ) );

    /// <summary>
    /// Gets the context configured for compact output.
    /// </summary>
    public static BackstageJsonContext Compact => _compact ??= new BackstageJsonContext( CreateOptions( writeIndented: false ) );

    private static JsonSerializerOptions CreateOptions( bool writeIndented )
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNameCaseInsensitive = true
        };

        // Add converter for ImmutableDictionary with case-insensitive string keys
        options.Converters.Add( new ImmutableDictionaryConverterFactory() );

        return options;
    }
}
