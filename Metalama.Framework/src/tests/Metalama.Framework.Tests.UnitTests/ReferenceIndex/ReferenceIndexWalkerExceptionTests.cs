// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Introspection.References;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Introspection;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpCrafters.Backstage.Diagnostics;
using SharpCrafters.Backstage.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Verifies that an exception thrown while the reference indexer processes one syntax node is contained to that node, reported
/// to the exception handler of the project, and reported as a diagnostic of the node. See issue #2415.
/// </summary>
/// <remarks>
/// The exception is thrown by the fault injection point of the walker for the nodes of the kind
/// <see cref="SyntaxKind.ExpressionStatement"/>. In the code of these tests, only the methods of <c>B</c> contain expression statements:
/// the first one is the invocation of <c>A.F()</c> in <c>B.M()</c>.
/// </remarks>
public sealed class ReferenceIndexWalkerExceptionTests : UnitTestClass
{
    private static readonly Dictionary<string, string> _code = new()
    {
        ["A.cs"] = "class A { public static int F() => 0; public static int G() => 0; }",
        ["B.cs"] = "class B { void M() { A.F(); A.G(); } void N() { A.F(); } }"
    };

    private static readonly string _expressionStatementFault = FaultInjectionPoints.GetReferenceIndexWalkerVisit( SyntaxKind.ExpressionStatement );

    /// <summary>
    /// Verifies that the index is built when the processing of a node fails, that only the references of that node are missing,
    /// and that a diagnostic is reported at the location of the node.
    /// </summary>
    [Fact]
    public void FailedNode_IsSkipped()
    {
        using var testContext = this.CreateTestContext( new AdditionalServiceCollection() );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault, count: 1 );

        var index = BuildIndex( testContext, compilation );

        Assert.Equal( ["B.N()"], GetReferencingSymbols( index, compilation, "F" ) );
        Assert.Equal( ["B.M()"], GetReferencingSymbols( index, compilation, "G" ) );
        Assert.Equal( 1, index.SkippedNodeCount );

        var diagnostic = Assert.Single( index.Diagnostics );
        Assert.Equal( "LAMA0094", diagnostic.Id );
        Assert.Equal( DiagnosticSeverity.Warning, diagnostic.Severity );
        Assert.Equal( "A.F();", diagnostic.Location.SourceTree!.GetText( Xunit.TestContext.Current.CancellationToken ).ToString( diagnostic.Location.SourceSpan ) );
        Assert.Contains( "ExpressionStatement", diagnostic.GetMessage(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that an exception is reported once to the exception handler of the project when the processing of several nodes fails
    /// with the same exception, and that a diagnostic is reported for every node.
    /// </summary>
    [Fact]
    public void SameException_ReportedOnce()
    {
        var exceptionHandler = new RecordingExceptionHandler();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService<ICompileTimeExceptionHandler>( exceptionHandler );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault );

        var index = BuildIndex( testContext, compilation );

        Assert.Equal( 3, index.SkippedNodeCount );
        Assert.Equal( 3, index.Diagnostics.Count( d => d.Id == "LAMA0094" ) );

        // The exception is wrapped, so that the crash report gives the location of the node.
        Assert.IsType<SyntaxProcessingException>( Assert.Single( exceptionHandler.Exceptions ) );
        Assert.Single( index.Diagnostics, d => d.Id == RecordingExceptionHandler.DiagnosticId );
    }

    /// <summary>
    /// Verifies that, when the project has no exception handler, an exception is reported once to the telemetry context that the
    /// host registered in the service provider of the project, as a workspace does.
    /// </summary>
    [Fact]
    public void NoExceptionHandler_ReportedToTelemetryContext()
    {
        var telemetryContext = new RecordingTelemetryContext();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddUntypedProjectService( typeof(ITelemetryContext), telemetryContext );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault );

        var builder = new InboundReferenceIndexBuilder( compilation.Project.ServiceProvider, ReferenceIndexerOptions.All, StructuralSymbolComparer.Default );

        foreach ( var syntaxTree in compilation.PartialCompilation.SyntaxTreeCollection )
        {
            builder.IndexSyntaxTree( syntaxTree, compilation.CompilationContext.SemanticModelProvider, Xunit.TestContext.Current.CancellationToken );
        }

        var index = builder.ToReadOnly();

        Assert.Equal( 3, index.SkippedNodeCount );
        Assert.IsType<SyntaxProcessingException>( Assert.Single( telemetryContext.Exceptions ) );
    }

