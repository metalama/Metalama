// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Creates a copy of <c>Metalama.Framework</c> that declares an older version than the engine. A project references such a
/// version when the engine that analyzes it comes from a newer package than the one the project builds with, as with the
/// MCP server, the LINQPad driver or <c>Metalama.Framework.Workspaces</c>.
/// </summary>
internal static class OlderFrameworkAssembly
{
    public static Version Version { get; } = new( 2027, 0, 1, 0 );

    /// <summary>
    /// Writes the copy to a subdirectory of <paramref name="directory"/> and returns its path. Only the version of the assembly
    /// definition changes: the copy has the same types and templates, and Roslyn does not verify its strong-name signature.
    /// </summary>
    public static string Create( string directory )
    {
        var bytes = File.ReadAllBytes( typeof(IAspect).Assembly.Location );

        using ( var peReader = new PEReader( new MemoryStream( bytes ) ) )
        {
            var metadataReader = peReader.GetMetadataReader();

            // The row of the assembly definition starts with HashAlgId (4 bytes), followed by MajorVersion, MinorVersion,
            // BuildNumber and RevisionNumber (2 bytes each). See ECMA-335, II.22.2.
            var rowOffset = peReader.PEHeaders.MetadataStartOffset + metadataReader.GetTableMetadataOffset( TableIndex.Assembly );
            var version = bytes.AsSpan( rowOffset + 4, 8 );

            BinaryPrimitives.WriteUInt16LittleEndian( version.Slice( 0 ), (ushort) Version.Major );
            BinaryPrimitives.WriteUInt16LittleEndian( version.Slice( 2 ), (ushort) Version.Minor );
            BinaryPrimitives.WriteUInt16LittleEndian( version.Slice( 4 ), (ushort) Version.Build );
            BinaryPrimitives.WriteUInt16LittleEndian( version.Slice( 6 ), (ushort) Version.Revision );
        }

        var path = Path.Combine( directory, "OlderFramework", "Metalama.Framework.dll" );
        Directory.CreateDirectory( Path.GetDirectoryName( path )! );
        File.WriteAllBytes( path, bytes );

        return path;
    }

    /// <summary>
    /// Returns a copy of <paramref name="compilation"/> whose reference to <c>Metalama.Framework</c> is the older copy.
    /// </summary>
    public static Compilation ReferenceFrom( Compilation compilation, string directory )
    {
        var framework = compilation.References.OfType<PortableExecutableReference>()
            .Single( r => string.Equals( Path.GetFileName( r.FilePath ), "Metalama.Framework.dll", StringComparison.OrdinalIgnoreCase ) );

        return compilation.ReplaceReference( framework, MetadataReference.CreateFromFile( Create( directory ) ) );
    }
}
