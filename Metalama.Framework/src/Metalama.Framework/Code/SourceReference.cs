// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using System;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a syntax node in source code. Using <c>Metalama.Framework.Sdk</c> you can use <c>ToSyntaxNodeOrToken</c> to convert it to a Roslyn object.
/// </summary>
[CompileTime]
[PublicAPI]
public readonly struct SourceReference : IDiagnosticLocation
{
    private readonly ISourceReferenceImpl _sourceReferenceImpl;

    public SourceReference( object nodeOrToken, ISourceReferenceImpl sourceReferenceImpl )
    {
        this.NodeOrTokenInternal = nodeOrToken;
        this._sourceReferenceImpl = sourceReferenceImpl;
    }

    /// <summary>
    /// Gets the Roslyn <c>SyntaxNode</c>, <c>SyntaxToken</c>.
    /// This property can be used by SDK-based plugins. 
    /// </summary>
    [Obsolete( "Use ToSyntaxNodeOrToken() from Metalama.Framework.Sdk." )]
    public object NodeOrToken => this.NodeOrTokenInternal;

    internal object NodeOrTokenInternal { get; }

    /// <summary>
    /// Gets the <c>SyntaxKind</c> of the node or token.
    /// </summary>
    public string Kind => this._sourceReferenceImpl.GetKind( this );

    /// <summary>
    /// Gets a value indicating whether the current syntax node is contains the implementation of the declaration.
    /// This property evaluates to <c>false</c> only for partial methods without implementation.
    /// </summary>
    public bool IsImplementationPart => this._sourceReferenceImpl.IsImplementationPart( this );

    /// <summary>
    /// Gets source file, line and column for the node.
    /// </summary>
    public SourceSpan Span => this._sourceReferenceImpl.GetSourceSpan( this );

    /// <summary>
    /// Gets the text representation (i.e. the source code) of the current syntax node or token.
    /// </summary>
    /// <param name="normalized"><c>true</c> if whitespace should be normalized, <c>false</c> if the original formatting should be preserved.</param>
    /// <returns>The source code of the current syntax node.</returns>
    public string GetText( bool normalized = false ) => this._sourceReferenceImpl.GetText( this, normalized );

    /// <summary>
    /// Gets the content of the node or token (without trivia).
    /// </summary>
    public override string ToString() => this.Span.ToString();
}