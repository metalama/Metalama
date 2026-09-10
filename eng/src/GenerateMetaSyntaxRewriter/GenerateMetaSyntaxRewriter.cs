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

        // Versions that should be considered when generating code, but not have their own generated code.
        string[] legacyVersionNames = ["4.0.1", "4.4.0", "4.8.0", "4.12.0"];

        // The versions in ascending order, which is the order that assigns the ordinals of RoslynApiVersion. The
        // ordinals are not persisted: a manifest stores the member by name, because ManifestJsonContext registers
        // JsonStringEnumConverter. Retiring a version therefore removes its member rather than renumbering around it,
        // which is what issue #2005 did with 5.10.0 when the latest variant was renumbered to 5.11.0.
        string[] versionNames = [.. legacyVersionNames, "5.0.0", "5.11.0"];

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