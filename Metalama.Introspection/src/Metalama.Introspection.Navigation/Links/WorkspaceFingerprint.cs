// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Workspaces;
using System;
using System.Globalization;
using System.Linq;
using Compilation = Microsoft.CodeAnalysis.Compilation;

namespace Metalama.Introspection.Navigation.Links;

/// <summary>
/// Computes the fingerprint of a loaded workspace, which detects that a fragile link is stale.
/// </summary>
/// <remarks>
/// The fingerprint covers the compilation inputs and the load itself. The inputs alone are not enough: some collections of the
/// code model, such as <see cref="ICompilationSet.Types"/> and <see cref="ICompilationSet.Methods"/>, are not enumerated in
/// source order, and their order can differ between two loads of identical sources. An index step is therefore only valid for
/// the load that produced it.
/// </remarks>
internal static class WorkspaceFingerprint
{
    public static string Compute( Workspace workspace, long loadGeneration )
        => Compute( loadGeneration, workspace.Projects.Select( p => (WorkspaceLinkContext.GetScope( p ), p.RoslynCompilation) ).ToArray() );

    public static string Compute( long loadGeneration, params (string Scope, Compilation Compilation)[] compilations )
    {
        using var handle = HashUtilities.AllocateHasher();
        var hash = handle.Value;
        hash.Append( loadGeneration );

        foreach ( var (scope, compilation) in compilations.OrderBy( c => c.Scope, StringComparer.Ordinal ) )
        {
            hash.Append( scope );

            foreach ( var tree in compilation.SyntaxTrees.OrderBy( t => t.FilePath, StringComparer.Ordinal ) )
            {
                hash.Append( tree.FilePath );
                hash.Append( tree.GetText().GetChecksum().AsSpan() );
            }

            foreach ( var reference in compilation.References.Select( r => r.Display ?? "" ).OrderBy( r => r, StringComparer.Ordinal ) )
            {
                hash.Append( reference );
            }

            hash.Append( compilation.Options.OutputKind.ToString() );
            hash.Append( compilation.Options.NullableContextOptions.ToString() );
        }

        return hash.GetCurrentHashAsUInt64().ToString( "x16", CultureInfo.InvariantCulture );
    }
}
