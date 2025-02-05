// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.AspectWorkbench.Model;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Tests.TemplateTests.Runner;
using Metalama.Testing.UnitTesting;
using Metalama.Testing.AspectTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Metalama.AspectWorkbench.ViewModels
{
    internal sealed class MainViewModel : INotifyPropertyChanged
    {
        static MainViewModel()
        {
            // Make sure a few assemblies are loaded.
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            typeof(Console).ToString();
        }

        private static readonly TestProjectProperties _projectProperties = new(
            assemblyName: null,
            projectDirectory: null,
            sourceDirectory: null,
            [
                "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER", "ROSLYN_4_4_0_OR_GREATER",
                "ROSLYN_4_8_0_OR_GREATER", "ROSLYN_4_12_0_OR_GREATER"
            ],
            "net9.0",
            [] );

        private TemplateTest? _currentTest;
        private string? _sourceCode;
        private string? _expectedTransformedCode;
        private FlowDocument? _coloredSourceCodeDocument;
        private FlowDocument? _compiledTemplateDocument;
        private FlowDocument? _transformedCodeDocument;
        private FlowDocument? _intermediateLinkerCodeCodeDocument;
        private FlowDocument? _errorsDocument;
        private DetailPaneContent _detailPaneContent;
        private string? _actualProgramOutput;
        private string? _expectedProgramOutput;

        public string Title => this.CurrentPath == null ? "Aspect Workbench" : $"Aspect Workbench - {this.CurrentPath}";

        public string? SourceCode
        {
            get => this._sourceCode;
            set => this.SetField( ref this._sourceCode, value );
        }

        public string? ExpectedTransformedCode
        {
            get => this._expectedTransformedCode;
            set => this.SetField( ref this._expectedTransformedCode, value );
        }

        public FlowDocument? ColoredSourceCodeDocument
        {
            get => this._coloredSourceCodeDocument;
            set => this.SetField( ref this._coloredSourceCodeDocument, value );
        }

        public FlowDocument? CompiledTemplateDocument
        {
            get => this._compiledTemplateDocument;
            set => this.SetField( ref this._compiledTemplateDocument, value );
        }
        
        public FlowDocument? TransformedCodeDocument
        {
            get => this._transformedCodeDocument;
            set => this.SetField( ref this._transformedCodeDocument, value );
        }

        public FlowDocument? IntermediateLinkerCodeCodeDocument
        {
            get => this._intermediateLinkerCodeCodeDocument;
            set => this.SetField( ref this._intermediateLinkerCodeCodeDocument, value );
        }

        public FlowDocument? ErrorsDocument
        {
            get => this._errorsDocument;
            set => this.SetField( ref this._errorsDocument, value );
        }

        public bool IsNewTest => string.IsNullOrEmpty( this.CurrentPath );

        private string? CurrentPath { get; set; }

        public DetailPaneContent DetailPaneContent
        {
            get => this._detailPaneContent;
            set
            {
                if ( this.SetField( ref this._detailPaneContent, value ) )
                {
                    this.OnPropertyChanged( nameof(this.CompiledTemplateVisibility) );
                    this.OnPropertyChanged( nameof(this.ProgramOutputVisibility) );
                    this.OnPropertyChanged( nameof(this.IntermediateLinkerCodeVisibility) );
                    this.OnPropertyChanged( nameof(this.HighlightedTemplateVisibility) );
                }
            }
        }

        public string? ActualProgramOutput
        {
            get => this._actualProgramOutput;
            private set => this.SetField( ref this._actualProgramOutput, value );
        }

        public string? ExpectedProgramOutput
        {
            get => this._expectedProgramOutput;
            set => this.SetField( ref this._expectedProgramOutput, value );
        }

        public Visibility CompiledTemplateVisibility
            => this.DetailPaneContent == DetailPaneContent.CompiledTemplate ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ProgramOutputVisibility => this.DetailPaneContent == DetailPaneContent.ProgramOutput ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IntermediateLinkerCodeVisibility
            => this.DetailPaneContent == DetailPaneContent.IntermediateLinkerCode ? Visibility.Visible : Visibility.Collapsed;

        public Visibility HighlightedTemplateVisibility
            => this.DetailPaneContent == DetailPaneContent.HighlightedTemplate ? Visibility.Visible : Visibility.Collapsed;

        public async Task RunTestAsync()
        {
            if ( this.SourceCode == null )
            {
                throw new InvalidOperationException( $"Property {nameof(this.SourceCode)} not set." );
            }
            else if ( this.CurrentPath == null )
            {
                throw new InvalidOperationException( "The test must be saved before you can run it." );
            }

            this.ErrorsDocument = new FlowDocument();
            this.TransformedCodeDocument = null;

            var testInput = TestInput.Factory.Default.FromSource( _projectProperties, this.SourceCode, this.CurrentPath );

            // This is a dirty trick. We should read options from the directory instead.
            if ( this.SourceCode.Contains( "[TestTemplate]", StringComparison.Ordinal ) )
            {
                testInput.Options.TestRunnerFactoryType = typeof(TemplatingTestRunnerFactory).AssemblyQualifiedName;
            }

            var testContextOptions =
                new TestContextOptions()
                {
                    FormatCompileTimeCode = testInput.Options.FormatCompileTimeCode ?? true,
                    AdditionalMetadataReferences = [MetadataReference.CreateFromFile( typeof(TestTemplateAttribute).Assembly.Location )]
                };

            using var testContext = new TestContext( testContextOptions );

            var serviceProvider = testContext.ServiceProvider;

            var syntaxColorizer = new SyntaxColorizer( serviceProvider );

            var testRunner = TestRunnerFactory.CreateTestRunner(
                testInput,
                serviceProvider,
                new TestProjectReferences(
                    testContext.GetMetadataReferences().ToImmutableArray(),
                    ImmutableArray<TestAssemblyReference>.Empty,
                    ImmutableArray<TestAssemblyReference>.Empty,
                    ImmutableArray<string>.Empty, 
                    null ),
                null );

            var compilationStopwatch = Stopwatch.StartNew();
            TestResult? testResult = null;
            Exception? exception = null;

            try
            {
                try
                {
                    testResult = await testRunner.RunAsync( testInput, testContext );
                }
                catch ( Exception e )
                {
                    exception = e;
                }
                finally
                {
                    compilationStopwatch.Stop();
                }

                var errorsDocument = new FlowDocument();

                var testSyntaxTree = testResult?.SyntaxTrees.FirstOrDefault();

                if ( testSyntaxTree != null )
                {
                    var annotatedTemplateSyntax = testSyntaxTree.AnnotatedSyntaxRoot;

                    if ( annotatedTemplateSyntax != null )
                    {
                        // Display the annotated syntax tree.
                        this.ColoredSourceCodeDocument = await syntaxColorizer.WriteSyntaxColoringAsync(
                            testResult.SyntaxTrees.First().InputDocument,
                            diagnostics: testResult.Diagnostics );
                    }

                    var transformedTemplateSyntax = testSyntaxTree.OutputCompileTimeSyntaxRoot;

                    if ( transformedTemplateSyntax != null )
                    {
                        // Render the transformed tree.
                        var project3 = testRunner.CreateProject( testContext, testInput.Options );

                        var document3 = project3.AddDocument(
                            testSyntaxTree.OutputCompileTimePath ?? "TransformedTemplate.cs",
                            transformedTemplateSyntax,
                            filePath: testSyntaxTree.OutputCompileTimePath );

                        if ( testInput.Options.FormatCompileTimeCode != false )
                        {
                            var codeFormatter = serviceProvider.GetRequiredService<CodeFormatter>();
                            var formattedDocument3 = await codeFormatter.FormatAsync( document3, testResult.CompileTimeCompilationDiagnostics );

                            this.CompiledTemplateDocument = await syntaxColorizer.WriteSyntaxColoringAsync( formattedDocument3, true );
                        }
                        else
                        {
                            this.CompiledTemplateDocument = await syntaxColorizer.WriteSyntaxColoringAsync( document3, true );
                        }

                        if ( testResult.CompileTimeCompilation != null )
                        {
                            if ( !SyntaxTreeStructureVerifier.VerifyMetaSyntax( testResult.CompileTimeCompilation, serviceProvider ) )
                            {
                                testResult.SetFailed(
                                    "The compiled template syntax tree object model is incorrect: roundloop parsing verification failed. Add a breakpoint in SyntaxTreeStructureVerifier.Verify and diff manually." );
                            }
                        }
                    }
                }

                testResult.BuildSyntaxTreesForComparison();

                var syntaxTreesForComparison = testResult.SyntaxTrees
                    .Where( t => t.OutputRunTimeSyntaxTreeForComparison != null )
                    .ToList();

                // Multi file tests are not supported.
                switch ( syntaxTreesForComparison.Count )
                {
                    case 0:
                        errorsDocument.Blocks.Add( new Paragraph( new Run( "The test did not produce any output." ) { Foreground = Brushes.Red } ) );

                        return;

                    case > 1:
                        errorsDocument.Blocks.Add( new Paragraph( new Run( "The test did not produce more than one output." ) { Foreground = Brushes.Red } ) );

                        return;
                }

                var consolidatedOutputSyntax = await syntaxTreesForComparison[0].OutputRunTimeSyntaxTreeForComparison!.GetRootAsync();

                if ( !testInput.Options.FormatOutput.GetValueOrDefault() )
                {
                    consolidatedOutputSyntax = consolidatedOutputSyntax.NormalizeWhitespace();
                }

                var consolidatedOutputText = await consolidatedOutputSyntax.SyntaxTree.GetTextAsync();

                var project = testResult.OutputProject ?? testResult.InputProject;

                if ( project != null )
                {
                    var consolidatedOutputDocument = project.AddDocument( "ConsolidatedOutput.cs", consolidatedOutputSyntax );

                    // Display the transformed code.
                    this.TransformedCodeDocument = await syntaxColorizer.WriteSyntaxColoringAsync( consolidatedOutputDocument );
                }

                // Display the intermediate linker code.
                if ( testResult.IntermediateLinkerCompilation != null )
                {
                    var intermediateSyntaxTree = testResult.IntermediateLinkerCompilation.Compilation.SyntaxTrees.First();

                    intermediateSyntaxTree = intermediateSyntaxTree.WithRootAndOptions(
                        (await intermediateSyntaxTree.GetRootAsync()).NormalizeWhitespace(),
                        intermediateSyntaxTree.Options );

                    var linkerProject = testRunner.CreateProject( testContext, testInput.Options );

                    var linkerDocument = linkerProject.AddDocument(
                        "IntermediateLinkerCode.cs",
                        RenderAspectReferences( await intermediateSyntaxTree.GetRootAsync() ) );

                    this.IntermediateLinkerCodeCodeDocument = await syntaxColorizer.WriteSyntaxColoringAsync( linkerDocument );
                }

                if ( exception != null )
                {
                    errorsDocument.Blocks.Add( new Paragraph( new Run( exception.ToString() ) { Foreground = Brushes.Red } ) );
                }
                else
                {
                    // Compare the output and shows the result.
                    if ( TestOutputNormalizer.NormalizeTestOutput( this.ExpectedTransformedCode, false, true ) ==
                         TestOutputNormalizer.NormalizeTestOutput( consolidatedOutputText.ToString(), false, true ) )
                    {
                        errorsDocument.Blocks.Add(
                            new Paragraph( new Run( "The transformed target code is equal to expectations." ) { Foreground = Brushes.Green } ) );
                    }
                    else
                    {
                        errorsDocument.Blocks.Add(
                            new Paragraph( new Run( "The transformed target code is different than expectations." ) { Foreground = Brushes.Red } ) );
                    }
                }

                var errors = testResult.Diagnostics;

                errorsDocument.Blocks.AddRange(
                    errors.Select(
                        e => new Paragraph(
                            new Run( e.ToString() )
                            {
                                Foreground = e.Severity switch
                                {
                                    DiagnosticSeverity.Error => Brushes.Red,
                                    DiagnosticSeverity.Warning => Brushes.Chocolate,
                                    _ => Brushes.Black
                                }
                            } ) ) );

                if ( !string.IsNullOrEmpty( testResult.ErrorMessage ) )
                {
                    errorsDocument.Blocks.Add( new Paragraph( new Run( testResult.ErrorMessage ) { Foreground = Brushes.Red } ) );
                }

                this.ErrorsDocument = errorsDocument;

                this.ActualProgramOutput = testResult.ProgramOutput;

                if ( this.ActualProgramOutput == this.ExpectedProgramOutput )
                {
                    errorsDocument.Blocks.Add( new Paragraph( new Run( "The program output is equal to expectations." ) { Foreground = Brushes.Green } ) );
                }
                else
                {
                    errorsDocument.Blocks.Add(
                        new Paragraph( new Run( "The program output code is different than expectations." ) { Foreground = Brushes.Red } ) );
                }
            }
            finally
            {
                testResult?.Dispose();
            }
        }

        private static SyntaxNode RenderAspectReferences( SyntaxNode rootNode ) => new AnnotationRenderingRewriter().Visit( rootNode ).AssertNotNull();

        public void NewTest( string path )
        {
            var projectDirectory = TestInput.Factory.Default.FromSource( _projectProperties, "", path ).SourceDirectory;
            var pathParts = Path.GetRelativePath( projectDirectory, path ).Split( "\\" ).SelectAsImmutableArray( Path.GetFileNameWithoutExtension ).Skip( 1 );
            var ns = Path.GetFileName( projectDirectory ) + "." + string.Join( ".", pathParts );
            this.SourceCode = NewTestDefaults.TemplateSource.Replace( "$ns", ns, StringComparison.OrdinalIgnoreCase );
            this.ExpectedTransformedCode = null;
            this.CompiledTemplateDocument = null;
            this.TransformedCodeDocument = null;
            this.ExpectedProgramOutput = null;
            this.ActualProgramOutput = null;
            this.CurrentPath = path;
        }

        public async Task LoadTestAsync( string filePath )
        {
            this._currentTest = await TestSerializer.LoadFromFileAsync( _projectProperties, filePath );

            var input = this._currentTest.Input ?? throw new InvalidOperationException( $"The {nameof(this._currentTest.Input)} property cannot be null." );

            this.SourceCode = input.SourceCode;
            this.ExpectedTransformedCode = this._currentTest.ExpectedTransformedCode;
            this.ExpectedProgramOutput = this._currentTest.ExpectedProgramOutput;
            this.CompiledTemplateDocument = null;
            this.TransformedCodeDocument = null;
            this.ActualProgramOutput = null;
            this.CurrentPath = filePath;
        }

        public async Task SaveTestAsync( string? filePath )
        {
            filePath ??= this.CurrentPath ?? throw new ArgumentNullException( nameof(filePath) );

            if ( string.IsNullOrEmpty( filePath ) )
            {
                throw new ArgumentException( "The path cannot be null or empty." );
            }

            if ( string.IsNullOrEmpty( this.SourceCode ) )
            {
                throw new InvalidOperationException( $"The {nameof(this.SourceCode)} property cannot be null." );
            }

            this._currentTest ??= new TemplateTest();

            this._currentTest.Input = TestInput.Factory.Default.FromSource( _projectProperties, this.SourceCode, filePath );
            this._currentTest.ExpectedTransformedCode = this.ExpectedTransformedCode ?? string.Empty;
            this._currentTest.ExpectedProgramOutput = this.ExpectedProgramOutput ?? string.Empty;

            this.CurrentPath = filePath;

            await TestSerializer.SaveToFileAsync( this._currentTest, this.CurrentPath );
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
        {
            this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        private bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
        {
            if ( EqualityComparer<T>.Default.Equals( field, value ) )
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged( propertyName );

            return true;
        }
    }
}