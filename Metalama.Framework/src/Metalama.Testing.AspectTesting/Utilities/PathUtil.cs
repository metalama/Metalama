// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using System;
using System.IO;

namespace Metalama.Testing.AspectTesting.Utilities
{
    internal static class PathUtil
    {
        /// <summary>
        /// This should emulate <c>Path.GetRelativePath</c>, which is not available in .NET Standard.
        /// </summary>
        public static string GetRelativePath( this IFileSystem fileSystem, string relativeTo, string path )
        {
            if ( relativeTo[^1] != Path.DirectorySeparatorChar && fileSystem.DirectoryExists( relativeTo ) )
            {
                relativeTo += Path.DirectorySeparatorChar;
            }

            var relativeUri = new Uri( relativeTo ).MakeRelativeUri( new Uri( path ) );

            return relativeUri.OriginalString.Replace( '/', Path.DirectorySeparatorChar );
        }
    }
}