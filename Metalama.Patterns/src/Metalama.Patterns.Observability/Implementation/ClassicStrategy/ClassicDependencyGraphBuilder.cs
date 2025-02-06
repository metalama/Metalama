// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicDependencyGraphBuilder : DependencyGraphBuilder
{
    public ClassicGraphBuildingContext Context { get; }

    public INamedType CurrentType { get; }

    public ClassicDependencyGraphBuilder( ClassicGraphBuildingContext context, INamedType currentType ) : base( context.Assets )
    {
        this.Context = context;
        this.CurrentType = currentType;
    }

    protected override ObservableTypeInfo CreateTypeInfo( INamedType type ) => new ClassicObservableTypeInfo( this, type );

    public override ObservablePropertyInfo CreatePropertyInfo( IFieldOrProperty fieldOrProperty, ObservableTypeInfo parent )
        => new ClassicObservablePropertyInfo( fieldOrProperty, parent );

    public override ObservableExpression CreateExpression(
        ObservablePropertyInfo propertyInfo,
        ObservableExpression? parent )
        => new ClassicObservableExpression( propertyInfo, parent, this );
}