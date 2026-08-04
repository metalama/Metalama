// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// An object discovered by an <see cref="ObjectGraphWalker"/>, together with the reference through which it was
/// discovered.
/// </summary>
/// <remarks>
/// A node holds its <see cref="Parent"/>, so the chain of references that leads to it can be reconstructed after the
/// walk without the walker having to maintain a separate map. Because the walk is breadth-first, the chain recorded
/// for an object is the shortest one.
/// </remarks>
internal sealed class ObjectGraphNode
{
    /// <summary>
    /// Gets the object itself.
    /// </summary>
    public object Object { get; }

    /// <summary>
    /// Gets the name of the field, the index of the array element, or the description of the conditional-weak-table
    /// entry through which <see cref="Object"/> was discovered. For a root, this is the name given by the caller.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the node from which <see cref="Object"/> was discovered, or <c>null</c> when the object is a root.
    /// </summary>
    public ObjectGraphNode? Parent { get; }

    /// <summary>
    /// Gets a value indicating whether the incoming reference is held through a
    /// <see cref="ConditionalWeakTable{TKey,TValue}"/>, in which case it extends the lifetime of <see cref="Object"/>
    /// only while the key of the entry is alive.
    /// </summary>
    public bool IsConditional { get; }

    /// <summary>
    /// Gets the number of references between the root and this node. A root has depth zero.
    /// </summary>
    public int Depth { get; }

    internal ObjectGraphNode( object obj, string label, ObjectGraphNode? parent, bool isConditional )
    {
        this.Object = obj;
        this.Label = label;
        this.Parent = parent;
        this.IsConditional = isConditional;
        this.Depth = parent == null ? 0 : parent.Depth + 1;
    }

    /// <summary>
    /// Gets the chain of nodes from the root to the current node, both included.
    /// </summary>
    public IReadOnlyList<ObjectGraphNode> GetPath()
    {
        var path = new List<ObjectGraphNode>( this.Depth + 1 );

        for ( var node = this; node != null; node = node.Parent )
        {
            path.Add( node );
        }

        path.Reverse();

        return path;
    }

    /// <summary>
    /// Formats the chain of references that leads to the current node, one hop per line, indented by depth.
    /// </summary>
    public string FormatPath()
    {
        var path = this.GetPath();
        var stringBuilder = new StringBuilder();

        for ( var i = 0; i < path.Count; i++ )
        {
            var indent = new string( ' ', i * 2 );
            var kind = path[i].IsConditional ? " [conditional]" : "";
            stringBuilder.AppendLine( $"{indent}{path[i].Label} : {FormatType( path[i].Object.GetType() )}{kind}" );
        }

        return stringBuilder.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats a type name in a form short enough to read in a diagnostic message.
    /// </summary>
    public static string FormatType( Type type )
    {
        if ( !type.IsGenericType )
        {
            return type.Name;
        }

        var name = type.Name;
        var backTick = name.IndexOf( '`' );

        if ( backTick > 0 )
        {
            name = name.Substring( 0, backTick );
        }

        var arguments = type.GetGenericArguments();
        var formattedArguments = new string[arguments.Length];

        for ( var i = 0; i < arguments.Length; i++ )
        {
            formattedArguments[i] = FormatType( arguments[i] );
        }

        return $"{name}<{string.Join( ",", formattedArguments )}>";
    }

    public override string ToString() => $"{this.Label} : {FormatType( this.Object.GetType() )}";
}
