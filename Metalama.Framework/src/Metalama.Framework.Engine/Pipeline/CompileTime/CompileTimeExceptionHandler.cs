// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Framework.Engine.Pipeline.CompileTime
{
    internal sealed class CompileTimeExceptionHandler : ICompileTimeExceptionHandler
    {
        private readonly ITelemetryService? _telemetryService;
        private readonly IStandardDirectories? _standardDirectories;
        private readonly IProjectOptions? _projectOptions;

        public CompileTimeExceptionHandler( IServiceProvider serviceProvider )
        {
            this._telemetryService = serviceProvider.GetBackstageService<ITelemetryService>();
            this._standardDirectories = serviceProvider.GetBackstageService<IStandardDirectories>();
            this._projectOptions = (IProjectOptions?) serviceProvider.GetService( typeof(IProjectOptions) );
        }

        // Writes the rich crash report. Returns <c>true</c> and sets <paramref name="reportFile"/> to the written path on
        // success. Returns <c>false</c> on failure: <paramref name="errorMessage"/> describes the failure when the write
        // was attempted but failed (an I/O error), or is <c>null</c> when there was simply no place to write the report
        // (no standard-directories service, e.g. in unit tests where the flow is cut). The write is best-effort: it is
        // itself handling an exception, so it must never throw. See #1701.
        private bool TryWriteLocalReport( string reportContent, [NotNullWhen( true )] out string? reportFile, out string? errorMessage )
        {
            reportFile = null;
            errorMessage = null;

            if ( this._standardDirectories == null )
            {
                return false;
            }

            var file = Path.Combine( this._standardDirectories.CrashReportsDirectory, $"exception-{Guid.NewGuid()}.txt" );

            try
            {
                Directory.CreateDirectory( this._standardDirectories.CrashReportsDirectory );
                File.WriteAllText( file, reportContent );
                reportFile = file;

                return true;
            }
            catch ( Exception e )
            {
                errorMessage = $"Cannot write the crash report to '{file}': {e.Message}";

                return false;
            }
        }

        // Opens the telemetry context for the report. With project options (the normal, project-scoped case), it honors
        // the project repository's metalama.json opt-out. With no project options (the handler used as a global fallback),
        // it reports as a tooling exception. The local crash report is written regardless. See #1701.
        private ITelemetryContext? GetTelemetryContext()
        {
            if ( this._telemetryService == null )
            {
                return null;
            }

            if ( this._projectOptions == null )
            {
                return this._telemetryService.OpenContext( this._telemetryService.GetToolingPolicy() );
            }

            var projectDirectory = string.IsNullOrEmpty( this._projectOptions.ProjectPath ) ? null : Path.GetDirectoryName( this._projectOptions.ProjectPath );

            return this._telemetryService.OpenContext( this._telemetryService.GetPolicy( projectDirectory ) );
        }

        public void ReportException(
            Exception exception,
            Action<Diagnostic> reportDiagnostic,
            bool canIgnoreException,
            out bool isHandled )
        {
            // Unwrap AggregateException.
            if ( exception is AggregateException { InnerExceptions: { Count: 1 } innerExceptions } )
            {
                exception = innerExceptions[0];
            }

            SyntaxNode? node = null;

            if ( exception is SyntaxProcessingException syntaxProcessingException )
            {
                node = syntaxProcessingException.SyntaxNode;
            }

            var exceptionText = new StringBuilder();

            exceptionText.AppendLineInvariant( $"Metalama Version: {EngineAssemblyMetadataReader.Instance.PackageVersion}" );
            exceptionText.AppendLineInvariant( $"Runtime: {RuntimeInformation.FrameworkDescription}" );
            exceptionText.AppendLineInvariant( $"Processor Architecture: {RuntimeInformation.ProcessArchitecture}" );
            exceptionText.AppendLineInvariant( $"OS Description: {RuntimeInformation.OSDescription}" );
            exceptionText.AppendLineInvariant( $"OS Architecture: {RuntimeInformation.OSArchitecture}" );
            exceptionText.AppendLineInvariant( $"Exception type: {exception.GetType()}" );
            exceptionText.AppendLineInvariant( $"Exception message: {exception.Message}" );

            try
            {
                // The next line may fail.
                var exceptionToString = exception.ToString();
                exceptionText.AppendLine( "===== Exception ===== " );
                exceptionText.AppendLine( exceptionToString );

                if ( node != null )
                {
                    exceptionText.AppendLine( "===== Syntax Tree ===== " );
                    exceptionText.AppendLine( node.SyntaxTree.GetText().ToString() );
                }
            }

            // ReSharper disable once EmptyGeneralCatchClause
            catch { }

            string reportFile;

            if ( this.TryWriteLocalReport( exceptionText.ToString(), out var writtenReportFile, out var reportErrorMessage ) )
            {
                reportFile = writtenReportFile;
            }
            else if ( reportErrorMessage != null )
            {
                // The report could not be written. Cite "<error>" in the main diagnostic and surface the write failure as
                // a separate diagnostic so the user understands why the cited path is unavailable. See #1701.
                reportFile = "<error>";
                reportDiagnostic( GeneralDiagnosticDescriptors.CannotWriteCrashReportFile.CreateRoslynDiagnostic( node?.GetLocation(), reportErrorMessage ) );
            }
            else
            {
                // There was no place to write the report (e.g. in unit tests where the flow is cut).
                reportFile = "(crash report unavailable)";
            }

            var diagnosticDefinition =
                canIgnoreException ? GeneralDiagnosticDescriptors.IgnorableUnhandledException : GeneralDiagnosticDescriptors.UnhandledException;

            reportDiagnostic(
                diagnosticDefinition.CreateRoslynDiagnostic(
                    node?.GetLocation(),
                    (exception.Message, reportFile),
                    description: exceptionText.ToString() ) );

            // We have already written the rich local report (reportFile) and cited it in the diagnostic above, so the
            // telemetry path must not write a duplicate local report (writeLocalReport: false). See #1701.
            this.GetTelemetryContext()?.ReportException( exception, writeLocalReport: false );

            isHandled = true;
        }
    }
}