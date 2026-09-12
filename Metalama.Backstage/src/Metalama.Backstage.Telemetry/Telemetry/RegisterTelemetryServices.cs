// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Repositories;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Extension methods that register the telemetry services in a <see cref="ServiceProviderBuilder"/>.
/// </summary>
public static class RegisterTelemetryServices
{
    /// <summary>
    /// Registers the telemetry services: usage sessions, exception reports, the upload queue and the uploaders. It
    /// requires the core and configuration services.
    /// </summary>
    public static ServiceProviderBuilder AddTelemetryServices( this ServiceProviderBuilder serviceProviderBuilder, TelemetryInitializationOptions options )
        => serviceProviderBuilder
            .AddSingleton( options )
            .AddSingleton<ITelemetryRetentionPolicy>( serviceProvider => new TelemetryRetentionPolicy( serviceProvider ) )
            .AddSingleton<IRepositoryConfigurationService>( serviceProvider => new RepositoryConfigurationService( serviceProvider ) )
            .AddSingleton( serviceProvider => new TelemetryLogger( serviceProvider ) )
            .AddSingleton<LocalExceptionReporter>( serviceProvider => new LocalExceptionReporter( serviceProvider ) )

            // A single ExceptionReporter instance is exposed under both IExceptionReportManager (review/upload, used by
            // the worker and CLI) and IExceptionCapturer (capture, used by the telemetry context). See #1701.
            .AddSingleton<ExceptionReporter>( serviceProvider => new ExceptionReporter( new TelemetryQueue( serviceProvider ), serviceProvider ) )
            .AddSingleton<IExceptionReportManager>( serviceProvider => serviceProvider.GetRequiredBackstageService<ExceptionReporter>() )
            .AddSingleton<IExceptionCapturer>( serviceProvider => serviceProvider.GetRequiredBackstageService<ExceptionReporter>() )
            .AddSingleton<ITelemetryUploader>( serviceProvider => new TelemetryUploader( serviceProvider ) )
            .AddSingleton<IUsageSessionFactory>( serviceProvider => new UsageSessionFactory( serviceProvider ) )
            .AddSingleton<ITelemetryConfigurationService>( serviceProvider => new TelemetryConfigurationService( serviceProvider ) )
            .AddSingleton<ITelemetryService>( serviceProvider => new TelemetryService( serviceProvider ) )
            .AddSingleton<TelemetryReportUploader>( serviceProvider => new TelemetryReportUploader( serviceProvider ) )
            .AddSingleton<MatomoUploader>( serviceProvider => new MatomoUploader( serviceProvider ) );
}
