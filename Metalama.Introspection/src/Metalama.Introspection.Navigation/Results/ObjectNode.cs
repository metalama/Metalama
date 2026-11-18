// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Introspection.Navigation.Results;

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
