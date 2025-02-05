// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Represents the test results for a syntax tree in <see cref="TestResult"/>.
    /// </summary>
    [PublicAPI]
    public sealed class TestSyntaxTree
    {
        private readonly TestResult _parent;
        
        private TestSyntaxTree( string? inputPath, Document? inputDocument, TestResult parent, SyntaxTree? inputSyntaxTree )
        {
            this.InputDocument = inputDocument;
            this._parent = parent;
            this.InputPath = inputPath;
            this.InputSyntaxTree = inputSyntaxTree;
        }

        internal static async Task<TestSyntaxTree> CreateAsync( string? inputPath, Document document, TestResult parent )
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();

            return new TestSyntaxTree( inputPath, document, parent, syntaxTree.AssertNotNull() );
        }

        internal static TestSyntaxTree CreateIntroduced( TestResult parent )
        {
            return new TestSyntaxTree( null, null, parent, null );
        }

        [Memo]
        public TestSyntaxTreeKind Kind
        {
            get
            {
                var fileName = Path.GetFileName( this.FilePath );

                if ( fileName.StartsWith( "_", StringComparison.Ordinal ) || fileName.EndsWith( ".Aspect.cs", StringComparison.Ordinal ) )
                {
                    return TestSyntaxTreeKind.Auxiliary;
                }
                else if ( fileName.StartsWith( "@@", StringComparison.Ordinal ) )
                {
                    return TestSyntaxTreeKind.Helper;
                }
                else if ( this.InputPath == null )
                {
                    return TestSyntaxTreeKind.Introduced;
                }
                else
                {
                    return TestSyntaxTreeKind.Default;
                }
            }
        }

        /// <summary>
        /// Gets the file path of the syntax tree. For input syntax trees, this is an absolute path of the input document.
        /// For introduced syntax trees, this is a relative path under which the syntax tree was introduced. 
        /// </summary>
        public string FilePath
            => this.InputPath ?? this.OutputDocument?.FilePath ?? throw new InvalidOperationException( "The test syntax tree does not have a path." );

        /// <summary>
        /// Gets the filename without extension of the syntax tree, shortened if its total length exceeds 20 characters. 
        /// </summary>
        [Memo]
        public string ShortName
        {
            get
            {
                var fileName = Path.GetFileNameWithoutExtension( this.FilePath );
                
                if ( this.Kind is TestSyntaxTreeKind.Introduced )
                {
                    var nameParts = fileName.Split( '.' );
                    fileName = nameParts[^1];
            
                    return this._parent.TestInput.AssertNotNull().TestName + "." + fileName;
                }

                return fileName;
            }
        }

        /// <summary>
        /// Gets the input path from which the syntax tree was loaded. For introduced syntax trees, this is <c>null</c>.
        /// </summary>
        public string? InputPath { get; }

        /// <summary>
        /// Gets the input <see cref="Document" />. For introduced syntax trees, this is <c>null</c>.
        /// </summary>
        public Document? InputDocument { get; }

        /// <summary>
        /// Gets the output document.
        /// </summary>
        public Document? OutputDocument { get; private set; }

        /// <summary>
        /// Gets the input <see cref="SyntaxTree" />. For introduced syntax trees, this is <c>null</c>.
        /// </summary>
        public SyntaxTree? InputSyntaxTree { get; }

        /// <summary>
        /// Gets the root <see cref="SyntaxNode" /> of the output run-time syntax tree.
        /// </summary>
        public CompilationUnitSyntax? OutputRunTimeSyntaxRoot { get; private set; }

        public SyntaxTree? OutputRunTimeSyntaxTreeForComparison { get; internal set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global

        /// <summary>
        /// Gets the root <see cref="SyntaxNode" /> of the output compile-time syntax tree.
        /// </summary>
        public SyntaxNode? OutputCompileTimeSyntaxRoot { get; private set; }

        /// <summary>
        /// Gets or sets the root <see cref="SyntaxNode" /> for the annotated syntax tree (before transformation). This is
        /// useful for syntax highlighting.
        /// </summary>
        public SyntaxNode? AnnotatedSyntaxRoot { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global

        /// <summary>
        /// Gets the full path of the code for the output compile-time syntax tree.
        /// </summary>
        public string? OutputCompileTimePath { get; private set; }

        public string? HtmlInputPath { get; internal set; }

        public string? HtmlOutputPath { get; internal set; }

        public string? ExpectedTransformedCodeText { get; private set; }

        public string? ActualTransformedNormalizedCodeText { get; private set; }

        public string? ActualTransformedSourceTextForStorage { get; private set; }

        public string? ActualTransformedCodePath { get; private set; }

        public string? ExpectedTransformedCodePath { get; private set; }

        internal void SetCompileTimeCode( SyntaxNode? syntaxNode, string transformedTemplatePath )
        {
            if ( syntaxNode != null )
            {
                if ( this.InputDocument == null )
                {
                    throw new AssertionFailedException( "Introduced syntax trees cannot have compile-time code." );
                }

                var formattedOutput = Formatter.Format( syntaxNode, this.InputDocument.Project.Solution.Workspace );
                this.OutputCompileTimeSyntaxRoot = formattedOutput;
                this.OutputCompileTimePath = transformedTemplatePath;
            }
        }

        internal async Task SetRunTimeCodeAsync( SyntaxNode syntaxNode )
        {
            CompilationUnitSyntax compilationUnit;

            switch ( syntaxNode )
            {
                case CompilationUnitSyntax cu:
                    compilationUnit = cu;

                    break;

                case MemberDeclarationSyntax member:
                    compilationUnit = SyntaxFactory.CompilationUnit().WithMembers( SyntaxFactory.SingletonList( member ) );

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(syntaxNode),
                        $"The root of the document must be a CompilationUnitSyntax or a MemberDeclarationSyntax but it is a {syntaxNode.Kind()}." );
            }

            this._parent.OutputProject ??= this._parent.InputProject;

            Document outputDocument;

            if ( this.InputDocument != null )
            {
                var documentName = Path.GetFileName( this.InputDocument.FilePath )!;

                outputDocument =
                    this._parent.OutputProject!.RemoveDocument( this.InputDocument.Id )
                        .AddDocument( documentName, compilationUnit );

                this._parent.OutputProject = outputDocument.Project;
            }
            else
            {
                outputDocument = this._parent.OutputProject!.AddDocument(
                    syntaxNode.SyntaxTree.FilePath,
                    compilationUnit,
                    filePath: syntaxNode.SyntaxTree.FilePath );

                this._parent.OutputProject = outputDocument.Project;
            }

            if ( this._parent.TestInput!.Options.FormatOutput.GetValueOrDefault() )
            {
                var codeFormatter = new CodeFormatter();

                this.OutputDocument = await codeFormatter.FormatAsync( outputDocument );
                this.OutputRunTimeSyntaxRoot = (CompilationUnitSyntax) (await this.OutputDocument.GetSyntaxRootAsync())!;
            }
            else
            {
                this.OutputDocument = outputDocument;
                this.OutputRunTimeSyntaxRoot = compilationUnit;
            }
        }

        internal void SetTransformedSource(
            string? expectedTransformedSourceText,
            string? expectedTransformedSourcePath,
            string? actualTransformedNormalizedSourceText,
            string? actualTransformedSourceTextForStorage,
            string? actualTransformedSourcePath )
        {
            if ( this.Kind is not (TestSyntaxTreeKind.Default or TestSyntaxTreeKind.Introduced) )
            {
                throw new InvalidOperationException();
            }

            this.ExpectedTransformedCodeText = expectedTransformedSourceText;
            this.ExpectedTransformedCodePath = expectedTransformedSourcePath;
            this.ActualTransformedNormalizedCodeText = actualTransformedNormalizedSourceText;
            this.ActualTransformedSourceTextForStorage = actualTransformedSourceTextForStorage;
            this.ActualTransformedCodePath = actualTransformedSourcePath;
        }

        public override string ToString() => this.FilePath;
    }
}