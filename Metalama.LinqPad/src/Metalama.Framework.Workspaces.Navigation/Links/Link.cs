// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// Names an object by a stable anchor and a path of steps from it, so that it can be resolved again without any state.
/// </summary>
/// <param name="Workspace">The key of the workspace, or <c>null</c> for the default workspace.</param>
/// <param name="Scope">The scope, <c>Project@tfm</c>, <c>Project</c>, or <see cref="WholeWorkspaceScope"/>.</param>
/// <param name="IsTransformed">Whether the anchor is resolved in the transformed code instead of the source code.</param>
/// <param name="Anchor">A declaration identifier, a type identifier, a collection name, or <c>query/…</c>.</param>
/// <param name="Steps">Property names, zero-based indexes, or key steps.</param>
/// <param name="Fingerprint">The fingerprint of the workspace when the link was produced. Formatted only for fragile links.</param>
internal sealed record Link( string? Workspace, string Scope, bool IsTransformed, string Anchor, ImmutableArray<string> Steps, string? Fingerprint )
{
    public const string Scheme = "metalama://";
    public const string WholeWorkspaceScope = "~";
    public const string QueryAnchorPrefix = "query/";

    /// <summary>
    /// Gets a value indicating whether the link depends on enumeration order or on re-running code, so that it is valid only
    /// for the workspace state given by <see cref="Fingerprint"/>.
    /// </summary>
    public bool IsFragile => this.IsQueryAnchor || this.Steps.Any( IsIndexStep );

    public bool IsQueryAnchor => this.Anchor.StartsWith( QueryAnchorPrefix, StringComparison.Ordinal );

    /// <summary>
    /// Gets a value indicating whether the anchor is a declaration, type or assembly identifier rather than a collection name.
    /// </summary>
    public bool IsIdentifierAnchor => IsIdentifier( this.Anchor );

    public Link WithStep( string step ) => this with { Steps = this.Steps.Add( step ) };

    public static bool IsIndexStep( string step ) => step.Length > 0 && step.All( char.IsAsciiDigit );

    /// <summary>
    /// Gets a value indicating whether a step is a key step, such as <c>aspects</c>. Key steps are lowercase, while property
    /// steps are the names of properties, which start with an uppercase letter.
    /// </summary>
    public static bool IsKeyStep( string step ) => step.Length > 0 && char.IsAsciiLetterLower( step[0] );

    public static bool IsIdentifier( string anchor )
    {
        var colon = anchor.IndexOf( ':', StringComparison.Ordinal );

        return colon > 0 && anchor.Take( colon ).All( char.IsAsciiLetter );
    }

    /// <summary>
    /// Formats the link as an absolute, percent-encoded URI.
    /// </summary>
    public string ToAbsoluteString()
    {
        var builder = new StringBuilder();
        builder.Append( Scheme );
        builder.Append( EscapeSegment( this.Workspace ?? "" ) );
        builder.Append( '/' );
        builder.Append( EscapeSegment( this.Scope ) );
        builder.Append( '/' );
        builder.Append( this.IsTransformed ? "transformed" : "source" );
        this.AppendPath( builder, true );
        this.AppendFingerprint( builder );

        return builder.ToString();
    }

    /// <summary>
    /// Formats the link relative to a base. The relative form is not percent-encoded, which keeps it readable.
    /// </summary>
    public string ToRelativeString( LinkBase? linkBase )
    {
        if ( linkBase == null || !string.Equals( linkBase.Workspace, this.Workspace, StringComparison.Ordinal ) )
        {
            return this.ToAbsoluteString();
        }

        var builder = new StringBuilder();

        if ( !string.Equals( linkBase.Scope, this.Scope, StringComparison.Ordinal ) || linkBase.IsTransformed != this.IsTransformed )
        {
            builder.Append( this.Scope );
            builder.Append( '/' );
            builder.Append( this.IsTransformed ? "transformed" : "source" );
        }

        this.AppendPath( builder, false );

        return builder.ToString().TrimStart( '/' ) + this.GetFingerprintSuffix();
    }

    public override string ToString() => this.ToAbsoluteString();

    // ImmutableArray compares by reference, so the steps are compared by value here.
    public bool Equals( Link? other )
        => other != null
           && string.Equals( this.Workspace, other.Workspace, StringComparison.Ordinal )
           && string.Equals( this.Scope, other.Scope, StringComparison.Ordinal )
           && this.IsTransformed == other.IsTransformed
           && string.Equals( this.Anchor, other.Anchor, StringComparison.Ordinal )
           && this.Steps.SequenceEqual( other.Steps, StringComparer.Ordinal )
           && string.Equals( this.Fingerprint, other.Fingerprint, StringComparison.Ordinal );

    public override int GetHashCode()
        => HashCode.Combine( this.Workspace, this.Scope, this.IsTransformed, this.Anchor, this.Steps.Length, this.Fingerprint );

    private void AppendPath( StringBuilder builder, bool escape )
    {
        builder.Append( '/' );
        builder.Append( escape ? EscapeSegment( this.Anchor, allowSlash: this.IsQueryAnchor ) : this.Anchor );

        foreach ( var step in this.Steps )
        {
            builder.Append( '/' );
            builder.Append( escape ? EscapeSegment( step ) : step );
        }
    }

    private void AppendFingerprint( StringBuilder builder ) => builder.Append( this.GetFingerprintSuffix() );

    private string GetFingerprintSuffix() => this.IsFragile && this.Fingerprint != null ? "?v=" + this.Fingerprint : "";

    /// <summary>
    /// Percent-encodes the characters that cannot appear in a URI path segment, and keeps the ones that documentation
    /// identifiers use, such as <c>:</c>, <c>(</c> and <c>,</c>, so that encoded links stay readable.
    /// </summary>
    internal static string EscapeSegment( string segment, bool allowSlash = false )
    {
        var builder = new StringBuilder( segment.Length );

        foreach ( var b in Encoding.UTF8.GetBytes( segment ) )
        {
            var c = (char) b;

            if ( b < 0x80 && (char.IsAsciiLetterOrDigit( c ) || "-._~!$&'()*+,;=:@".Contains( c, StringComparison.Ordinal ) || (allowSlash && c == '/')) )
            {
                builder.Append( c );
            }
            else
            {
                builder.Append( '%' );
                builder.Append( b.ToString( "X2", System.Globalization.CultureInfo.InvariantCulture ) );
            }
        }

        return builder.ToString();
    }
}
