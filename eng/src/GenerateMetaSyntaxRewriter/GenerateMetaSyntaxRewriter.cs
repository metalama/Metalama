// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.GenerateMetaSyntaxRewriter.Model;
using System;
using System.IO;
using System.Linq;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter;

internal static class GenerateMetaSyntaxRewriter
{
    public static void Generate( string baseDirectory )
    {
        var deprecatedVersionNames = Array.Empty<string>();
        string[] legacyVersionNames = ["4.0.1"]; // versions that should be considered when generating code, but not have their own generated code
        string[] versionNames = [.. legacyVersionNames, "4.4.0", "4.8.0", "4.12.0"];

        var syntaxDocuments = new SyntaxDocument[versionNames.Length];

        for ( var versionIndex = 0; versionIndex < versionNames.Length; versionIndex++ )
        {
            var version = new RoslynVersion( versionNames[versionIndex], versionIndex );
            syntaxDocuments[versionIndex] = new SyntaxDocument( baseDirectory, version );
        }

        VersionDetector.DetectVersions( syntaxDocuments );

        foreach ( var syntax in syntaxDocuments )
        {
            if ( legacyVersionNames.Contains( syntax.Version.Name ) )
            {
                continue;
            }

            Generator generator = new( syntax, Path.Combine( baseDirectory, ".generated", $"{syntax.Version.Name}" ) );

            generator.GenerateRoslynApiVersionEnum(
                Path.Combine( $"Metalama.Framework.Engine", "RoslynApiVersion.g.cs" ),
                deprecatedVersionNames,
                syntaxDocuments );

            generator.GenerateTemplateFiles( Path.Combine( $"Metalama.Framework.Engine", "MetaSyntaxRewriter.g.cs" ), syntaxDocuments );
            generator.GenerateVersionChecker( Path.Combine( $"Metalama.Framework.Engine", "RoslynVersionSyntaxVerifier.g.cs" ) );
            generator.GenerateHasher( Path.Combine( $"Metalama.Framework.DesignTime", "RunTimeCodeHasher.g.cs" ), "RunTimeCodeHasher", false );
            generator.GenerateHasher( Path.Combine( $"Metalama.Framework.DesignTime", "CompileTimeCodeHasher.g.cs" ), "CompileTimeCodeHasher", true );
            generator.GeneratePartialUpdate( Path.Combine( $"Metalama.Framework.Engine", "SyntaxNodePartialUpdateExtensions.g.cs" ) );
        }
    }
}