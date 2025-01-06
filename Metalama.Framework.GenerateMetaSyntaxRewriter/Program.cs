// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.GenerateMetaSyntaxRewriter;
using Metalama.Framework.GenerateMetaSyntaxRewriter.Model;
using System;
using System.IO;
using System.Linq;

if ( args is not ([_] or [_, "--stubs"]) )
{
    Console.Error.WriteLine( "Usage: GenerateMetaSyntaxRewriter.exe <targetDirectory> [--stubs]" );

    return 1;
}

var deprecatedVersionNames = Array.Empty<string>();
string[] legacyVersionNames = ["4.0.1"]; // versions that should be considered when generating code, but not have their own generated code
string[] versionNames = [.. legacyVersionNames, "4.4.0", "4.8.0", "4.12.0"];
var baseDirectory = args[0];
var generateStubs = args.Length > 1;

var syntaxDocuments = new SyntaxDocument[versionNames.Length];

for ( var versionIndex = 0; versionIndex < versionNames.Length; versionIndex++ )
{
    var version = new RoslynVersion( versionNames[versionIndex], versionIndex );
    syntaxDocuments[versionIndex] = new SyntaxDocument( version );
}

VersionDetector.DetectVersions( syntaxDocuments );

foreach ( var syntax in syntaxDocuments )
{
    if ( legacyVersionNames.Contains( syntax.Version.Name ) )
    {
        continue;
    }

    var directorySuffix = generateStubs ? "-stubs" : string.Empty;
    var extension = generateStubs ? ".cs" : ".g.cs";
    Generator generator = new( syntax, Path.Combine( baseDirectory, ".generated", $"{syntax.Version.Name}{directorySuffix}" ), generateStubs );

    generator.GenerateRoslynApiVersionEnum(
        Path.Combine( $"Metalama.Framework.Engine", $"RoslynApiVersion{extension}" ),
        deprecatedVersionNames,
        syntaxDocuments );

    generator.GenerateTemplateFiles( Path.Combine( $"Metalama.Framework.Engine", $"MetaSyntaxRewriter{extension}" ), syntaxDocuments );
    generator.GenerateVersionChecker( Path.Combine( $"Metalama.Framework.Engine", $"RoslynVersionSyntaxVerifier{extension}" ) );
    generator.GenerateHasher( Path.Combine( $"Metalama.Framework.DesignTime", $"RunTimeCodeHasher{extension}" ), "RunTimeCodeHasher", false );
    generator.GenerateHasher( Path.Combine( $"Metalama.Framework.DesignTime", $"CompileTimeCodeHasher{extension}" ), "CompileTimeCodeHasher", true );
    generator.GeneratePartialUpdate( Path.Combine( $"Metalama.Framework.Engine", $"SyntaxNodePartialUpdate{extension}" ) );
}

return 0;