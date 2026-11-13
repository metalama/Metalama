// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Workspaces;
using Metalama.Framework.Workspaces.Navigation.Links;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// The base class of compiled queries. Its protected members are the globals of a query.
/// </summary>
[PublicAPI]
public abstract class QueryBase
{
    private QueryContext? _context;

    // ReSharper disable InconsistentNaming
#pragma warning disable SA1401, IDE1006, SA1300, SA1306, CA1051

    /// <summary>
    /// The workspace named by the <c>workspace</c> argument, or the only loaded one. <c>null</c> when no workspace is loaded.
    /// </summary>
    protected Workspace workspace = null!;

    /// <summary>
    /// All loaded workspaces, by key.
    /// </summary>
    protected IReadOnlyDictionary<string, Workspace> workspaces = ImmutableDictionary<string, Workspace>.Empty;

    /// <summary>
    /// Fires on the soft timeout. Long queries should observe it, otherwise the worker is stopped on the hard timeout.
    /// </summary>
    protected CancellationToken ct;

#pragma warning restore SA1401, IDE1006, SA1300, SA1306, CA1051

    // ReSharper restore InconsistentNaming

    internal void Initialize( QueryContext context )
    {
        this._context = context;
        this.workspace = context.Workspace!;
        this.workspaces = context.Workspaces;
        this.ct = context.CancellationToken;
    }

    /// <summary>
    /// Resolves a declaration identifier, such as <c>T:MyApp.Customer</c>, in the source code of the workspace.
    /// </summary>
    /// <param name="id">The identifier, as printed at the start of a result row.</param>
    /// <param name="project">The project name. Required when the workspace has several projects.</param>
    /// <param name="tfm">The target framework. Required when the project has several.</param>
    /// <param name="transformed">Whether to resolve in the code transformed by Metalama.</param>
    protected IDeclaration D( string id, string? project = null, string? tfm = null, bool transformed = false )
    {
        var context = this.GetContext();
        string scope;

        if ( project != null )
        {
            scope = tfm != null ? $"{project}@{tfm}" : project;
        }
        else
        {
            scope = context.Base?.Scope
                    ?? throw new InvalidOperationException( "The workspace has several projects or target frameworks: pass `project` and `tfm` to D()." );
        }

        var value = this.Resolve( new Link( context.Base?.Workspace, scope, transformed, id, [], null ) );

        return value as IDeclaration ?? throw new InvalidOperationException( $"'{id}' resolves to a {value?.GetType().Name ?? "null"}, not to a declaration." );
    }

    /// <inheritdoc cref="D(string,string?,string?,bool)"/>
    protected T D<T>( string id, string? project = null, string? tfm = null, bool transformed = false )
        where T : class, IDeclaration
        => this.D( id, project, tfm, transformed ) as T ?? throw new InvalidOperationException( $"'{id}' is not a {typeof(T).Name}." );

    /// <summary>
    /// Resolves a link printed by a previous result, relative to the base of the workspace or absolute.
    /// </summary>
    protected object? Open( string link )
    {
        var context = this.GetContext();

        if ( !LinkParser.TryParse( link, context.Base, out var parsed, out var error ) )
        {
            throw new InvalidOperationException( error );
        }

        return this.Resolve( parsed );
    }

    /// <inheritdoc cref="Open(string)"/>
    protected T Open<T>( string link )
        where T : class
        => this.Open( link ) as T ?? throw new InvalidOperationException( $"The link '{link}' does not resolve to a {typeof(T).Name}." );

    /// <summary>
    /// Runs the query. Generated code overrides this method.
    /// </summary>
    protected internal abstract Task<object?> ExecuteAsync();

    private object? Resolve( Link link )
    {
        var context = this.GetContext();

        if ( !context.Resolver.TryResolve( link, out var value, out var error ) )
        {
            throw new InvalidOperationException( error );
        }

        return value;
    }

    private QueryContext GetContext() => this._context ?? throw new InvalidOperationException( "The query is not initialized." );
}
