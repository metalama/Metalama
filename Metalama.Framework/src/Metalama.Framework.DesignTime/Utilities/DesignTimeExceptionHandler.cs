// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Services;
using SharpCrafters.Backstage.Diagnostics;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.Telemetry;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Utilities
{
    /// <summary>
    /// Reports the exceptions of the design-time process: it logs them, and reports the classified errors to telemetry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <see cref="Pipeline.DesignTimeAspectPipeline"/> opens one <see cref="ITelemetryContext"/> when it is created and
    /// registers it with <see cref="RegisterProjectTelemetryContext"/>. The handler reports the exceptions of a project to the
    /// registered context, so the policy (the consent of the user, the <c>metalama.json</c> opt-out of the repository, and the
    /// gates of the process) is resolved once per project and not when an exception occurs. See #2421.
    /// </para>
    /// <para>
    /// The handler opens a context itself only for an exception that has no registered context: an exception that has no
    /// project, or an exception of a project for which no pipeline has been created.
    /// </para>
    /// <para>
    /// The handler stores one context per <see cref="ProjectKey"/>. A pipeline that is created again for the same project
    /// replaces the context of the previous pipeline. Neither <see cref="ProjectKey"/> nor <see cref="ITelemetryContext"/>
    /// references a compilation, so the dictionary does not retain any code model.
    /// </para>
    /// </remarks>
    public sealed class DesignTimeExceptionHandler : IGlobalService
    {
        private readonly ITelemetryService? _telemetryService;
        private readonly IProjectOptionsFactory? _projectOptionsFactory;
        private readonly ConcurrentDictionary<ProjectKey, ITelemetryContext> _projectTelemetryContexts = new();

        public DesignTimeExceptionHandler( ServiceProvider<IGlobalService> serviceProvider )
        {
            this._telemetryService = serviceProvider.GetBackstageService<ITelemetryService>();
            this._projectOptionsFactory = serviceProvider.GetService<IProjectOptionsFactory>();
        }

        // It is critical that OperationCanceledException is NOT handled, i.e. this exception should flow to the caller, otherwise VS will be satisfied
        // with the incomplete results it received, and cache them.
        internal static bool MustHandle( Exception e ) => ExceptionClassifier.Classify( e ).IsError;

        /// <summary>
        /// Gets the telemetry service, or <see langword="null"/> when Backstage is not initialized.
        /// </summary>
        /// <remarks>
        /// The service of the service provider is preferred. The service of <see cref="BackstageServiceFactoryInitializer"/> is
        /// the fallback for a service provider that was created without the Backstage services. The value is memoized. A
        /// <see langword="null"/> value is not memoized, so the fallback is resolved again until Backstage is initialized.
        /// </remarks>
        [Memo]
        private ITelemetryService? TelemetryService
            => this._telemetryService ?? BackstageServiceFactoryInitializer.ServiceProvider.GetBackstageService<ITelemetryService>();

        /// <summary>
        /// Opens the telemetry context of a project. The pipeline of the project calls this method once, when it is created.
        /// </summary>
        /// <param name="projectOptions">The options of the project. The policy is resolved from the directory of the project.</param>
        /// <returns>The opened context, or <see langword="null"/> when the telemetry service is not available.</returns>
        internal ITelemetryContext? OpenProjectTelemetryContext( IProjectOptions projectOptions )
        {
            var telemetryService = this.TelemetryService;

            return telemetryService?.OpenContext( telemetryService.GetPolicy( GetProjectDirectory( projectOptions ) ) );
        }

        /// <summary>
        /// Registers the telemetry context of a project, so that the exceptions of this project are reported to it. A context
        /// that is already registered for the same project is replaced.
        /// </summary>
        internal void RegisterProjectTelemetryContext( ProjectKey projectKey, ITelemetryContext telemetryContext )
            => this._projectTelemetryContexts[projectKey] = telemetryContext;

        /// <summary>
        /// Removes the registration of the telemetry context of a project, when this context is still the registered one.
        /// </summary>
        /// <remarks>
        /// The removal goes through <see cref="ICollection{T}.Remove"/>, which <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// implements atomically for a key and a value, because the overload of <c>TryRemove</c> that takes a pair is not
        /// available on .NET Framework.
        /// </remarks>
        internal void UnregisterProjectTelemetryContext( ProjectKey projectKey, ITelemetryContext telemetryContext )
            => ((ICollection<KeyValuePair<ProjectKey, ITelemetryContext>>) this._projectTelemetryContexts).Remove(
                new KeyValuePair<ProjectKey, ITelemetryContext>( projectKey, telemetryContext ) );

        /// <summary>
        /// Reports an exception with no project context: the local crash report is written, but no telemetry is sent (the
        /// design-time process spans many projects, so a report not associated with one has no repository to consult).
        /// Prefer an overload that identifies the project whenever one is known. See #1701.
        /// </summary>
        internal void ReportException( Exception e, ILogger? logger = null ) => this.ReportException( e, null, null, logger );

        /// <summary>
        /// Reports an exception for the given Roslyn <paramref name="project"/>. The exception is reported to the telemetry
        /// context of the project when one is registered.
        /// </summary>
        internal void ReportException( Exception e, Microsoft.CodeAnalysis.Project project, ILogger? logger = null )
            => this.ReportException( e, ProjectKeyFactory.FromProject( project ), this._projectOptionsFactory?.GetProjectOptions( project ), logger );

        /// <summary>
        /// Reports an exception for the project described by <paramref name="projectOptions"/>, when its <see cref="ProjectKey"/>
        /// is not known. A context is opened for the directory of the project. Prefer an overload that takes the
        /// <see cref="ProjectKey"/> whenever it is known.
        /// </summary>
        internal void ReportException( Exception e, IProjectOptions? projectOptions, ILogger? logger = null )
            => this.ReportException( e, null, projectOptions, logger );

        /// <summary>
        /// Reports an exception for the project identified by <paramref name="projectKey"/>. The local crash report is written,
        /// and telemetry is captured through the telemetry context of the project when one is registered. Otherwise, a context
        /// is opened for the directory of the project given by <paramref name="projectOptions"/>, so the repository
        /// <c>metalama.json</c> opt-out is honored. See #1701 and #2421.
        /// </summary>
        internal void ReportException( Exception e, ProjectKey? projectKey, IProjectOptions? projectOptions, ILogger? logger = null )
        {
            logger ??= Logger.DesignTime;

            // A DiagnosticException means that the failure has already been analyzed and that the responsibility for it
            // lies with the user or with the build environment, typically a nested build that cannot restore its NuGet
            // packages. Such a condition is not a defect of Metalama and must not be sent as a crash report. See #1744.
            if ( DiagnosticException.TryFind( e ) is { } diagnosticException )
            {
                logger.Warning?.Log( $"A user-attributable failure occurred: {diagnosticException.GetSingleLineMessage()}" );

                return;
            }

            var classifiedException = ExceptionClassifier.Classify( e );
            logger.LogException( classifiedException );

            if ( classifiedException.IsError )
            {
                this.GetTelemetryContext( projectKey, projectOptions )?.ReportException( classifiedException );
            }
        }

        /// <summary>
        /// Gets the registered telemetry context of a project, or opens a context for the directory of the project when no
        /// context is registered.
        /// </summary>
        private ITelemetryContext? GetTelemetryContext( ProjectKey? projectKey, IProjectOptions? projectOptions )
        {
            if ( projectKey != null && this._projectTelemetryContexts.TryGetValue( projectKey, out var registeredContext ) )
            {
                return registeredContext;
            }

            var telemetryService = this.TelemetryService;

            return telemetryService?.OpenContext( telemetryService.GetPolicy( GetProjectDirectory( projectOptions ) ) );
        }

        private static string? GetProjectDirectory( IProjectOptions? projectOptions )
            => string.IsNullOrEmpty( projectOptions?.ProjectPath ) ? null : Path.GetDirectoryName( projectOptions!.ProjectPath );
    }
}
