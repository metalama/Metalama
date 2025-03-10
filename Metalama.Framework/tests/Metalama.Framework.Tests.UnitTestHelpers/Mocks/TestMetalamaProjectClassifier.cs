// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

#pragma warning disable LAMA0821 // Do not expose internal APIs.
internal sealed class TestMetalamaProjectClassifier : IMetalamaProjectClassifier
{
    public static Version CurrentMetalamaVersion { get; } = EngineAssemblyMetadataReader.Instance.AssemblyVersion;

    public static Version OtherMetalamaVersion { get; } = GetOtherMetalamaVersion();

    public const string OtherMetalamaVersionPreprocessorSymbol = "OTHER_METALAMA_VERSION";

    private static Version GetOtherMetalamaVersion()
    {
        var version = EngineAssemblyMetadataReader.Instance.AssemblyVersion;

        return new Version( version.Major + 10, version.Minor, version.Build, version.Revision );
    }

    public bool TryGetMetalamaVersion( Compilation compilation, [NotNullWhen( true )] out Version? version )
    {
        var reference = compilation.ExternalReferences.OfType<PortableExecutableReference>()
            .SingleOrDefault( x => Path.GetFileNameWithoutExtension( x.FilePath )!.Equals( "Metalama.Framework", StringComparison.OrdinalIgnoreCase ) );

        if ( reference != null )
        {
            var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options;

            // We assume, in all tests, by default, that the code is compiled against the current version of Metalama.
            version = CurrentMetalamaVersion;

            if ( parseOptions != null && parseOptions.PreprocessorSymbolNames.Contains( OtherMetalamaVersionPreprocessorSymbol ) )
            {
                version = OtherMetalamaVersion;
            }

            return true;
        }
        else
        {
            version = null;

            return false;
        }
    }
}