// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Options;

/// <summary>
/// Context of an <see cref="IIncrementalObject.ApplyChanges"/> operation.
/// </summary>
/// <remarks>
/// <para>
/// This structure provides contextual information when options are being merged. The <see cref="Axis"/> property indicates
/// the dimension along which merging is occurring (such as inheritance from base types, containing types, or namespaces),
/// while the <see cref="Declaration"/> property identifies the target declaration for which options are being resolved.
/// </para>
/// <para>
/// Most implementations of <see cref="IIncrementalObject.ApplyChanges"/> can ignore the context and simply merge properties
/// where the <paramref name="changes"/> parameter wins over the current instance. However, advanced scenarios may use the
/// <see cref="Axis"/> to apply different merging logic based on the inheritance dimension.
/// </para>
/// </remarks>
/// <seealso cref="IIncrementalObject.ApplyChanges"/>
/// <seealso cref="ApplyChangesAxis"/>
/// <seealso href="@configuration-custom-merge"/>
[CompileTime]
public readonly struct ApplyChangesContext
{
    /// <summary>
    /// Gets the axis along which the merge operation is performed.
    /// </summary>
    /// <value>
    /// The axis indicating how options are being combined, such as merging options from the same declaration,
    /// from a base declaration, from a containing declaration, or from an aspect.
    /// </value>
    public ApplyChangesAxis Axis { get; }

    /// <summary>
    /// Gets the declaration for which the merge operation is performed.
    /// </summary>
    /// <value>
    /// The target declaration that is receiving the merged options.
    /// </value>
    public IDeclaration Declaration { get; }

    internal ApplyChangesContext( ApplyChangesAxis axis, IDeclaration declaration )
    {
        this.Axis = axis;
        this.Declaration = declaration;
    }
}