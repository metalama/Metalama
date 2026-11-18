// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A <see cref="System.FormattableString"/>, such as a diagnostic message, whose object arguments are kept as nodes.
/// </summary>
internal sealed class RichTextNode : ResultNode
{
    public RichTextNode( string text, ImmutableArray<RichTextSegment> segments )
    {
        this.Text = text;
        this.Segments = segments;
    }

    public string Text { get; }

    public ImmutableArray<RichTextSegment> Segments { get; }
}
