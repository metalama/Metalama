// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Observers;

namespace Metalama.Framework.Tests.Benchmarks;

internal sealed class TemplatingCodeValidatorObserver : ITemplatingCodeValidatorObserver
{
    private int _semanticModelUsedCount;
    private int _symbolClassifierUsedNormalCount;
    private int _symbolClassifierUsedQuickCount;
    private int _syntaxTreesValidatedCount;
    private int _syntaxTreesSkippedCount;

    public int SemanticModelUsedCount => this._semanticModelUsedCount;

    public int SymbolClassifierUsedCount => this._symbolClassifierUsedNormalCount + this._symbolClassifierUsedQuickCount;

    public int SymbolClassifierUsedNormalCount => this._symbolClassifierUsedNormalCount;

    public int SymbolClassifierUsedQuickCount => this._symbolClassifierUsedQuickCount;

    public int SyntaxTreesValidatedCount => this._syntaxTreesValidatedCount;

    public int SyntaxTreesSkippedCount => this._syntaxTreesSkippedCount;

    public void OnSemanticModelUsed()
    {
        Interlocked.Increment( ref this._semanticModelUsedCount );
    }

    public void OnSymbolClassifierUsed( bool isQuickMode )
    {
        if ( isQuickMode )
        {
            Interlocked.Increment( ref this._symbolClassifierUsedQuickCount );
        }
        else
        {
            Interlocked.Increment( ref this._symbolClassifierUsedNormalCount );
        }
    }

    public void OnSyntaxTreeValidated()
    {
        Interlocked.Increment( ref this._syntaxTreesValidatedCount );
    }

    public void OnSyntaxTreeSkipped()
    {
        Interlocked.Increment( ref this._syntaxTreesSkippedCount );
    }

    public void Reset()
    {
        this._semanticModelUsedCount = 0;
        this._symbolClassifierUsedNormalCount = 0;
        this._symbolClassifierUsedQuickCount = 0;
        this._syntaxTreesValidatedCount = 0;
        this._syntaxTreesSkippedCount = 0;
    }

    public void PrintMetrics()
    {
        Console.WriteLine( $"Syntax trees: {this._syntaxTreesValidatedCount:N0} validated, {this._syntaxTreesSkippedCount:N0} skipped" );
        Console.WriteLine( $"Semantic model calls: {this._semanticModelUsedCount:N0}" );

        Console.WriteLine(
            $"Symbol classifier calls: {this.SymbolClassifierUsedCount:N0} (normal: {this._symbolClassifierUsedNormalCount:N0}, quick: {this._symbolClassifierUsedQuickCount:N0})" );
    }
}