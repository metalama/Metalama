// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using System;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable StringLiteralTypo

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A test runner that generates HTML output for input and output code, with syntax highlighting
/// and diagnostic annotations. Use this runner to test HTML documentation generation.
/// </summary>
internal sealed class HtmlGenerationTestRunner : AspectTestRunner
{
    private const string _htmlProlog = @"
<html>
    <head>
        <style>
            .cr-CompileTime,
            .cr-Conflict,
            .cr-TemplateKeyword,
            .cr-Dynamic,
            .cr-CompileTimeVariable,
            .cr-GeneratedCode
            {
                background-color: rgba(50,50,90,0.1);
            }

            .cr-NeutralTrivia
            {
                background-color: rgba(0,255,0,0.1);
            }

            .cr-TemplateKeyword
            {
                color: rgb(250, 0, 250) !important;
                font-weight: bold;
            }

            .cr-Dynamic
            {
                text-decoration: underline;
            }

            .cr-CompileTimeVariable
            {
                font-style: italic;
            }

            .diag-Warning
            {
                text-decoration: underline 1px wavy orange;
            }

            .diag-Error
            {
                text-decoration: underline 1px wavy red;
            }

         .diff-Imaginary {
                display: block;
                background-image: repeating-linear-gradient( -45deg, gray, gray 2px, transparent 2px, transparent 8px );
            }


            .legend
            {
                margin-top: 100px;
            }
        </style>
    </head>
    <body>";

    private readonly string _htmlEpilogue;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlGenerationTestRunner"/> class.
    /// </summary>
    internal HtmlGenerationTestRunner(
        GlobalServiceProvider serviceProvider,
        string? projectDirectory,
        TestProjectReferences references,
        ITestOutputHelper? logger )
        : base( serviceProvider, projectDirectory, references, logger )
    {
        StringBuilder epilogueBuilder = new();

        epilogueBuilder.AppendLine( "       <p class='legend'>Legend:</p>" );
        epilogueBuilder.AppendLine( "       <pre>" );

        foreach ( var classification in Enum.GetValues( typeof(TextSpanClassification) ) )
        {
            epilogueBuilder.AppendLine( FormattableString.Invariant( $"<span class='cr-{classification}'>{classification}</span>" ) );
        }

        epilogueBuilder.AppendLine( "       </pre>" );
        epilogueBuilder.AppendLine( "   </body>" );
        epilogueBuilder.AppendLine( "</html>" );

        this._htmlEpilogue = epilogueBuilder.ToString();
    }

    /// <inheritdoc />
    protected override bool CompareTransformedCode => false;

    /// <inheritdoc />
    protected override HtmlCodeWriterOptions GetHtmlCodeWriterOptions( TestOptions options )
        => new( options.AddHtmlTitles.GetValueOrDefault(), _htmlProlog, this._htmlEpilogue );

    /// <inheritdoc />
    protected override void ExecuteAssertions( TestInput testInput, TestResult testResult )
    {
        base.ExecuteAssertions( testInput, testResult );

        Assert.NotNull( testInput.ProjectDirectory );
        Assert.NotNull( testInput.RelativePath );

        foreach ( var diagnostic in testResult.AllDiagnostics )
        {
            this.Logger?.WriteLine( diagnostic.ToString() );
        }

        // Input
        if ( testInput.Options.WriteInputHtml.GetValueOrDefault() )
        {
            foreach ( var syntaxTree in testResult.SyntaxTrees )
            {
                var sourceAbsolutePath = syntaxTree.InputPath;

                // Skip introduced trees.
                if ( sourceAbsolutePath == null )
                {
                    continue;
                }

                // Input.
                var expectedInputHtmlPath = Path.Combine(
                    Path.GetDirectoryName( sourceAbsolutePath )!,
                    Path.GetFileNameWithoutExtension( sourceAbsolutePath ) + FileExtensions.InputHtml );

                this.CompareHtmlFiles( syntaxTree.HtmlInputPath!, expectedInputHtmlPath, testInput.Options );
            }
        }

        // Output.
        if ( testInput.Options.WriteOutputHtml.GetValueOrDefault() )
        {
            foreach ( var syntaxTree in testResult.SyntaxTrees )
            {
                var extension = syntaxTree.Kind is TestSyntaxTreeKind.Introduced ? FileExtensions.IntroducedHtml : FileExtensions.TransformedHtml;

                var expectedOutputHtmlPath = Path.Combine(
                    Path.GetDirectoryName( testInput.FullPath )!,
                    syntaxTree.ShortName + extension );

                this.CompareHtmlFiles( syntaxTree.HtmlOutputPath.AssertNotNull(), expectedOutputHtmlPath, testInput.Options );
            }
        }
    }

    private void CompareHtmlFiles( string actualHtmlPath, string expectedHtmlPath, TestOptions testOptions )
    {
        this.Logger?.WriteLine( "Actual HTML: " + actualHtmlPath );

        if ( !File.Exists( expectedHtmlPath ) )
        {
            File.WriteAllText( expectedHtmlPath, "TODO: Replace this file with the expected/accepted HTML." );
        }

        this.Logger?.WriteLine( "Expected HTML: " + expectedHtmlPath );

        var expectedHighlightedSource = TestOutputNormalizer.NormalizeEndOfLines( File.ReadAllText( expectedHtmlPath ) );

        var htmlPath = actualHtmlPath;
        var actualHighlightedSource = TestOutputNormalizer.NormalizeEndOfLines( File.ReadAllText( htmlPath ) );

        var hasDifference = this.CompareFiles( expectedHighlightedSource, expectedHtmlPath, actualHighlightedSource, htmlPath, testOptions );

        if ( hasDifference )
        {
            Assert.Equal( expectedHighlightedSource, actualHighlightedSource );
        }
    }
}
