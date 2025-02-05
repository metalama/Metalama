// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Annotates syntax nodes with <see cref="Location"/> annotations that can then be resolved by <see cref="ILocationAnnotationMap"/>.
    /// </summary>
    internal interface ILocationAnnotationMapBuilder : ILocationAnnotationMap
    {
        SyntaxNode AddLocationAnnotation( SyntaxNode originalNode, SyntaxNode transformedNode );

        SyntaxToken AddLocationAnnotation( SyntaxToken originalToken );
    }
}