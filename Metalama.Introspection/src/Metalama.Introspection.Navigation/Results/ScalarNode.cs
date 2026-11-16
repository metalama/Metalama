// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A value shown inline: null, a string, a number, a Boolean, an enum value, or another value shown with its string form.
/// </summary>
internal sealed class ScalarNode : ResultNode
{
    public ScalarNode( ScalarKind kind, string? text )
    {
        this.Kind = kind;
        this.Text = text;
    }

    public ScalarKind Kind { get; }

    public string? Text { get; }
}
