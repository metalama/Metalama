// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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