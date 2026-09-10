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

        // Versions that should be considered when generating code, but not have their own generated code. 5.10.0 is
        // here, and not among the versions that carry generated code, because the latest variant was renumbered from
        // it to 5.11.0 and no variant binds against Roslyn 5.10 any more. Its grammar is still read, so that the
        // version checker and the ordinals of RoslynApiVersion keep the value V5_10_0 that a host running Roslyn 5.10
        // is detected as.
        string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0", "5.10.0"];

        // The versions in ascending order, which is the order that assigns the ordinals of RoslynApiVersion. A version
        // inserted out of order renumbers the enumeration, and the values are persisted in compile-time assemblies.
        string[] versionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0", "5.0.0", "5.10.0", "5.11.0"];

        var syntaxDocuments = new SyntaxDocument[versionNames.Length];

        for ( var versionIndex = 0; versionIndex < versionNames.Length; versionIndex++ )
        {
            var version = new RoslynVersion( versionNames[versionIndex], versionIndex );
            syntaxDocuments[versionIndex] = new SyntaxDocument( baseDirectory, version, RoslynPreview.KeptGrammarFeatures );
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