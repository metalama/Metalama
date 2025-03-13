// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable AccessToModifiedClosure

namespace Metalama.Framework.Engine.Formatting;

public sealed class HtmlCodeWriter : FormattedCodeWriter
{
    private readonly HtmlCodeWriterOptions _options;

    private static readonly Regex _cleanAutomaticPropertiesRegex =
        new( @"\{(\s*(private|internal|protected|private protected|protected internal)?\s*[gs]et;\s*){1,2}\}" );

    private static readonly Regex _cleanReturnStatementRegex = new( @"(?<=^\s*)return(?=\s*[^\;])" );

    public HtmlCodeWriter( in ProjectServiceProvider serviceProvider, HtmlCodeWriterOptions options ) : base( serviceProvider )
    {
        this._options = options;
    }

    public Task WriteAsync( Document document, TextWriter textWriter, IEnumerable<Diagnostic>? diagnostics = null )
        => this.WriteAsync( document, textWriter, diagnostics, null );

    public async Task WriteDiffAsync(
        Document inputDocument,
        Document outputDocument,
        TextWriter inputTextWriter,
        TextWriter outputTextWriter,
        IEnumerable<Diagnostic> inputDiagnostics )
    {
        var oldSyntaxTree = await inputDocument.GetSyntaxTreeAsync();
        var newSyntaxTree = await outputDocument.GetSyntaxTreeAsync();
        await this.WriteAsync( inputDocument, inputTextWriter, inputDiagnostics, GetDiffInfo( oldSyntaxTree!, newSyntaxTree!, true ) );
        await this.WriteAsync( outputDocument, outputTextWriter, null, GetDiffInfo( oldSyntaxTree!, newSyntaxTree!, false ) );
    }

