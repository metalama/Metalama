// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// A derivation of <see cref="CSharpSyntaxVisitor{TResult}"/> that provides enhanced exception handling
/// when visiting syntax nodes with a result value.
/// </summary>
/// <typeparam name="T">The type of value returned by the visitor methods.</typeparam>
/// <remarks>
/// <para>
/// This class wraps exceptions thrown during node visitation with <see cref="SyntaxProcessingException"/>,
/// which includes detailed information about the syntax node being processed when the exception occurred.
/// This makes debugging weaver issues significantly easier.
/// </para>
/// <para>
/// Use this class instead of directly inheriting from <see cref="CSharpSyntaxVisitor{TResult}"/> in your aspect weaver implementations.
/// Override <see cref="VisitCore"/> instead of <see cref="Visit"/> to add custom visiting logic.
/// </para>
/// </remarks>
/// <seealso cref="SafeSyntaxVisitor"/>
/// <seealso cref="SafeSyntaxRewriter"/>
/// <seealso cref="SafeSyntaxWalker"/>
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