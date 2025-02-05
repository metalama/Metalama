// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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