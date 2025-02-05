// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Framework.Engine.Pipeline.CompileTime
{
    internal sealed class CompileTimeExceptionHandler : ICompileTimeExceptionHandler
    {
        private readonly IExceptionReporter? _exceptionReporter;
        private readonly IStandardDirectories _standardDirectories;

        public CompileTimeExceptionHandler( IServiceProvider serviceProvider )
        {
            this._exceptionReporter = (IExceptionReporter?) serviceProvider.GetService( typeof(IExceptionReporter) );
            this._standardDirectories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        }

        public void ReportException( Exception exception, Action<Diagnostic> reportDiagnostic, bool canIgnoreException, out bool isHandled )
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

            var reportFile = Path.Combine( this._standardDirectories.CrashReportsDirectory, $"exception-{Guid.NewGuid()}.txt" );

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

            File.WriteAllText( reportFile, exceptionText.ToString() );

            var diagnosticDefinition =
                canIgnoreException ? GeneralDiagnosticDescriptors.IgnorableUnhandledException : GeneralDiagnosticDescriptors.UnhandledException;

            reportDiagnostic( diagnosticDefinition.CreateRoslynDiagnostic( node?.GetLocation(), (exception.Message, reportFile) ) );

            this._exceptionReporter?.ReportException( exception, localReportPath: reportFile );

            isHandled = true;
        }
    }
}