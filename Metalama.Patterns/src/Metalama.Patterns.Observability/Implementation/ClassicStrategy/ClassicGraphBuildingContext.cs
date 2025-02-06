// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;
using Microsoft.CodeAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicGraphBuildingContext : GraphBuildingContext
{
    private readonly ClassicObservabilityStrategyImpl _strategy;

    public ClassicGraphBuildingContext( ClassicObservabilityStrategyImpl strategy ) : base( strategy.CurrentType.Compilation, strategy.Assets )
    {
        this._strategy = strategy;
    }

    public bool HasReportedErrors { get; private set; }

    public override void ReportDiagnostic( IDiagnostic diagnostic, Location? location = null )
    {
        this.HasReportedErrors |= diagnostic.Definition.Severity == Severity.Error;
        this._strategy.AspectBuilder.Diagnostics.Report( diagnostic, location.ToDiagnosticLocation() );
    }

    public override bool TreatAsImplementingInpc( ITypeSymbol type )
    {
        var typeDecl = this._strategy.CurrentType.Compilation.GetDeclaration( type ) as IType;

        return typeDecl != null && this._strategy.InpcInstrumentationKindLookup.Get( typeDecl ) != InpcInstrumentationKind.None;
    }

    public bool HasInheritedOnChildPropertyChangedPropertyPath( string dottedPropertyPath )
        => this._strategy.HasInheritedOnChildPropertyChangedPropertyPath( dottedPropertyPath );

    public bool HasInheritedOnObservablePropertyChangedProperty( string dottedPropertyPath )
        => this._strategy.HasInheritedOnObservablePropertyChangedProperty( dottedPropertyPath );

    public InpcInstrumentationKind GetInpcInstrumentationKind( INamedType type ) => this._strategy.GetInpcInstrumentationKind( type );
}