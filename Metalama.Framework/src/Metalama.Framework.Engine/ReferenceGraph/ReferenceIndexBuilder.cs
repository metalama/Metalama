// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpCrafters.Backstage.Diagnostics;
using SharpCrafters.Backstage.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// The base class of the builders of reference indexes, which receive the references found by <see cref="ReferenceIndexWalker"/>.
/// </summary>
/// <remarks>
/// <para>
/// When an exception occurs while the walker processes a syntax node, the walker skips the node and calls
/// <see cref="ReportSkippedNode"/>, so that one defect does not discard the whole index. The builder reports each distinct
/// exception once, and adds the diagnostic LAMA0094 to <see cref="Diagnostics"/> for each skipped node.
/// </para>
/// <para>
/// The exception is reported to the reporting channel that the host of the project registered before the project was processed.
/// In the compiler, it is <see cref="ICompileTimeExceptionHandler"/>, which writes the crash report, reports the exception to
/// telemetry, and reports the diagnostic LAMA0049. In a workspace of the introspection API, it is the
/// <see cref="ITelemetryContext"/> of the workspace. When neither is registered, the exception is only written to the log.
/// </para>
/// <para>
/// The builder does not know where diagnostics must be reported. The code that creates the builder reads
/// <see cref="Diagnostics"/> and reports them where it can.
/// </para>
/// </remarks>
public abstract class ReferenceIndexBuilder
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Diagnostic> _diagnostics = new();
    private readonly ConcurrentDictionary<string, bool> _reportedExceptions = new( StringComparer.Ordinal );
    private int _skippedNodeCount;

    private protected ReferenceIndexBuilder( ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "ReferenceIndex" );
    }

    /// <summary>
    /// Gets the service provider of the project whose references are indexed.
    /// </summary>
    private protected ProjectServiceProvider ServiceProvider => this._serviceProvider;

    /// <summary>
    /// Gets the diagnostics that report the syntax nodes that the walker skipped because of an exception.
    /// </summary>
    internal IReadOnlyCollection<Diagnostic> Diagnostics => this._diagnostics;

    /// <summary>
    /// Gets the number of syntax nodes that the walker skipped because of an exception. When the number is not zero, the index
    /// is incomplete.
    /// </summary>
    internal int SkippedNodeCount => Volatile.Read( ref this._skippedNodeCount );

    /// <summary>
    /// Records that the walker skipped a syntax node because an exception occurred while the node was processed.
    /// </summary>
    /// <remarks>
    /// This method must not throw, because it is called while an exception is handled. It can be called concurrently.
    /// </remarks>
    internal void ReportSkippedNode( SyntaxNode node, Exception exception )
    {
        Interlocked.Increment( ref this._skippedNodeCount );

        try
        {
            var reportedException = SyntaxProcessingException.ShouldWrapException( exception, node )
                ? new SyntaxProcessingException( exception, node )
                : exception;

            this._logger.Warning?.Log( $"The reference indexer skipped a node because of an exception: {reportedException}" );

            // A defect usually fails on every node of the same shape. The exception is reported once per distinct message, so that
            // the crash reports and the telemetry are not flooded.
            if ( this._reportedExceptions.TryAdd( $"{exception.GetType().FullName}: {exception.Message}", true ) )
            {
                if ( this._serviceProvider.GetService<ICompileTimeExceptionHandler>() is { } exceptionHandler )
                {
                    exceptionHandler.ReportException( reportedException, this._diagnostics.Enqueue, canIgnoreException: true, out _ );
                }
                else
                {
                    // Resolved untyped, because ITelemetryContext is a type of Backstage and not a service of the engine. The host
                    // opens the context before the project is processed and registers it, as the compiler does.
                    ((ITelemetryContext?) this._serviceProvider.Underlying.GetService( typeof(ITelemetryContext) ))?.ReportException( reportedException );
                }
            }

            this._diagnostics.Enqueue(
                GeneralDiagnosticDescriptors.ReferenceIndexerSkippedNode.CreateRoslynDiagnostic(
                    node.GetLocation(),
                    (node.Kind().ToString(), exception.Message) ) );
        }
        catch ( Exception reportingException )
        {
            this._logger.Error?.Log( $"Cannot report the exception of the reference indexer: {reportingException}" );
        }
    }

    internal void AddReference( ISymbol? referencedSymbol, ISymbol? referencingSymbol, SyntaxNodeOrToken node, ReferenceKinds referenceKind )
    {
        if ( referencedSymbol == null || referencingSymbol == null )
        {
            return;
        }

        // A classic extension method called in reduced form, for instance x.M(), is bound to a reduced method symbol, which is a different
        // symbol from the static method. The reference is keyed by the static method so that it is found whichever form the call uses.
        if ( referencedSymbol.Kind == SymbolKind.Method
             && referencedSymbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension, ReducedFrom: { } unreducedMethod } )
        {
            referencedSymbol = unreducedMethod;
        }

        referencedSymbol = referencedSymbol.OriginalDefinition;
        referencingSymbol = referencingSymbol.OriginalDefinition;

        if ( !CheckSymbolKind( referencedSymbol ) || !CheckSymbolKind( referencingSymbol ) )
        {
            return;
        }

        this.AddReferenceCore( referencedSymbol, referencingSymbol, node, referenceKind );
    }

    protected abstract void AddReferenceCore( ISymbol referencedSymbol, ISymbol referencingSymbol, SyntaxNodeOrToken node, ReferenceKinds referenceKind );

    private static bool CheckSymbolKind( ISymbol symbol )
    {
        switch ( symbol.Kind )
        {
            case SymbolKind.Local:
            case SymbolKind.Alias:
            case SymbolKind.Label:
            case SymbolKind.Preprocessing:
            case SymbolKind.DynamicType:
                return false;

            // An error type is the result of a name that the compiler could not bind. It is not a declaration, so no query can
            // find a reference to it, and unrelated error types can be equal under a structural comparison.
            case SymbolKind.ErrorType:
                return false;
        }

        if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } )
        {
            return false;
        }

        return true;
    }
}