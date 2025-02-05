// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// A derivation of <see cref="CSharpSyntaxVisitor{TResult}"/> that throws a <see cref="SyntaxProcessingException"/>
/// when an unhandled exception is detected while processing a node. 
/// </summary>
[PublicAPI]
public abstract class SafeSyntaxVisitor<T> : CSharpSyntaxVisitor<T>
{
    public sealed override T? Visit( SyntaxNode? node )
    {
        try
        {
            return this.VisitCore( node );
        }
        catch ( Exception e ) when ( SyntaxProcessingException.ShouldWrapException( e, node ) )
        {
            throw new SyntaxProcessingException( e, node );
        }
    }

    protected virtual T? VisitCore( SyntaxNode? node )
    {
        return base.Visit( node );
    }
}