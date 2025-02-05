// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating.Mapping;

internal interface ITextMapFileProvider
{
    /// <summary>
    /// Gets a <see cref="TextMapFile"/> given a full path on disk.
    /// </summary>
    bool TryGetMapFile( string path, [NotNullWhen( true )] out TextMapFile? file );
}