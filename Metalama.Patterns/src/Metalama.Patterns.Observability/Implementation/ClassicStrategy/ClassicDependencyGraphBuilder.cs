// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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