// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicObservablePropertyInfo : ObservablePropertyInfo
{
    public ClassicObservablePropertyInfo(
        IFieldOrProperty fieldOrProperty,
        ObservableTypeInfo declaringTypeInfo ) : base( fieldOrProperty, declaringTypeInfo ) { }

    public new ClassicObservableExpression RootReferenceNode => (ClassicObservableExpression) base.RootReferenceNode;
}