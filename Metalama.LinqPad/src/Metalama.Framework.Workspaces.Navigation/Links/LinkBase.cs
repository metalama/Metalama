// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Text;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// The base against which relative links are formatted and parsed, as <c>&lt;base href&gt;</c> in HTML.
/// </summary>
/// <param name="Workspace">The key of the workspace.</param>
/// <param name="Scope">The default scope, or <c>null</c> when the workspace has several projects or target frameworks.</param>
/// <param name="IsTransformed">Whether the default code version is the transformed code.</param>
internal sealed record LinkBase( string? Workspace, string? Scope, bool IsTransformed = false )
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append( Link.Scheme );
        builder.Append( Link.EscapeSegment( this.Workspace ?? "" ) );
        builder.Append( '/' );

        if ( this.Scope != null )
        {
            builder.Append( Link.EscapeSegment( this.Scope ) );
            builder.Append( '/' );
            builder.Append( this.IsTransformed ? "transformed" : "source" );
            builder.Append( '/' );
        }

        return builder.ToString();
    }
}
