// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Workspaces.Navigation.Links;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Workspaces.Navigation.Tests;

/// <summary>
/// An <see cref="ILinkContext"/> over a single in-memory compilation, named <c>Test@net10.0</c> in the workspace <c>test</c>.
/// </summary>
internal sealed class TestLinkContext : ILinkContext
{
    public const string Workspace = "test";
    public const string Scope = "Test@net10.0";

    private readonly ICompilation _compilation;

    public TestLinkContext( ICompilation compilation, string fingerprint = "0123456789ab" )
    {
        this._compilation = compilation;
        this.Fingerprint = fingerprint;
    }

    public string Fingerprint { get; set; }

    public static LinkBase Base => new( Workspace, Scope );

    public bool TryGetScope( ICompilation compilation, out string? workspace, [NotNullWhen( true )] out string? scope, out bool isTransformed )
    {
        workspace = Workspace;
        scope = Scope;
        isTransformed = false;

        return true;
    }

    public Link? GetObjectAnchor( object value ) => null;

    public string? GetFingerprint( string? workspace ) => this.Fingerprint;

    public bool TryGetCompilation(
        string? workspace,
        string scope,
        bool isTransformed,
        [NotNullWhen( true )] out ICompilation? compilation,
        [NotNullWhen( false )] out string? error )
    {
        if ( scope != Scope )
        {
            compilation = null;
            error = $"Unknown scope {scope}.";

            return false;
        }

        compilation = this._compilation;
        error = null;

        return true;
    }

    public bool TryGetCollectionAnchor( string? workspace, string scope, string name, out object? value, [NotNullWhen( false )] out string? error )
    {
        if ( name == "types" )
        {
            value = this._compilation.Types;
            error = null;

            return true;
        }

        value = null;
        error = $"Unknown anchor {name}.";

        return false;
    }
}
