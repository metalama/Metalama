// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Workspaces.Navigation.Facade;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Metalama.Framework.Workspaces.Navigation.Links;

/// <summary>
/// Resolves a <see cref="Link"/> to an object: it resolves the anchor, checks the fingerprint of fragile links, and applies
/// the steps one by one.
/// </summary>
internal sealed class LinkResolver
{
    private readonly ILinkContext _context;
    private readonly FacadeTypeFactory _facadeTypeFactory;

    public LinkResolver( ILinkContext context, FacadeTypeFactory? facadeTypeFactory = null )
    {
        this._context = context;
        this._facadeTypeFactory = facadeTypeFactory ?? FacadeTypeFactory.Default;
    }

    public bool TryResolve( Link link, out object? value, [NotNullWhen( false )] out string? error )
    {
        value = null;

        if ( link.IsFragile && link.Fingerprint != null )
        {
            var currentFingerprint = this._context.GetFingerprint( link.Workspace );

            if ( currentFingerprint != null && !string.Equals( currentFingerprint, link.Fingerprint, StringComparison.OrdinalIgnoreCase ) )
            {
                error = "stale: the workspace changed since this link was produced; re-run the query.";

                return false;
            }
        }

        if ( !this.TryResolveAnchor( link, out var current, out error ) )
        {
            return false;
        }

        foreach ( var step in link.Steps )
        {
            if ( current == null )
            {
                error = $"Cannot apply the step '{step}' because the previous value is null.";

                return false;
            }

            if ( !this.TryApplyStep( current, step, out current, out error ) )
            {
                return false;
            }
        }

        value = current;

        return true;
    }

    private bool TryResolveAnchor( Link link, out object? value, [NotNullWhen( false )] out string? error )
    {
        value = null;

        if ( link.IsQueryAnchor )
        {
            error = "Query anchors are not supported yet. Re-run the query instead.";

            return false;
        }

        if ( !link.IsIdentifierAnchor )
        {
            return this._context.TryGetCollectionAnchor( link.Workspace, link.Scope, link.Anchor, out value, out error );
        }

        if ( !this._context.TryGetCompilation( link.Workspace, link.Scope, link.IsTransformed, out var compilation, out error ) )
        {
            return false;
        }

        try
        {
            if ( link.Anchor.StartsWith( "Y:", StringComparison.Ordinal ) || link.Anchor.StartsWith( "typeof", StringComparison.Ordinal ) )
            {
                value = new SerializableTypeId( link.Anchor ).Resolve( compilation );
            }
            else
            {
                value = new SerializableDeclarationId( link.Anchor ).Resolve( compilation );
            }
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            error = $"not found: cannot resolve '{link.Anchor}' in {link.Scope}: {e.Message}";

            return false;
        }

        if ( value == null )
        {
            error = $"not found: '{link.Anchor}' does not exist in {link.Scope}.";

            return false;
        }

        error = null;

        return true;
    }

    private bool TryApplyStep( object current, string step, out object? value, [NotNullWhen( false )] out string? error )
    {
        value = null;

        if ( Link.IsIndexStep( step ) )
        {
            if ( current is string or not IEnumerable )
            {
                error = $"Cannot apply the index step '{step}' to a value of type '{current.GetType().Name}', which is not a collection.";

                return false;
            }

            var index = int.Parse( step, CultureInfo.InvariantCulture );
            var position = 0;

            foreach ( var item in (IEnumerable) current )
            {
                if ( position == index )
                {
                    value = item;
                    error = null;

                    return true;
                }

                position++;
            }

            error = $"The index {index} is out of range: the collection has {position} item(s).";

            return false;
        }

        if ( Link.IsKeyStep( step ) )
        {
            error = $"The step '{step}' is not supported yet.";

            return false;
        }

        var facadeType = this._facadeTypeFactory.GetFacadeType( current.GetType() );

        if ( !facadeType.TryGetProperty( step, out var property ) )
        {
            error = $"The type '{current.GetType().Name}' has no displayed property '{step}'.";

            return false;
        }

        try
        {
            value = property.GetFunc( current );
        }
        catch ( Exception e ) when ( e is not OutOfMemoryException )
        {
            error = $"Getting the property '{step}' threw {e.GetType().Name}: {e.Message}";

            return false;
        }

        error = null;

        return true;
    }
}