    private async Task WriteAsync( Document document, TextWriter textWriter, IEnumerable<Diagnostic>? diagnostics, FileDiffInfo? diffInfo )
    {
        var sourceText = await document.GetTextAsync( CancellationToken.None );
        var syntaxRoot = (await document.GetSyntaxRootAsync()).AssertNotNull();

        var classifiedTextSpans = await this.GetClassifiedTextSpansAsync( document, addTitles: this._options.AddTitles, diagnostics: diagnostics );

        var finalBuilder = new StringBuilder();      // Builds the whole file.
        var codeLineBuilder = new StringBuilder();   // Builds the current line.
        var diagnosticBuilder = new StringBuilder(); // Builds the diagnostics of the current line.
        var diagnosticSet = new HashSet<string>( StringComparer.Ordinal );
        var lineNumber = 0;

        void AppendEmptyDiffLine( int? number )
        {
            if ( number != null && number != 0 )
            {
                finalBuilder.AppendLineInvariant( $"<span class='diff-Imaginary' data-height='{number}'> " );

                for ( var i = 1; i < number.Value; i++ )
                {
                    finalBuilder.AppendLine();
                }

                finalBuilder.Append( "</span>" );
            }
        }

        void FlushLine( TextSpan span )
        {
            LineDiffInfo? lineDiffInfo;

            if ( diffInfo is { Lines.Length: > 0 } )
            {
                lineDiffInfo = diffInfo.Lines[lineNumber];
            }
            else
            {
                lineDiffInfo = null;
            }

            // First write the imaginary diff lines before.
            AppendEmptyDiffLine( lineDiffInfo?.ImaginaryLinesBefore );

            // Then write the diagnostics.
            if ( diagnosticBuilder.Length > 0 )
            {
                // Figure out the indentation of the next block.
                var indentation = "";
                var isTag = false;

                foreach ( var c in codeLineBuilder.ToString() )
                {
                    switch ( c )
                    {
                        case '<':
                            isTag = true;

                            break;

                        case '>':
                            isTag = false;

                            break;

                        case ' ' or '\t' when !isTag:
                            indentation += c;

                            break;

                        default:
                            if ( !isTag )
                            {
                                goto parsingIndentationFinished;
                            }
                            else
                            {
                                break;
                            }
                    }
                }

            parsingIndentationFinished:

                // Write any buffered diagnostic.
                var diagnosticLines = diagnosticBuilder.ToString().Split( '\n' );

                finalBuilder.Append( "<span class=\"diagLines\">" );

                foreach ( var diagnostic in diagnosticLines )
                {
                    finalBuilder.Append( indentation );
                    finalBuilder.Append( diagnostic );
                    finalBuilder.Append( '\n' );
                }

                finalBuilder.Append( "</span>" );

                diagnosticBuilder.Clear();
                diagnosticSet.Clear();
            }

            // Find the member at the line number.
            var line = sourceText.Lines.GetLineFromPosition( span.Start );
            var node = syntaxRoot.FindNode( line.Span, getInnermostNodeForTie: true );

            // Moved to a local function due to Roslyn bug that results in assertion failure in Metalama.Compiler debug build: https://github.com/dotnet/roslyn/issues/69015
            var members = node.AncestorsAndSelf()
                .Select( GetMemberTextPair )
                .Where( x => x.Node != null )
                .ToMutableList();

            static (SyntaxNode? Node, string? Text) GetMemberTextPair( SyntaxNode n )
            {
                return n switch
                {
                    MethodDeclarationSyntax method => (method, method.Identifier.Text),
                    BaseFieldDeclarationSyntax field => (field, field.Declaration.Variables[0].Identifier.Text),
                    EventDeclarationSyntax @event => (@event, @event.Identifier.Text),
                    BaseTypeDeclarationSyntax type => (type, type.Identifier.Text),
                    PropertyDeclarationSyntax property => (property, property.Identifier.Text),
                    _ => (null, null)
                };
            }

            finalBuilder.AppendInvariant( $"<span class='line-number'" );

            if ( members.Count > 0 )
            {
                var topNode = members[0].Node;

                var syntaxToken = topNode!.GetFirstToken();

                if ( (line.End < syntaxToken.Span.Start || line.Start > topNode.GetLastToken().Span.End)
                     && string.IsNullOrWhiteSpace( sourceText.GetSubText( line.Span ).ToString() ) )
                {
                    // This is a blank line.
                }
                else
                {
                    members.Reverse();
                    finalBuilder.AppendInvariant( $" data-member='{string.Join( ".", members.SelectAsReadOnlyList( x => x.Text! ) )}'" );
                }
            }

            finalBuilder.AppendInvariant( $">{lineNumber + 1}</span>" );
            finalBuilder.AppendLine( codeLineBuilder.ToString() );
            codeLineBuilder.Clear();

            // Write the diff lines after, if any.
            AppendEmptyDiffLine( lineDiffInfo?.ImaginaryLinesAfter );

            lineNumber++;
        }

        if ( this._options.Prolog != null )
        {
            finalBuilder.Append( this._options.Prolog );
        }

        finalBuilder.Append( "<pre><code class=\"nohighlight\">" );

        var isTopOfTheFile = true;

        foreach ( var classifiedSpan in classifiedTextSpans )
        {
            // Write the text between the previous span and the current one.
            var textSpan = classifiedSpan.Span;

            var subText = sourceText.GetSubText( textSpan );
            var spanText = subText.ToString();

            // Ignore blank lines on the top of the file.
            if ( spanText.Trim().Length == 0 && isTopOfTheFile )
            {
                continue;
            }

            if ( classifiedSpan.Classification != TextSpanClassification.Excluded )
            {
                List<string> classes = new();
                List<string> titles = new();

                const bool isLeadingTrivia = false; // string.IsNullOrWhiteSpace( spanText ) && (spanText[0] == '\r' || spanText[0] == '\n');

                if ( !isLeadingTrivia )
                {
                    if ( classifiedSpan.Classification != TextSpanClassification.Default )
                    {
                        classes.Add( $"cr-{classifiedSpan.Classification}" );
                    }

                    if ( classifiedSpan.Tags.TryGetValue( CSharpClassTagName, out var csClassification ) )
                    {
                        // Ignore the header.
                        if ( csClassification == "header" )
                        {
                            continue;
                        }

                        foreach ( var classification in csClassification.Split( ';' ) )
                        {
                            foreach ( var c in classification.Split( '-' ) )
                            {
                                classes.Add( "cs-" + c.Trim().ReplaceOrdinal( " ", "-" ) );
                            }
                        }
                    }

                    isTopOfTheFile = false;

                    if ( classifiedSpan.Tags.TryGetValue( DiagnosticTagName, out var diagnosticJson ) )
                    {
                        var diagnostic = DiagnosticAnnotation.FromJson( diagnosticJson );

                        if ( diagnostic.Severity != DiagnosticSeverity.Hidden && diagnosticSet.Add( diagnostic.Message ) )
                        {
                            titles.Add( diagnostic.ToString() );

                            classes.Add( "diag-" + diagnostic.Severity );

                            diagnosticBuilder.AppendInvariant( $"<span class=\"diagLine-{diagnostic.Severity}\">{diagnostic.Severity} {diagnostic.Id}: " );

                            HtmlEncode( diagnosticBuilder, textSpan, diagnostic.Message );
                            diagnosticBuilder.Append( "</span>\n" );
                        }
                    }

                    string? docTitle = null;

                    if ( this._options.AddTitles && !classifiedSpan.Tags.TryGetValue( "title", out docTitle ) )
                    {
                        docTitle = classifiedSpan.Classification switch
                        {
                            TextSpanClassification.Dynamic => "Dynamic member.",
                            TextSpanClassification.CompileTime => "Compile-time code.",
                            TextSpanClassification.RunTime => "Run-time code.",
                            TextSpanClassification.TemplateKeyword => "Meta API.",
                            TextSpanClassification.CompileTimeVariable => "Compile-time variable.",
                            TextSpanClassification.GeneratedCode when classifiedSpan.Tags.TryGetValue( GeneratingAspectTagName, out var aspect ) =>
                                $"Generated by {aspect}.",
                            TextSpanClassification.GeneratedCode => "Generated code.",
                            _ => null
                        };
                    }

                    if ( docTitle != null )
                    {
                        titles.Insert( 0, docTitle );
                    }
                }

                if ( classes.Count > 0 || titles.Count > 0 )
                {
                    codeLineBuilder.Append( "<span" );

                    if ( classes.Count > 0 )
                    {
                        codeLineBuilder.AppendInvariant( $" class=\"{string.Join( " ", classes )}\"" );
                    }

                    if ( titles.Count > 0 )
                    {
                        codeLineBuilder.Append( " title=\"" );

                        for ( var i = 0; i < titles.Count; i++ )
                        {
                            if ( i > 0 )
                            {
                                codeLineBuilder.Append( "&#13;&#10;" );
                            }

                            HtmlEncode( codeLineBuilder, textSpan, titles[i], true );
                        }

                        codeLineBuilder.Append( "\"" );
                    }

                    codeLineBuilder.Append( ">" );
                    HtmlEncode( codeLineBuilder, textSpan, spanText, onNewLine: FlushLine );
                    codeLineBuilder.Append( "</span>" );
                }
                else
                {
                    HtmlEncode( codeLineBuilder, textSpan, spanText, onNewLine: FlushLine );
                }
            }
        }

        FlushLine( default );

        finalBuilder.AppendLine( "</code></pre>" );

        if ( this._options.Epilogue != null )
        {
            finalBuilder.Append( this._options.Epilogue );
        }

        await textWriter.WriteAsync( finalBuilder.ToString() );
    }

