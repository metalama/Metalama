// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Represents a method of substituting a syntax tree node with another while allowing for recursive replacements.
/// </summary>
internal abstract class SyntaxNodeSubstitution
{
    protected CompilationContext CompilationContext { get; }

    protected SyntaxNodeSubstitution( CompilationContext compilationContext )
    {
        this.CompilationContext = compilationContext;
    }

    /// <summary>
    /// Gets a node that was initially marked for replacement.
    /// </summary>
    public abstract SyntaxNode ReplacedNode { get; }

    /// <summary>
    /// Replaces the current node, which is a result of recursive replacements.
    /// </summary>
    /// <param name="currentNode">Current node. This can be the original node, or node that was created by recursively applying other replacers.</param>
    /// <param name="substitutionContext">Current syntax generation context.</param>
    /// <returns>A new node after applying this replacer.</returns>
    /// <remarks>The received node is detached and thus without SemanticModel. The algorithm must work based solely on syntax.</remarks>
    public abstract SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext );
}