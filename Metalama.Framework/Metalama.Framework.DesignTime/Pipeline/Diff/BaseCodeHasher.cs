// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using K4os.Hash.xxHash;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Text;

namespace Metalama.Framework.DesignTime.Pipeline.Diff;

/// <summary>
/// Base for the auto-generated <see cref="RunTimeCodeHasher"/> and <see cref="CompileTimeCodeHasher"/>.
/// Generates a hash that is unique enough under the desired invariants.
/// </summary>
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
public abstract class BaseCodeHasher : SafeSyntaxWalker
{
    private readonly XXH64 _hasher;

    internal StringBuilder? Log { get; private set; }

    internal void EnableLogging() => this.Log = new StringBuilder();

    protected BaseCodeHasher( XXH64 hasher )
    {
        this._hasher = hasher;
    }

    protected void VisitTrivialToken( SyntaxToken token )
    {
        if ( token.RawKind != 0 && !token.IsMissing )
        {
            this._hasher.Update( token.RawKind );
            this.Log?.AppendLineInvariant( $"Adding '{token.RawKind}' to the hash." );
        }
    }

    protected void VisitNonTrivialToken( SyntaxToken token )
    {
        if ( !token.IsMissing )
        {
            this._hasher.Update( token.Text );
            this.Log?.AppendLineInvariant( $"Adding '{token.Text}' to the hash." );
        }
    }

    protected void HashValue<T>( T value )
        where T : unmanaged
    {
        this._hasher.Update( value );
        this.Log?.AppendLineInvariant( $"Adding '{value}' to the hash." );
    }

    protected void Visit<T>( in SyntaxList<T> list )
        where T : SyntaxNode
    {
        foreach ( var item in list )
        {
            this.Visit( item );
        }
    }

    protected void Visit<T>( in SeparatedSyntaxList<T> list )
        where T : SyntaxNode
    {
        foreach ( var item in list )
        {
            this.Visit( item );
        }
    }

    protected void Visit( in SyntaxToken token )
    {
        this._hasher.Update( token.Text );
        this.Log?.AppendLineInvariant( $"Adding '{token.Text}' to the hash." );
    }

    protected void Visit( in SyntaxTokenList list )
    {
        foreach ( var item in list )
        {
            this.Visit( item );
        }
    }
}