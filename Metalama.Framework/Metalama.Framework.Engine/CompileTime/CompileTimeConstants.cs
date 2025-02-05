// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.IO;

namespace Metalama.Framework.Engine.CompileTime
{
    public static class CompileTimeConstants
    {
        internal static string GetPrefixedSyntaxTreeName( string name ) => "(" + name + ")";

        public static bool IsPredefinedSyntaxTree( string path )
        {
            var fileName = Path.GetFileNameWithoutExtension( path );

            return fileName.StartsWith( "(", StringComparison.Ordinal ) && fileName.EndsWith( ")", StringComparison.Ordinal );
        }

        internal const string CompileTimeProjectResourceName = "Metalama.CompileTimeProject.zip";

        internal const string InheritableAspectManifestResourceName = "Metalama.InheritableAspects.bin";
    }
}