// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class CompileTimePreviewTests : UnitTestClass
{
    protected override MetalamaTestContextOptions CreateDefaultTestContextOptions() => new() { CodeFormattingOptions = CodeFormattingOptions.Formatted };

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;

                                       public class LogAttribute : OverrideMethodAspect
                                       {
                                           public override dynamic? OverrideMethod()
                                           {
                                               System.Console.WriteLine( "Entering " + meta.Target.Method.Name );
                                               return meta.Proceed();
                                           }
                                       }
                                       """;

    private const string _previewedCode = """
                                          public class Previewed
                                          {
                                              [Log]
                                              public void M() { }
                                          }
                                          """;

    private const string _otherCode = """
                                      public class Other
                                      {
                                          [Log]
                                          public void N() { }
                                      }
                                      """;

    [Fact]
    public async Task OnlyPreviewedFileIsWrittenAndCompilerIsStopped()
    {
        using var testContext = this.CreateTestContext();

        var outputDirectory = Path.Combine( testContext.BaseDirectory, "preview" );
        var filesListPath = Path.Combine( testContext.BaseDirectory, "files.txt" );
        var previewedPath = Path.GetFullPath( "Previewed.cs" );
        await File.WriteAllLinesAsync( filesListPath, [previewedPath], TestContext.Current.CancellationToken );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Aspect.cs"] = _aspectCode, ["Previewed.cs"] = _previewedCode, ["Other.cs"] = _otherCode } );

        var serviceProvider = testContext.ServiceProvider.WithService(
            new PreviewTestProjectOptions( testContext.ProjectOptions, filesListPath, outputDirectory ),
            true );

        var pipeline = new CompileTimeAspectPipeline( serviceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        // The pipeline fails on purpose, with the error that stops the compiler, and with no other error.
        Assert.False( result.IsSuccessful );
        Assert.Equal( ["LAMA0121"], diagnostics.Where( d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error ).Select( d => d.Id ) );

        using var manifest = JsonDocument.Parse( await File.ReadAllTextAsync( Path.Combine( outputDirectory, "preview.json" ), TestContext.Current.CancellationToken ) );
        var file = Assert.Single( manifest.RootElement.GetProperty( "files" ).EnumerateArray() );

        Assert.Equal( previewedPath, file.GetProperty( "sourcePath" ).GetString() );
        Assert.True( file.GetProperty( "isTransformed" ).GetBoolean() );

        var previewedOutput = await File.ReadAllTextAsync( file.GetProperty( "outputPath" ).GetString()!, TestContext.Current.CancellationToken );

        // The previewed file is linked and formatted.
        Assert.Contains( "Entering M", previewedOutput, StringComparison.Ordinal );
        Assert.DoesNotContain( "global::", previewedOutput, StringComparison.Ordinal );
        Assert.DoesNotContain( "Other", previewedOutput, StringComparison.Ordinal );

        // The other file is not written.
        Assert.Single( Directory.GetFiles( outputDirectory, "*.cs", SearchOption.AllDirectories ) );
    }

    private sealed class PreviewTestProjectOptions : ProjectOptionsWrapper
    {
        private readonly string _filesListPath;
        private readonly string _outputPath;

        public PreviewTestProjectOptions( IProjectOptions wrapped, string filesListPath, string outputPath ) : base( wrapped )
        {
            this._filesListPath = filesListPath;
            this._outputPath = outputPath;
        }

        public override string? PreviewFilesListPath => this._filesListPath;

        public override string? PreviewOutputPath => this._outputPath;
    }
}
