// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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