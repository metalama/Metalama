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
    }
}