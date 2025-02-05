// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Diagnostics;

/// <summary>
/// Represents the suppression of a diagnostic of a given id in a given scope, possibly with a filter.
/// </summary>
public sealed class ScopedSuppression : IScopedSuppression
{
    public ISuppression Suppression { get; }

    public ISymbol GetScopeSymbolOrNull( CompilationContext compilationContext ) => this.ScopeSymbol;

    public ISymbol ScopeSymbol { get; }

    internal ScopedSuppression( ISuppression suppression, ISymbol symbol )
    {
        this.Suppression = suppression;
        this.ScopeSymbol = symbol;
    }

    public override string ToString() => $"{this.Suppression} on {this.ScopeSymbol}";

    public bool Matches( Diagnostic diagnostic, Compilation compilation, Func<Func<bool>, bool> codeInvoker )
    {
        if ( diagnostic.Id != this.Suppression.Definition.SuppressedDiagnosticId )
        {
            return false;
        }

        var symbolId = this.ScopeSymbol.GetSerializableId();

        return this.Matches( diagnostic, compilation, codeInvoker, symbolId );
    }

    internal bool Matches( Diagnostic diagnostic, Compilation compilation, Func<Func<bool>, bool> codeInvoker, SerializableDeclarationId declarationId )
    {
        if ( diagnostic.Id != this.Suppression.Definition.SuppressedDiagnosticId )
        {
            return false;
        }

        var location = diagnostic.Location;

        if ( location.SourceTree == null )
        {
            return false;
        }

        var node = location.SourceTree.GetRoot().FindNode( location.SourceSpan ).FindSymbolDeclaringNode();

        if ( node == null )
        {
            return false;
        }

        if ( !compilation.ContainsSyntaxTree( location.SourceTree ) )
        {
            return false;
        }

        var diagnosticSymbol = compilation.GetCachedSemanticModel( location.SourceTree ).GetDeclaredSymbol( node );

        while ( diagnosticSymbol != null )
        {
            if ( diagnosticSymbol.TryGetSerializableId( out var id ) && declarationId.Equals( id ) )
            {
                break;
            }

            diagnosticSymbol = diagnosticSymbol.ContainingSymbol;
        }

        if ( diagnosticSymbol == null )
        {
            return false;
        }

        if ( this.Suppression.Filter is { } filter )
        {
            var filterPassed = codeInvoker( () => filter( SuppressionFactories.CreateDiagnostic( diagnostic ) ) );

            if ( !filterPassed )
            {
                return false;
            }
        }

        return true;
    }
}