    /// <summary>
    /// Verifies that a cancellation is not contained.
    /// </summary>
    [Fact]
    public void Cancellation_IsNotContained()
    {
        using var testContext = this.CreateTestContext( new AdditionalServiceCollection() );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault, () => new OperationCanceledException() );

        Assert.ThrowsAny<OperationCanceledException>( () => BuildIndex( testContext, compilation ) );
    }

    /// <summary>
    /// Verifies that the introspection reference graph returns the references that it could index, and reports the diagnostics of the
    /// skipped node once to <see cref="IIntrospectionReferenceDiagnosticSink"/>, although the index is queried twice.
    /// </summary>
    [Fact]
    public void IntrospectionInboundReferences_ReportDiagnosticsOnce()
    {
        var sink = new RecordingDiagnosticSink();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService<IIntrospectionReferenceDiagnosticSink>( sink );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault, count: 1 );

        var graph = compilation.Project.ServiceProvider.GetRequiredService<IProjectIntrospectionService>().GetReferenceGraph( compilation );
        var methodF = GetMethodOfA( compilation, "F" );

        var references1 = graph.GetInboundReferences( methodF, cancellationToken: Xunit.TestContext.Current.CancellationToken ).ToList();
        var references2 = graph.GetInboundReferences( methodF, cancellationToken: Xunit.TestContext.Current.CancellationToken ).ToList();

        Assert.Equal( ["N"], references1.SelectAsArray( r => ((INamedDeclaration) r.OriginDeclaration).Name ) );
        Assert.Equal( ["N"], references2.SelectAsArray( r => ((INamedDeclaration) r.OriginDeclaration).Name ) );

        var report = Assert.Single( sink.Reports );
        Assert.Same( compilation, report.Compilation );
        Assert.Equal( "LAMA0094", Assert.Single( report.Diagnostics ).Id );
    }

    /// <summary>
    /// Verifies that the outbound references of the introspection reference graph contain the references that could be indexed, and that
    /// the diagnostics of the skipped node are reported to <see cref="IIntrospectionReferenceDiagnosticSink"/>.
    /// </summary>
    [Fact]
    public void IntrospectionOutboundReferences_ReportDiagnostics()
    {
        var sink = new RecordingDiagnosticSink();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService<IIntrospectionReferenceDiagnosticSink>( sink );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault, count: 1 );

        var graph = compilation.Project.ServiceProvider.GetRequiredService<IProjectIntrospectionService>().GetReferenceGraph( compilation );
        var methodM = compilation.Types.OfName( "B" ).Single().Methods.OfName( "M" ).Single();

        var references = graph.GetOutboundReferences( methodM, Xunit.TestContext.Current.CancellationToken ).ToList();

        Assert.Contains( references, r => r.DestinationDeclaration is INamedDeclaration { Name: "G" } );
        Assert.DoesNotContain( references, r => r.DestinationDeclaration is INamedDeclaration { Name: "F" } );
        Assert.Equal( "LAMA0094", Assert.Single( Assert.Single( sink.Reports ).Diagnostics ).Id );
    }

    /// <summary>
    /// Verifies that the source reference index of the pipeline extensions reports the diagnostics of the skipped node once to the sink
    /// given by its creator, although the index is read twice.
    /// </summary>
    [Fact]
    public async Task SourceReferenceIndex_ReportsDiagnosticsOnce()
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );
        testContext.FaultInjector.ArmFault( _expressionStatementFault, count: 1 );

        var diagnostics = new List<Diagnostic>();

        var requirements = new SourceIndexRequirements(
            [ReferenceIndexerRequirements.Create( GetMethodOfA( compilation, "F" ), ReferenceKinds.Invocation, false )] );

        using var sourceIndex = SourceReferenceIndexService.Create( compilation.Project.ServiceProvider, compilation, [requirements], diagnostics.Add );

        var index1 = await sourceIndex.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );
        var index2 = await sourceIndex.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Same( index1, index2 );
        Assert.Equal( 1, index1.SkippedNodeCount );
        Assert.Equal( "LAMA0094", Assert.Single( diagnostics ).Id );
    }

    /// <summary>
    /// Builds the inbound index of every syntax tree with the options and the comparer of the introspection API.
    /// </summary>
    private static InboundReferenceIndex BuildIndex( MetalamaTestContext testContext, CompilationModel compilation )
    {
        var builder = new InboundReferenceIndexBuilder( testContext.ServiceProvider, ReferenceIndexerOptions.All, StructuralSymbolComparer.Default );

        foreach ( var syntaxTree in compilation.PartialCompilation.SyntaxTreeCollection )
        {
            builder.IndexSyntaxTree( syntaxTree, compilation.CompilationContext.SemanticModelProvider, Xunit.TestContext.Current.CancellationToken );
        }

        return builder.ToReadOnly();
    }

    private static IMethod GetMethodOfA( CompilationModel compilation, string name )
        => compilation.Types.OfName( "A" ).Single().Methods.OfName( name ).Single();

    /// <summary>
    /// Returns the names of the symbols that reference the given method of <c>A</c>, sorted.
    /// </summary>
    private static IReadOnlyList<string> GetReferencingSymbols( InboundReferenceIndex index, CompilationModel compilation, string methodName )
    {
        var symbol = GetMethodOfA( compilation, methodName ).GetSymbol().AssertNotNull();

        return index.TryGetInboundReferences( symbol, out var info )
            ? info.References.Select( r => r.ReferencingSymbol.ToTestName() ).ToOrderedList( n => n, StringComparer.Ordinal )
            : [];
    }

    /// <summary>
    /// An exception handler that records the exceptions and reports one diagnostic for each of them.
    /// </summary>
    private sealed class RecordingExceptionHandler : ICompileTimeExceptionHandler
    {
        public const string DiagnosticId = "TEST0001";

        private static readonly DiagnosticDescriptor _descriptor = new(
            DiagnosticId,
            "Recorded exception",
            "The exception was recorded",
            "Test",
            DiagnosticSeverity.Warning,
            true );

        public ConcurrentQueue<Exception> Exceptions { get; } = new();

        public void ReportException( Exception exception, Action<Diagnostic> reportDiagnostic, bool canIgnoreException, out bool isHandled )
        {
            Assert.True( canIgnoreException );

            this.Exceptions.Enqueue( exception );
            reportDiagnostic( Diagnostic.Create( _descriptor, Location.None ) );
            isHandled = true;
        }
    }

    /// <summary>
    /// A telemetry context that records the exceptions reported to it.
    /// </summary>
    private sealed class RecordingTelemetryContext : ITelemetryContext
    {
        public ConcurrentQueue<Exception> Exceptions { get; } = new();

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;

        public ITelemetryPolicy Policy => throw new NotSupportedException();

        public IUsageSession StartUsageSession( string kind, string? projectName = null ) => throw new NotSupportedException();

        public void ReportException(
            Exception exception,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this.Exceptions.Enqueue( exception );

        public void ReportException(
            ClassifiedException classifiedException,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this.Exceptions.Enqueue( classifiedException.Exception );
    }

    /// <summary>
    /// A diagnostic sink of the introspection reference graph that records what it receives.
    /// </summary>
    private sealed class RecordingDiagnosticSink : IIntrospectionReferenceDiagnosticSink
    {
        public ConcurrentQueue<(ICompilation Compilation, ImmutableArray<Diagnostic> Diagnostics)> Reports { get; } = new();

        public void Report( ICompilation compilation, ImmutableArray<Diagnostic> diagnostics ) => this.Reports.Enqueue( (compilation, diagnostics) );
    }
}
