// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Exposes a method that allows to resolve the original <see cref="Location"/> of a transformed node based on
    /// annotations.
    /// </summary>
    internal interface ILocationAnnotationMap
    {
        Location? GetLocation( SyntaxNodeOrToken node );
    }
}