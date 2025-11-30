// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// A derivation of <see cref="CSharpSyntaxRewriter"/> that provides enhanced exception handling and stack overflow protection
/// when processing syntax nodes.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps exceptions thrown during node visitation with <see cref="SyntaxProcessingException"/>,
/// which includes detailed information about the syntax node being processed when the exception occurred.
/// This makes debugging weaver issues significantly easier.
/// </para>
/// <para>
/// It also includes a recursion guard to prevent <see cref="InsufficientExecutionStackException"/> when processing
/// deeply nested syntax trees.
/// </para>
/// <para>
/// Use this class instead of directly inheriting from <see cref="CSharpSyntaxRewriter"/> in your aspect weaver implementations.
/// Override <see cref="VisitCore"/> instead of <see cref="Visit"/> to add custom visiting logic.
/// </para>
/// </remarks>
/// <seealso cref="SafeSyntaxWalker"/>
/// <seealso cref="SafeSyntaxVisitor"/>
/// <seealso cref="SafeSyntaxVisitor{T}"/>
[PublicAPI]
public abstract class SafeSyntaxRewriter : CSharpSyntaxRewriter
{
    private RecursionGuard _recursionGuard;

    protected SafeSyntaxRewriter( bool visitIntoStructuredTrivia = false ) : base( visitIntoStructuredTrivia )
    {
        this._recursionGuard = new RecursionGuard( this );
    }

    public sealed override SyntaxNode? Visit( SyntaxNode? node )
    {
        try
        {
            this._recursionGuard.IncrementDepth();

            var result = this._recursionGuard.ShouldSwitch ? this._recursionGuard.Switch( node, this.VisitCore ) : this.VisitCore( node );

            this._recursionGuard.DecrementDepth();

            return result;
        }
        catch ( Exception e ) when ( SyntaxProcessingException.ShouldWrapException( e, node ) )
        {
            this._recursionGuard.Failed();

            throw new SyntaxProcessingException( e, node );
        }
    }

    protected virtual SyntaxNode? VisitCore( SyntaxNode? node )
    {
        return base.Visit( node );
    }
}