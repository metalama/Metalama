// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces.Navigation.Links;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces.Navigation.Results;

/// <summary>
/// A node of a result tree: a value summarized for display, independently of the output format.
/// </summary>
internal abstract class ResultNode
{
    /// <summary>
    /// Gets the link that resolves the value again, or <c>null</c> when the value has none.
    /// </summary>
    public Link? Link { get; init; }
}

internal enum ScalarKind
{
    Null,
    String,
    Number,
    Boolean,
    Enum,
    Severity,
    Other
}

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

/// <summary>
/// An object, either collapsed to its summary (<see cref="Properties"/> is <c>null</c>) or expanded to its properties.
/// </summary>
internal sealed class ObjectNode : ResultNode
{
    public ObjectNode( string typeName, string display )
    {
        this.TypeName = typeName;
        this.Display = display;
    }

    public string TypeName { get; }

    public string Display { get; }

    /// <summary>
    /// Gets the source location, as <c>path:line</c> relative to the workspace root.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the name of the public interface used to type the C# expression that resolves the object, such as <c>IMethod</c>.
    /// </summary>
    public string? InterfaceName { get; init; }

    public bool IsDeclaration { get; init; }

    public bool IsTransformed { get; init; }

    public ImmutableArray<PropertyNode>? Properties { get; init; }
}

internal sealed record PropertyNode( string Name, ResultNode Value );

/// <summary>
/// A collection, either collapsed to its count (<see cref="Items"/> is <c>null</c>) or expanded to its first items.
/// </summary>
internal sealed class CollectionNode : ResultNode
{
    public CollectionNode( string typeName, int? count )
    {
        this.TypeName = typeName;
        this.Count = count;
    }

    public string TypeName { get; }

    /// <summary>
    /// Gets the number of items, or <c>null</c> when it is unknown because the collection has no <c>Count</c> or <c>Length</c>
    /// and was not enumerated to the end.
    /// </summary>
    public int? Count { get; }

    public ImmutableArray<ResultNode>? Items { get; init; }

    /// <summary>
    /// Gets a value indicating whether the collection has more items than <see cref="Items"/>.
    /// </summary>
    public bool IsTruncated { get; init; }
}

/// <summary>
/// A group of a <c>GroupBy</c>.
/// </summary>
internal sealed class GroupingNode : ResultNode
{
    public GroupingNode( ResultNode key, CollectionNode items )
    {
        this.Key = key;
        this.Items = items;
    }

    public ResultNode Key { get; }

    public CollectionNode Items { get; }
}

/// <summary>
/// An exception thrown while getting a value. Rendering continues around it.
/// </summary>
internal sealed class ErrorNode : ResultNode
{
    public ErrorNode( string exceptionType, string message )
    {
        this.ExceptionType = exceptionType;
        this.Message = message;
    }

    public string ExceptionType { get; }

    public string Message { get; }
}

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

/// <summary>
/// A segment of a <see cref="RichTextNode"/>: either literal text or an argument.
/// </summary>
internal sealed record RichTextSegment( string? Text, ResultNode? Argument );
