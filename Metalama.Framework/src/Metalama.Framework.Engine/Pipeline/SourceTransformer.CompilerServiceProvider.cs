// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Compiler.Services;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using ILogger = Metalama.Compiler.Services.ILogger;

namespace Metalama.Framework.Engine.Pipeline;

public sealed partial class SourceTransformer
{
    private sealed class CompilerServiceProvider : IDisposableServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _services = new();
        private readonly IDisposable _scope;
        private readonly IUsageSession? _session;
        private readonly ITelemetryContext _telemetryContext;

        public CompilerServiceProvider( IServiceProvider serviceProvider, AnalyzerConfigOptionsProvider contextAnalyzerConfigOptionsProvider )
        {
            this._serviceProvider = serviceProvider;

            var options = new MSBuildProjectOptions( contextAnalyzerConfigOptionsProvider.GlobalOptions );
            var assemblyName = options.AssemblyName ?? "Unnamed";

            var loggerFactory = serviceProvider.GetLoggerFactory();
            this._scope = loggerFactory.EnterScope( $"{assemblyName}({options.TargetFramework})" );

            this._services.Add( typeof(ILoggerFactory), loggerFactory );
            this._services.Add( typeof(ILogger), new LoggerAdapter( loggerFactory.GetLogger( "Compiler" ) ) );

            // Open a telemetry context for the project's repository so the repository-root metalama.json opt-out is
            // honored. The context is resolved from the project directory; when the project path is unknown, telemetry
            // is disabled (the null context still writes local crash reports). See #1701.
            var telemetryService = serviceProvider.GetRequiredBackstageService<ITelemetryService>();
            var projectDirectory = string.IsNullOrEmpty( options.ProjectPath ) ? null : Path.GetDirectoryName( options.ProjectPath );

            this._telemetryContext = telemetryService.OpenContext( telemetryService.GetPolicy( projectDirectory ) );

            // Expose the telemetry context through this service provider, and route engine exceptions through it. The
            // adapter implements the compiler's IExceptionReporter (Metalama.Compiler.Services), so it must be registered
            // under that type for Metalama.Compiler to resolve it.
            this._services.Add( typeof(ITelemetryContext), this._telemetryContext );
            this._services.Add( typeof(IExceptionReporter), new ExceptionReporterAdapter( this._telemetryContext ) );

            // Initialize usage reporting through the telemetry context (a no-op when the repository opted out).
            try
            {
                if ( options.AssemblyName != null )
                {
                    this._session = this._telemetryContext.StartUsageSession( "TransformerUsage", options.AssemblyName );
                }
            }
            catch ( Exception e )
            {
                ReportException( e, serviceProvider, false );

                // We don't re-throw here as we don't want compiler to crash because of usage reporting exceptions.
            }
        }

        public object? GetService( Type serviceType )
        {
            this._services.TryGetValue( serviceType, out var service );

            return service;
        }

        public void DisposeServices( Action<Diagnostic> reportDiagnostic )
        {
            // Report any repository-configuration warnings (e.g. a misplaced or malformed metalama.json) gathered when
            // the telemetry context was opened. We report them once per project, on the diagnostic sink the compiler
            // provides, with the location pointing at the metalama.json file. See #1701.
            foreach ( var warning in this._telemetryContext.Warnings )
            {
                var location = warning.FilePath != null ? Location.Create( warning.FilePath, default, default ) : null;

                reportDiagnostic( GeneralDiagnosticDescriptors.InvalidRepositoryConfiguration.CreateRoslynDiagnostic( location, warning.Message ) );
            }

            // Report usage.
            try
            {
                this._session?.Dispose();
            }
            catch ( Exception e )
            {
                ReportException( e, this._serviceProvider, false );

                // We don't re-throw here as we don't want compiler to crash because of usage reporting exceptions.
            }

            // Close the scope.
            this._scope.Dispose();
        }
    }
}