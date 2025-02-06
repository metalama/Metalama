// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

/// <summary>
/// Represents a property in an observability dependency graph.
/// </summary>
[CompileTime]
internal class ObservablePropertyInfo
{
    /// <summary>
    /// Gets the parent node, representing the declaring type.
    /// </summary>
    public ObservableTypeInfo DeclaringTypeInfo { get; }

    /// <summary>
    /// Gets the corresponding <see cref="IFieldOrProperty"/>.
    /// </summary>
    public IFieldOrProperty FieldOrProperty { get; }

    /// <summary>
    /// Gets the name of the <see cref="FieldOrProperty"/>.
    /// </summary>
    public string Name => this.FieldOrProperty.Name;

    /// <summary>
    /// Gets the root <see cref="ObservableExpression"/>, i.e. the reference node referencing the current property.
    /// </summary>
    public ObservableExpression RootReferenceNode { get; }

    public ObservablePropertyInfo( IFieldOrProperty fieldOrProperty, ObservableTypeInfo declaringTypeInfo )
    {
        this.FieldOrProperty = fieldOrProperty;
        this.DeclaringTypeInfo = declaringTypeInfo;
        this.RootReferenceNode = declaringTypeInfo.Builder.CreateExpression( this, null );
    }

    // ReSharper disable once RedundantSuppressNullableWarningExpression
    public override string ToString() => this.FieldOrProperty.ToString()!;
}