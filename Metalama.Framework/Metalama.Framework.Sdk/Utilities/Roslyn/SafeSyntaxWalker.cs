// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// A derivation of <see cref="CSharpSyntaxWalker"/> that throws a <see cref="SyntaxProcessingException"/>
/// when an unhandled exception is detected while processing a node.
/// Also prevents <see cref="InsufficientExecutionStackException "/> for deeply nested trees.
/// </summary>
[PublicAPI]
public abstract class SafeSyntaxWalker : CSharpSyntaxWalker
{
    private RecursionGuard _recursionGuard;

    protected SafeSyntaxWalker( SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node ) : base( depth )
    {
        this._recursionGuard = new RecursionGuard( this );
    }

    public sealed override void Visit( SyntaxNode? node )
    {
        try
        {
            this._recursionGuard.IncrementDepth();

            if ( this._recursionGuard.ShouldSwitch )
            {
                this._recursionGuard.Switch( node, this.VisitCore );
            }
            else
            {
                this.VisitCore( node );
            }

            this._recursionGuard.DecrementDepth();
        }
        catch ( Exception e ) when ( SyntaxProcessingException.ShouldWrapException( e, node ) )
        {
            this._recursionGuard.Failed();

            throw new SyntaxProcessingException( e, node );
        }
    }

    protected virtual void VisitCore( SyntaxNode? node )
    {
        base.Visit( node );
    }
}