// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// Gets the stable anchor of an object: its declaration identifier, its type identifier, or an anchor given by the context.
/// </summary>
internal sealed class LinkBuilder
{
    private readonly ILinkContext _context;

    public LinkBuilder( ILinkContext context )
    {
        this._context = context;
    }

    /// <summary>
    /// Gets the link of an object that has an anchor of its own, or <c>null</c> when the object can only be reached by a path
    /// from another object.
    /// </summary>
    public Link? GetAnchorLink( object? value )
    {
        switch ( value )
        {
            case null:
                return null;

            case IDeclaration declaration:
                {
                    string id;

                    try
                    {
                        id = declaration.ToSerializableId().Id;
                    }
                    catch ( Exception e ) when ( e is NotImplementedException or NotSupportedException or InvalidOperationException or AssertionFailedException )
                    {
                        // Identifiers are not implemented for every declaration, notably some declarations introduced by aspects.
                        return null;
                    }

                    return this.CreateLink( declaration.Compilation, id );
                }

            case IType type:
                {
                    string id;

                    try
                    {
                        id = type.ToSerializableId().Id;
                    }
                    catch ( Exception e ) when ( e is NotImplementedException or NotSupportedException or InvalidOperationException or AssertionFailedException )
                    {
                        return null;
                    }

                    return this.CreateLink( type.Compilation, id );
                }

            default:
                return this._context.GetObjectAnchor( value );
        }
    }

    private Link? CreateLink( ICompilation compilation, string anchor )
    {
        if ( !this._context.TryGetScope( compilation, out var workspace, out var scope, out var isTransformed ) )
        {
            return null;
        }

        return new Link( workspace, scope, isTransformed, anchor, ImmutableArray<string>.Empty, this._context.GetFingerprint( workspace ) );
    }
}