    private static void HtmlEncode(
        StringBuilder stringBuilder,
        TextSpan span,
        string text,
        bool attributeEncode = false,
        Action<TextSpan>? onNewLine = null )
    {
        foreach ( var c in text )
        {
            switch ( c )
            {
                case '<':
                    stringBuilder.Append( "&lt;" );

                    break;

                case '>':
                    stringBuilder.Append( "&gt;" );

                    break;

                case '&':
                    stringBuilder.Append( "&amp;" );

                    break;

                case '"' when attributeEncode:
                    stringBuilder.Append( "&quot;" );

                    break;

                case '\r':
                    // Always ignored.
                    break;

                case '\n' when attributeEncode:
                    stringBuilder.Append( "&#10;" );

                    break;

                case '\n':
                    if ( onNewLine == null )
                    {
                        stringBuilder.Append( c );
                    }
                    else
                    {
                        onNewLine( span );
                    }

                    break;

                default:
                    stringBuilder.Append( c );

                    break;
            }
        }
    }

    private enum SyntaxTreeKind
    {
        Source,
        Transformed,
        Introduced
    }

    private static async Task WriteAllAsync(
        IProjectOptions projectOptions,
        ProjectServiceProvider serviceProvider,
        PartialCompilation partialCompilation,
        Func<string, SyntaxTreeKind> getSyntaxTreeKind,
        Func<string, FileDiffInfo?>? getDiffInfo = null,
        bool includeDiagnostics = false,
        IEnumerable<Diagnostic>? additionalDiagnostics = null )
    {
        var compilation = partialCompilation.Compilation;

        ImmutableDictionaryOfArray<string, Diagnostic>? diagnosticsBySyntaxTree;

        if ( includeDiagnostics )
        {
            additionalDiagnostics ??= [];

            diagnosticsBySyntaxTree = additionalDiagnostics.Concat( compilation.GetDiagnostics() )
                .ToMultiValueDictionary( d => d.Location.SourceTree?.FilePath ?? "", d => d );
        }
        else
        {
            diagnosticsBySyntaxTree = null;
        }

        var writer = new HtmlCodeWriter( serviceProvider, new HtmlCodeWriterOptions( true ) );

        var workspace = new AdhocWorkspace();

        var assemblyName = compilation.AssemblyName.AssertNotNull();

        var projectId = ProjectId.CreateNewId( assemblyName );

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            assemblyName,
            assemblyName,
            "C#",
            projectOptions.ProjectPath,
            compilationOptions: compilation.Options,
            metadataReferences: compilation.References );

