// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Framework.Engine.CompileTime
{
    public static class CompileTimeConstants
    {
        private const string _predefinedSyntaxTreePrefix = "sys!";

        internal static string GetPrefixedSyntaxTreeName( string name ) => _predefinedSyntaxTreePrefix + name;

        public static bool IsPredefinedSyntaxTree( string path )
        {
            var fileName = Path.GetFileNameWithoutExtension( path );

            return fileName.StartsWith( _predefinedSyntaxTreePrefix, StringComparison.Ordinal );
        }

        internal const string CompileTimeProjectResourceName = "Metalama.CompileTimeProject.zip";

        internal const string InheritableAspectManifestResourceName = "Metalama.InheritableAspects.bin";

        /// <summary>
        /// Determines whether the metadata of an assembly can be skipped, given only the file name of the assembly,
        /// because an assembly of that name never carries compile-time code.
        /// </summary>
        /// <remarks>
        /// This is a performance measure that avoids reading the metadata of the system assemblies, which form the
        /// majority of the references of a compilation. It is shared by the two places that need it, because they
        /// previously held two copies that had already diverged. The parameter is a file name and not an assembly name,
        /// so that the test can be applied before the metadata is read.
        /// </remarks>
        internal static bool IsSystemAssemblyFileName( string assemblyFileName )
            => assemblyFileName.Equals( "System", StringComparison.OrdinalIgnoreCase )
               || assemblyFileName.StartsWith( "System.", StringComparison.OrdinalIgnoreCase )
               || assemblyFileName.StartsWith( "Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase );
    }
}