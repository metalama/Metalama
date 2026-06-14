// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Audit;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Welcome;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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

// HashSet types needed for ImmutableHashSet built-in support
[JsonSerializable( typeof(HashSet<string>) )]
internal partial class BackstageJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Creates combined <see cref="JsonSerializerOptions"/> that chains this context with additional resolvers.
    /// </summary>
    /// <param name="writeIndented">Whether to write indented JSON.</param>
    /// <param name="additionalResolvers">Additional type info resolvers to chain after this context.</param>
    /// <returns>Combined options with all resolvers in the chain.</returns>
    public static JsonSerializerOptions CreateCombinedOptions(
        bool writeIndented,
        IEnumerable<IJsonTypeInfoResolver> additionalResolvers )
    {
        var options = new JsonSerializerOptions { WriteIndented = writeIndented, PropertyNameCaseInsensitive = true, TypeInfoResolver = Default };

        // Add additional resolvers (e.g., FrameworkConfigurationJsonContext)
        foreach ( var resolver in additionalResolvers )
        {
            options.TypeInfoResolverChain.Add( resolver );
        }

        return options;
    }
}