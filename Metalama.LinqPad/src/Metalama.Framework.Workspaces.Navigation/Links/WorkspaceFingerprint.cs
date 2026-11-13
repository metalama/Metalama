// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Framework.Workspaces.Navigation.Links;

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
        using var hash = IncrementalHash.CreateHash( HashAlgorithmName.SHA256 );
        hash.AppendData( BitConverter.GetBytes( loadGeneration ) );

        foreach ( var (scope, compilation) in compilations.OrderBy( c => c.Scope, StringComparer.Ordinal ) )
        {
            Append( hash, scope );

            foreach ( var tree in compilation.SyntaxTrees.OrderBy( t => t.FilePath, StringComparer.Ordinal ) )
            {
                Append( hash, tree.FilePath );
                hash.AppendData( tree.GetText().GetChecksum().AsSpan() );
            }

            foreach ( var reference in compilation.References.Select( r => r.Display ?? "" ).OrderBy( r => r, StringComparer.Ordinal ) )
            {
                Append( hash, reference );
            }

            Append( hash, compilation.Options.OutputKind.ToString() );
            Append( hash, compilation.Options.NullableContextOptions.ToString() );
        }

        return Convert.ToHexStringLower( hash.GetHashAndReset() ).Substring( 0, 12 );
    }

    private static void Append( IncrementalHash hash, string value )
    {
        hash.AppendData( Encoding.UTF8.GetBytes( value ) );
        hash.AppendData( [0] );
    }
}