        var project = workspace.AddProject( projectInfo );

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            var document = project.AddDocument( syntaxTree.FilePath, await syntaxTree.GetRootAsync(), null, syntaxTree.FilePath );
            project = document.Project;
        }

        var projectDirectory = Path.GetFullPath( Path.GetDirectoryName( projectOptions.ProjectPath )! );
        var outputDirectory = Path.Combine( projectDirectory, "obj", "html", projectOptions.TargetFramework ?? "" );

        foreach ( var document in project.Documents )
        {
            var documentPath = document.FilePath.AssertNotNull();
            var syntaxTreeKind = getSyntaxTreeKind( documentPath );

            var htmlExtension = syntaxTreeKind switch
            {
                SyntaxTreeKind.Introduced => ".i.cs.html",
                SyntaxTreeKind.Source => ".cs.html",
                SyntaxTreeKind.Transformed => ".t.cs.html",
                _ => throw new AssertionFailedException()
            };

            string outputPath;
            FileDiffInfo? diffInfo;

            if ( syntaxTreeKind == SyntaxTreeKind.Introduced )
            {
                diffInfo = null;

                outputPath = Path.Combine( outputDirectory, Path.ChangeExtension( documentPath, htmlExtension ) );
            }
            else
            {
                var documentFullPath = Path.GetFullPath( documentPath );

                if ( !documentFullPath.StartsWith( projectDirectory, StringComparison.OrdinalIgnoreCase ) )
                {
                    // Skipping this document.
                    continue;
                }

                var relativePath = documentFullPath.Substring( projectDirectory.Length + 1 );
                outputPath = Path.Combine( outputDirectory, Path.ChangeExtension( relativePath, htmlExtension ) );

                diffInfo = getDiffInfo?.Invoke( documentPath );
            }

            var outputSubdirectory = Path.GetDirectoryName( outputPath ).AssertNotNull();
            Directory.CreateDirectory( outputSubdirectory );

#if NET6_0_OR_GREATER
            await using var textWriter = new StreamWriter( outputPath );
#else
            using var textWriter = new StreamWriter( outputPath );
#endif

            var diagnostics = includeDiagnostics ? diagnosticsBySyntaxTree![documentPath] : ImmutableArray<Diagnostic>.Empty;

            await writer.WriteAsync( document, textWriter, diagnostics, diffInfo );
        }
    }

    internal static async Task WriteAllDiffAsync(
        IProjectOptions projectOptions,
        ProjectServiceProvider serviceProvider,
        PartialCompilation inputCompilation,
        PartialCompilation outputCompilation,
        IEnumerable<Diagnostic>? additionalDiagnostics = null )
    {
        await WriteAllAsync(
            projectOptions,
            serviceProvider,
            inputCompilation,
            _ => SyntaxTreeKind.Source,
            p => GetDiffInfoForPath( p, true ),
            true,
            additionalDiagnostics );

        await WriteAllAsync(
            projectOptions,
            serviceProvider,
            outputCompilation,
            GetOutputSyntaxTreeKind,
            p => GetDiffInfoForPath( p, false ) );

        FileDiffInfo? GetDiffInfoForPath( string path, bool isOld )
        {
            if ( !inputCompilation.SyntaxTrees.TryGetValue( path, out var oldTree ) )
            {
                return null;
            }

            if ( !outputCompilation.SyntaxTrees.TryGetValue( path, out var newTree ) )
            {
                return null;
            }

            return GetDiffInfo( oldTree, newTree, isOld );
        }

        SyntaxTreeKind GetOutputSyntaxTreeKind( string path )
        {
            if ( inputCompilation.SyntaxTrees.ContainsKey( path ) )
            {
                return SyntaxTreeKind.Transformed;
            }
            else
            {
                return SyntaxTreeKind.Introduced;
            }
        }
    }

    private static FileDiffInfo GetDiffInfo( SyntaxTree oldTree, SyntaxTree newTree, bool isOld )
    {
        // Gets the text that should be compared by the differ. This text has no other role than diffing the lines, and we ignore the in-line changes,
        // so we can do any transformation we want to make the diff cleaner.
        static string GetTextToCompare( SourceText text )
        {
            // We remove automatic accessors of automatic properties because it better matches the property
            // after automatic accessors have been replaced by explicit implementations.
            // We also rewrite all return statements to make it more likely to match.
            return _cleanReturnStatementRegex.Replace( _cleanAutomaticPropertiesRegex.Replace( text.ToString(), "" ), "result = " );
        }

        var diffBuilder = new SideBySideDiffBuilder();

#pragma warning disable VSTHRD103
        var oldText = oldTree.GetText();
        var newText = newTree.GetText();
#pragma warning restore VSTHRD103

        var diff = diffBuilder.BuildDiffModel( GetTextToCompare( oldText ), GetTextToCompare( newText ), true );

        var (text, diffPane) = isOld ? (oldText, diff.OldText) : (newText, diff.NewText);

        var lineDiffInfos = new List<LineDiffInfo>( diffPane.Lines.Count );

        var imaginaryLinesBefore = 0;
        var lineNumber = 0;

        foreach ( var diffLine in diffPane.Lines )
        {
            if ( diffLine.Type == ChangeType.Imaginary )
            {
                imaginaryLinesBefore++;
            }
            else
            {
                var lineText = text.Lines[lineNumber].ToString();

                if ( lineText.Trim() == "{" && imaginaryLinesBefore > 0 )
                {
                    // Prefer to insert imaginary lines after a bracket than before.
                    lineDiffInfos.Add( new LineDiffInfo( 0, 0, diffLine.Type ) );
                }
                else
                {
                    lineDiffInfos.Add( new LineDiffInfo( imaginaryLinesBefore, 0, diffLine.Type ) );
                    imaginaryLinesBefore = 0;
                }

                lineNumber++;
            }
        }

        if ( imaginaryLinesBefore != 0 && lineDiffInfos.Count > 0 )
        {
            lineDiffInfos[^1] = new LineDiffInfo( lineDiffInfos[^1].ImaginaryLinesBefore, imaginaryLinesBefore, lineDiffInfos[^1].ChangeType );
        }

        return new FileDiffInfo( lineDiffInfos.ToArray() );
    }

    private sealed record FileDiffInfo( LineDiffInfo[] Lines );

    private sealed record LineDiffInfo( int ImaginaryLinesBefore, int ImaginaryLinesAfter, ChangeType ChangeType );
}