// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
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
        File.WriteAllLines( filesListPath, new[] { previewedPath } );

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

        using var manifest = JsonDocument.Parse( File.ReadAllText( Path.Combine( outputDirectory, "preview.json" ) ) );
        var file = Assert.Single( manifest.RootElement.GetProperty( "files" ).EnumerateArray() );

        Assert.Equal( previewedPath, file.GetProperty( "sourcePath" ).GetString() );
        Assert.True( file.GetProperty( "isTransformed" ).GetBoolean() );

        var previewedOutput = File.ReadAllText( file.GetProperty( "outputPath" ).GetString()! );

        // The previewed file is linked and formatted.
        Assert.Contains( "Entering M", previewedOutput, StringComparison.Ordinal );
        Assert.DoesNotContain( "global::", previewedOutput, StringComparison.Ordinal );
        Assert.DoesNotContain( "Other", previewedOutput, StringComparison.Ordinal );

        // The other file is not written.
        Assert.Single( Directory.GetFiles( outputDirectory, "*.cs", SearchOption.AllDirectories ) );
    }

    [Fact]
    public async Task SyntaxTreesSharingThePreviewedPathAreOneDocument()
    {
        using var testContext = this.CreateTestContext();

        var outputDirectory = Path.Combine( testContext.BaseDirectory, "preview" );
        var filesListPath = Path.Combine( testContext.BaseDirectory, "files.txt" );
        var previewedPath = Path.GetFullPath( "Previewed.cs" );
        File.WriteAllLines( filesListPath, new[] { previewedPath } );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["Aspect.cs"] = _aspectCode, ["Previewed.cs"] = _previewedCode } );

        // Roslyn accepts two distinct syntax trees with the same path. Metalama treats them as one document, represented by the first.
        var duplicate = SyntaxFactory.ParseSyntaxTree(
            _otherCode,
            (CSharpParseOptions) compilation.SyntaxTrees.First().Options,
            "Previewed.cs",
            cancellationToken: TestContext.Current.CancellationToken );

        compilation = compilation.AddSyntaxTrees( duplicate );

        var serviceProvider = testContext.ServiceProvider.WithService(
            new PreviewTestProjectOptions( testContext.ProjectOptions, filesListPath, outputDirectory ),
            true );

        var pipeline = new CompileTimeAspectPipeline( serviceProvider );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.Contains( diagnostics, d => d.Id == "LAMA0121" );

        using var manifest = JsonDocument.Parse( File.ReadAllText( Path.Combine( outputDirectory, "preview.json" ) ) );
        var file = Assert.Single( manifest.RootElement.GetProperty( "files" ).EnumerateArray() );
        var previewedOutput = File.ReadAllText( file.GetProperty( "outputPath" ).GetString()! );

        Assert.Contains( "Entering M", previewedOutput, StringComparison.Ordinal );
        Assert.DoesNotContain( "Other", previewedOutput, StringComparison.Ordinal );
    }

    [Fact]
    public async Task TransformationsOfOtherFilesThatThePreviewedFileDependsOnAreApplied()
    {
        // The aspect makes TargetCode implement IInitializable. The creation of TargetCode in the previewed Caller.cs is linked
        // only if TargetCode, declared in a file that is not previewed, implements the interface in the intermediate compilation.
        const string aspectCode = """
                                  using Metalama.Framework.Advising;
                                  using Metalama.Framework.Aspects;
                                  using Metalama.Framework.Code;

                                  public class TheAspect : TypeAspect
                                  {
                                      public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                          => builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );

                                      [Template]
                                      private void InitializerTemplate() => System.Console.WriteLine( "Initialized!" );
                                  }
                                  """;

        const string targetCode = """
                                  [TheAspect]
                                  public class TargetCode { }
                                  """;

        const string callerCode = """
                                  public class Caller
                                  {
                                      public void Method()
                                      {
                                          var t = new TargetCode();
                                      }
                                  }
                                  """;

        using var testContext = this.CreateTestContext();

        var previewedOutput = await PreviewAsync(
            testContext,
            new Dictionary<string, string> { ["Aspect.cs"] = aspectCode, ["Target.cs"] = targetCode, ["Caller.cs"] = callerCode },
            "Caller.cs" );

        Assert.Contains( ".WithInitialize()", previewedOutput, StringComparison.Ordinal );
    }

    // Previews one file and returns its transformed code.
    private static async Task<string> PreviewAsync( MetalamaTestContext testContext, Dictionary<string, string> code, string previewedFile )
    {
        var outputDirectory = Path.Combine( testContext.BaseDirectory, "preview" );
        var filesListPath = Path.Combine( testContext.BaseDirectory, "files.txt" );
        File.WriteAllLines( filesListPath, new[] { Path.GetFullPath( previewedFile ) } );

        var compilation = testContext.CreateCSharpCompilation( code );

        var serviceProvider = testContext.ServiceProvider.WithService(
            new PreviewTestProjectOptions( testContext.ProjectOptions, filesListPath, outputDirectory ),
            true );

        var diagnostics = new DiagnosticBag();
        await new CompileTimeAspectPipeline( serviceProvider ).ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.Equal( ["LAMA0121"], diagnostics.Where( d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error ).Select( d => d.Id ) );

        using var manifest = JsonDocument.Parse( File.ReadAllText( Path.Combine( outputDirectory, "preview.json" ) ) );
        var file = Assert.Single( manifest.RootElement.GetProperty( "files" ).EnumerateArray() );

        return File.ReadAllText( file.GetProperty( "outputPath" ).GetString()! );
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
