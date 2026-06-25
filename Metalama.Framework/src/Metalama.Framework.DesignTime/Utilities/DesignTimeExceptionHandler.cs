// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Utilities
{
    public sealed class DesignTimeExceptionHandler : IGlobalService
    {
        private readonly ITelemetryService? _telemetryService;
        private readonly IProjectOptionsFactory? _projectOptionsFactory;

        public DesignTimeExceptionHandler( ServiceProvider<IGlobalService> serviceProvider )
        {
            this._telemetryService = serviceProvider.GetBackstageService<ITelemetryService>();
            this._projectOptionsFactory = serviceProvider.GetService<IProjectOptionsFactory>();
        }

        // It is critical that OperationCanceledException is NOT handled, i.e. this exception should flow to the caller, otherwise VS will be satisfied
        // with the incomplete results it received, and cache them.
        internal static bool MustHandle( Exception e ) => ExceptionClassifier.Classify( e ).IsError;

        /// <summary>
        /// Reports an exception with no project context: the local crash report is written, but no telemetry is sent (the
        /// design-time process spans many projects, so a report not associated with one has no repository to consult).
        /// Prefer an overload that identifies the project whenever one is known. See #1701.
        /// </summary>
        internal void ReportException( Exception e, ILogger? logger = null ) => this.ReportException( e, (IProjectOptions?) null, logger );

        /// <summary>
        /// Reports an exception for the given Roslyn <paramref name="project"/>: resolves its <see cref="IProjectOptions"/>
        /// and reports through the per-project context so the repository <c>metalama.json</c> opt-out is honored. See #1701.
        /// </summary>
        internal void ReportException( Exception e, Microsoft.CodeAnalysis.Project project, ILogger? logger = null )
            => this.ReportException( e, this._projectOptionsFactory?.GetProjectOptions( project ), logger );

        /// <summary>
        /// Reports an exception for the project described by <paramref name="projectOptions"/>: the local crash report is
        /// written, and telemetry is captured through the per-project context so the repository <c>metalama.json</c>
        /// opt-out is honored. See #1701.
        /// </summary>
        internal void ReportException( Exception e, IProjectOptions? projectOptions, ILogger? logger = null )
        {
            logger ??= Logger.DesignTime;

            var classifiedException = ExceptionClassifier.Classify( e );
            logger.LogException( classifiedException );

            if ( classifiedException.IsError )
            {
                // TODO: Is this guaranteed not to be called before the BackstageServiceFactory is initialized?
                var telemetryService = this._telemetryService ?? BackstageServiceFactory.ServiceProvider.GetBackstageService<ITelemetryService>();

                var projectDirectory = string.IsNullOrEmpty( projectOptions?.ProjectPath ) ? null : Path.GetDirectoryName( projectOptions!.ProjectPath );

                telemetryService?.OpenContext( telemetryService.GetPolicy( projectDirectory ) ).ReportException( classifiedException );
            }
        }
    }
}
