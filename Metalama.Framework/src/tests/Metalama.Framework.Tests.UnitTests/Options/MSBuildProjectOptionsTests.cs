// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Options;

public sealed class MSBuildProjectOptionsTests
{
    [Fact]
    public void EmptySourceGeneratorTouchFile_ReturnsNull()
    {
        // Simulates the scenario where MSBuild provides an empty string for MetalamaSourceGeneratorTouchFile,
        // which happens when the SDK is not resolved and IntermediateOutputPath is empty.
        var source = new DictionaryOptionsSource( new Dictionary<string, string> { [MSBuildPropertyNames.MetalamaSourceGeneratorTouchFile] = "" } );

        var options = new TestableMSBuildProjectOptions( source );

        Assert.Null( options.SourceGeneratorTouchFile );
    }

    [Fact]
    public void EmptyBuildTouchFile_ReturnsNull()
    {
        var source = new DictionaryOptionsSource( new Dictionary<string, string> { [MSBuildPropertyNames.MetalamaBuildTouchFile] = "" } );

        var options = new TestableMSBuildProjectOptions( source );

        Assert.Null( options.BuildTouchFile );
    }

    [Fact]
    public void WhitespaceSourceGeneratorTouchFile_ReturnsNull()
    {
        var source = new DictionaryOptionsSource( new Dictionary<string, string> { [MSBuildPropertyNames.MetalamaSourceGeneratorTouchFile] = "   " } );

        var options = new TestableMSBuildProjectOptions( source );

        Assert.Null( options.SourceGeneratorTouchFile );
    }

    [Fact]
    public void NonEmptySourceGeneratorTouchFile_ReturnsValue()
    {
        const string touchFilePath = @"C:\project\obj\MetalamaSourceGenerator.touch";

        var source = new DictionaryOptionsSource( new Dictionary<string, string> { [MSBuildPropertyNames.MetalamaSourceGeneratorTouchFile] = touchFilePath } );

        var options = new TestableMSBuildProjectOptions( source );

        Assert.Equal( touchFilePath, options.SourceGeneratorTouchFile );
    }

    [Fact]
    public void MissingSourceGeneratorTouchFile_ReturnsNull()
    {
        // When IntermediateOutputPath is undefined (e.g. SDK not yet resolved), the targets file skips
        // ExportMetalamaFrameworkProperties and the property is never exposed to the compiler. See #1597.
        var source = new DictionaryOptionsSource( new Dictionary<string, string>() );

        var options = new TestableMSBuildProjectOptions( source );

        Assert.Null( options.SourceGeneratorTouchFile );
        Assert.Null( options.BuildTouchFile );
    }

    private sealed class TestableMSBuildProjectOptions : MSBuildProjectOptions
    {
        public TestableMSBuildProjectOptions( IProjectOptionsSource source ) : base( source ) { }
    }

    private sealed class DictionaryOptionsSource : IProjectOptionsSource
    {
        private readonly Dictionary<string, string> _values;

        public DictionaryOptionsSource( Dictionary<string, string> values )
        {
            this._values = values;
        }

        public bool TryGetValue( string name, out string? value ) => this._values.TryGetValue( name, out value );

        public IEnumerable<string> PropertyNames => this._values.Keys;
    }
}