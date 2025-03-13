// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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