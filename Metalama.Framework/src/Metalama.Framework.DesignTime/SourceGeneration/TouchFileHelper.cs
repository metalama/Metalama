// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Globalization;

namespace Metalama.Framework.DesignTime.SourceGeneration;

internal static class TouchFileHelper
{
    /// <summary>
    /// Computes a touch ID from the file content and its last write time.
    /// The format is <c>content|lastWriteTimeTicks</c>.
    /// </summary>
    public static string GetTouchId( string content, string filePath )
    {
        var lastWriteTime = File.Exists( filePath )
            ? File.GetLastWriteTimeUtc( filePath ).Ticks.ToString( CultureInfo.InvariantCulture )
            : "";

        return content + "|" + lastWriteTime;
    }
}
