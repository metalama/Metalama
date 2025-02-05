// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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