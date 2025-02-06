// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicObservableTypeInfo : ObservableTypeInfo
{
    public ClassicObservableTypeInfo(
        ClassicDependencyGraphBuilder builder,
        INamedType type ) : base( builder, type )
    {
        this.InpcInstrumentationKind = builder.Context.GetInpcInstrumentationKind( type );
    }

    public InpcInstrumentationKind InpcInstrumentationKind { get; }

    public new IEnumerable<ClassicObservableExpression> AllExpressions => base.AllExpressions.Cast<ClassicObservableExpression>();

    public new IEnumerable<ClassicObservablePropertyInfo> Properties => base.Properties.Cast<ClassicObservablePropertyInfo>();
}