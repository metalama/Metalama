// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// A derivation of <see cref="CSharpSyntaxVisitor"/> that provides enhanced exception handling
/// when visiting syntax nodes without returning a result.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps exceptions thrown during node visitation with <see cref="SyntaxProcessingException"/>,
/// which includes detailed information about the syntax node being processed when the exception occurred.
/// This makes debugging weaver issues significantly easier.
/// </para>
/// <para>
/// Use this class instead of directly inheriting from <see cref="CSharpSyntaxVisitor"/> in your aspect weaver implementations.
/// Override <see cref="VisitCore"/> instead of <see cref="Visit"/> to add custom visiting logic.
/// </para>
/// </remarks>
/// <seealso cref="SafeSyntaxVisitor{T}"/>
/// <seealso cref="SafeSyntaxRewriter"/>
/// <seealso cref="SafeSyntaxWalker"/>
[PublicAPI]
public abstract class SafeSyntaxVisitor : CSharpSyntaxVisitor
{
    public sealed override void Visit( SyntaxNode? node )
    {
        try
        {
            this.VisitCore( node );
        }
        catch ( Exception e ) when ( SyntaxProcessingException.ShouldWrapException( e, node ) )
        {
            throw new SyntaxProcessingException( e, node );
        }
    }

    protected virtual void VisitCore( SyntaxNode? node )
    {
        base.Visit( node );
    }
}