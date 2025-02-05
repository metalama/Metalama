// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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