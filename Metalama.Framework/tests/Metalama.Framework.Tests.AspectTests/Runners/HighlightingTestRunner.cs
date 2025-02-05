// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.AspectTesting;
using System;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable StringLiteralTypo

namespace Metalama.Framework.Tests.AspectTests.Runners
{
    internal sealed class HighlightingTestRunner : AspectTestRunner
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

        public HighlightingTestRunner(
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

        protected override bool CompareTransformedCode => false;

        protected override HtmlCodeWriterOptions GetHtmlCodeWriterOptions( TestOptions options )
            => new( options.AddHtmlTitles.GetValueOrDefault(), _htmlProlog, this._htmlEpilogue );

        protected override void ExecuteAssertions( TestInput testInput, TestResult testResult )
        {
            base.ExecuteAssertions( testInput, testResult );

            Assert.NotNull( testInput.ProjectDirectory );
            Assert.NotNull( testInput.RelativePath );

            foreach ( var diagnostic in testResult.Diagnostics )
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

                    this.CompareHtmlFiles( syntaxTree.HtmlInputPath!, expectedInputHtmlPath );
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

                    this.CompareHtmlFiles( syntaxTree.HtmlOutputPath.AssertNotNull(), expectedOutputHtmlPath );
                }
            }
        }

        private void CompareHtmlFiles( string actualHtmlPath, string expectedHtmlPath )
        {
            this.Logger?.WriteLine( "Actual HTML: " + actualHtmlPath );

            Assert.True( File.Exists( expectedHtmlPath ), $"The expected HTML file '{expectedHtmlPath}' does not exist." );

            this.Logger?.WriteLine( "Expected HTML: " + expectedHtmlPath );

            var expectedHighlightedSource = TestOutputNormalizer.NormalizeEndOfLines( File.ReadAllText( expectedHtmlPath ) );

            var htmlPath = actualHtmlPath;
            var actualHighlightedSource = TestOutputNormalizer.NormalizeEndOfLines( File.ReadAllText( htmlPath ) );

            var hasDifference = this.RunDiffToolIfDifferent( expectedHighlightedSource, expectedHtmlPath, actualHighlightedSource, htmlPath );

            if ( hasDifference )
            {
                Assert.Equal( expectedHighlightedSource, actualHighlightedSource );
            }
        }
    }
}