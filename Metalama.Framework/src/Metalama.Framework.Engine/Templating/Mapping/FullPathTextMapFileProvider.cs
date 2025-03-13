// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Metalama.Framework.Engine.Templating.Mapping;

/// <summary>
/// An implementation of <see cref="ITextMapFileProvider"/> that loads the <c>.map</c>
/// file in the same directory as the <c>.cs</c> file.
/// </summary>
internal sealed class FullPathTextMapFileProvider : ITextMapFileProvider
{
    public static FullPathTextMapFileProvider Instance { get; } = new();

    private FullPathTextMapFileProvider() { }

    public bool TryGetMapFile( string path, [NotNullWhen( true )] out TextMapFile? file )
    {
        file = TextMapFile.Read( Path.ChangeExtension( path, ".map" ) );

        return file != null;
    }
}