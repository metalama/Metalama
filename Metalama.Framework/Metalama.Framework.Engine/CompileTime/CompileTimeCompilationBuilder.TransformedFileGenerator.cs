// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeCompilationBuilder
{
    private sealed class TransformedPathGenerator
    {
        private static int NameMaxLength => OutputPathHelper.MaxOutputFilenameLength - 1 /* backslash */ - 1 /* - */ - 8 /* hash */ - 3 /* .cs */;

        private readonly HashSet<string> _generatedNames = new( StringComparer.OrdinalIgnoreCase );

        public string GetTransformedFilePath( string fileName, ulong hash )
        {
            // It is essential that the file name here does NOT depend on the directory where the repo is checked out. If this happens, then the same project
            // built on different machines, or checked out in different directories, may have the same source hash but a different `manifest.json` file.
            // So, we hash the file according to its content, not according to its directory.

            var transformedFileName = fileName;

            if ( transformedFileName.Length > NameMaxLength )
            {
                transformedFileName = transformedFileName.Substring( 0, NameMaxLength );
            }

            string fileNameWithHash;

            unchecked
            {
                fileNameWithHash = $"{transformedFileName}_{(uint) hash:x8}.cs";
            }

            if ( !this._generatedNames.Add( fileNameWithHash ) )
            {
                throw new InvalidOperationException(
                    $"There cannot be two files in the compilation with the same filename '{transformedFileName}' and exactly the same code." );
            }

            return fileNameWithHash;
        }
    }
}