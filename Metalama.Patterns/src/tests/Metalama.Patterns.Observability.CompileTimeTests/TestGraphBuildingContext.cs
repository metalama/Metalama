// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Patterns.Observability.Configuration;
using Metalama.Patterns.Observability.Implementation;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;
using Microsoft.CodeAnalysis;

namespace Metalama.Patterns.Observability.CompileTimeTests;

internal sealed class TestGraphBuildingContext : GraphBuildingContext
{
    private readonly Func<ISymbol, bool>? _isConfiguredAsSafe;
    private readonly Action<string>? _reportDiagnostic;
    private readonly Func<ITypeSymbol, bool>? _treatAsImplementingInpc;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestGraphBuildingContext"/> class.
    /// </summary>
    /// <param name="isConfiguredAsSafe">If <see langword="null"/>, <see cref="GraphBuildingContext.CanIgnoreUnobservableExpressions"/> will always return <see langword="false"/>.</param>
    /// <param name="reportDiagnostic"></param>
    /// <param name="treatAsImplementingInpc">If <see langword="null"/>, <see cref="GraphBuildingContext.TreatAsImplementingInpc(ITypeSymbol)"/> will always return <see langword="false"/>.</param>
    /// <param name="isAutoPropertyOrField">If <see langword="null"/>, <see cref="GraphBuildingContext.IsAutoPropertyOrField(ISymbol)"/> will return <see langword="true"/> for any field or property.</param>
    public TestGraphBuildingContext(
        ICompilation compilation,
        Assets assets,
        Func<ISymbol, bool>? isConfiguredAsSafe = null,
        Action<string>? reportDiagnostic = null,
        Func<ITypeSymbol, bool>? treatAsImplementingInpc = null ) : base( compilation, assets )
    {
        this._isConfiguredAsSafe = isConfiguredAsSafe;
        this._reportDiagnostic = reportDiagnostic;
        this._treatAsImplementingInpc = treatAsImplementingInpc;
    }

    public override bool CanIgnoreUnobservableExpressions( ISymbol symbol )
        => this._isConfiguredAsSafe?.Invoke( symbol ) ?? base.CanIgnoreUnobservableExpressions( symbol );

    public override void ReportDiagnostic( IDiagnostic diagnostic, Location? location = null )
    {
        var formattedDiagnostic = diagnostic.Definition.Id;

        if ( location != null )
        {
            formattedDiagnostic += "@'" + location.SourceTree!.GetText().GetSubText( location.SourceSpan ) + "'";
        }

        this._reportDiagnostic?.Invoke( formattedDiagnostic );
    }

    public override bool TreatAsImplementingInpc( ITypeSymbol type ) => this._treatAsImplementingInpc?.Invoke( type ) ?? false;

    protected override DependencyAnalysisOptions GetDependencyAnalysisOptions( ISymbol symbol ) => DependencyAnalysisOptions.Default;
}