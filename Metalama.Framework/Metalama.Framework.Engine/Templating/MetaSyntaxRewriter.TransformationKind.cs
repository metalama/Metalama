// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating;

internal partial class MetaSyntaxRewriter
{
    /// <summary>
    /// Specifies how a <see cref="SyntaxNode"/> must be transformed.
    /// </summary>
    protected enum TransformationKind
    {
        /// <summary>
        /// No transformation. The original node is returned.
        /// </summary>
        None,

        /// <summary>
        /// The original node is cloned. This kind of transformation is currently only used
        /// to validate that the generated code is correct.
        /// </summary>
        [UsedImplicitly]
        Clone,

        /// <summary>
        /// The original node is transformed, i.e. the <c>Visit</c> method returns
        /// an expression that evaluates to an instance equivalent to the source one.
        /// </summary>
        Transform
    }
}