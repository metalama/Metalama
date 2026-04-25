// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable AccessToModifiedClosure

namespace Metalama.Extensions.HtmlWriter;

/// <summary>
/// Implementation of <see cref="IHtmlCodeWriter"/> that provides HTML code writing functionality.
/// </summary>
internal sealed class HtmlCodeWriter : IHtmlCodeWriter
{
    private static readonly Regex _cleanAutomaticPropertiesRegex =
        new( @"\{(\s*(private|internal|protected|private protected|protected internal)?\s*[gs]et;\s*){1,2}\}" );

    private static readonly Regex _cleanReturnStatementRegex = new( @"(?<=^\s*)return(?=\s*[^\;])" );

    private readonly IFormattedCodeWriter _formattedCodeWriter;

    public HtmlCodeWriter( in ProjectServiceProvider serviceProvider )
    {
        this._formattedCodeWriter = serviceProvider.GetRequiredService<IFormattedCodeWriter>();
    }

    public Task WriteAsync(
        Document document,
        TextWriter textWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? diagnostics = null,
        CancellationToken cancellationToken = default )
        => this.WriteAsync( document, textWriter, options, diagnostics, null, cancellationToken );

    public async Task WriteDiffAsync(
        Document inputDocument,
        Document outputDocument,
        TextWriter inputTextWriter,
        TextWriter outputTextWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? inputDiagnostics,
        IEnumerable<Diagnostic>? outputDiagnostics,
        CancellationToken cancellationToken )
    {
        var oldSyntaxTree = await inputDocument.GetSyntaxTreeAsync( cancellationToken );
        var newSyntaxTree = await outputDocument.GetSyntaxTreeAsync( cancellationToken );

        await this.WriteAsync(
            inputDocument,
            inputTextWriter,
            options,
            inputDiagnostics,
            GetDiffInfo( oldSyntaxTree!, newSyntaxTree!, true ),
            cancellationToken );

        await this.WriteAsync(
            outputDocument,
            outputTextWriter,
            options,
            outputDiagnostics,
            GetDiffInfo( oldSyntaxTree!, newSyntaxTree!, false ),
            cancellationToken );
    }

    private async Task WriteAsync(
        Document document,
        TextWriter textWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? diagnostics,
        FileDiffInfo? diffInfo,
        CancellationToken cancellationToken )
    {
        var sourceText = await document.GetTextAsync( cancellationToken );
        var syntaxRoot = await document.GetSyntaxRootAsync( cancellationToken ) ?? throw new InvalidOperationException( "Document has no syntax root." );

        var classifiedTextSpans = await this._formattedCodeWriter.GetClassifiedTextSpansAsync(
            document,
            addTitles: options.AddTitles,
            diagnostics: diagnostics,
            cancellationToken: cancellationToken );

        var finalBuilder = new StringBuilder();      // Builds the whole file.
        var codeLineBuilder = new StringBuilder();   // Builds the current line.
        var diagnosticBuilder = new StringBuilder(); // Builds the diagnostics of the current line.
        var diagnosticSet = new HashSet<string>( StringComparer.Ordinal );
        var lineNumber = 0;

        void AppendEmptyDiffLine( int? number )
        {
            if ( number != null && number != 0 )
            {
                finalBuilder.AppendLine( FormattableString.Invariant( $"<span class='diff-Imaginary' data-height='{number}'> " ) );

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
                .ToList();

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

            finalBuilder.Append( FormattableString.Invariant( $"<span class='line-number'" ) );

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
                    finalBuilder.Append( FormattableString.Invariant( $" data-member='{string.Join( ".", members.Select( x => x.Text! ) )}'" ) );
                }
            }

            finalBuilder.Append( FormattableString.Invariant( $">{lineNumber + 1}</span>" ) );
            finalBuilder.AppendLine( codeLineBuilder.ToString() );
            codeLineBuilder.Clear();

            // Write the diff lines after, if any.
            AppendEmptyDiffLine( lineDiffInfo?.ImaginaryLinesAfter );

            lineNumber++;
        }

        if ( options.Prolog != null )
        {
            finalBuilder.Append( options.Prolog );
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

                    var csClassification = classifiedSpan.CSharpClassification;

                    if ( csClassification != null )
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
#pragma warning disable CA1307 // Specify StringComparison for clarity - not available in .NET Framework
                                classes.Add( "cs-" + c.Trim().Replace( " ", "-" ) );
#pragma warning restore CA1307
                            }
                        }
                    }

                    isTopOfTheFile = false;

                    var diagnostic = classifiedSpan.Diagnostic;

                    if ( diagnostic != null )
                    {
                        var message = diagnostic.GetMessage( CultureInfo.InvariantCulture );

                        if ( diagnostic.Severity != DiagnosticSeverity.Hidden && diagnosticSet.Add( message ) )
                        {
                            titles.Add( $"{diagnostic.Severity} {diagnostic.Id}: {message}" );

                            classes.Add( "diag-" + diagnostic.Severity );

                            diagnosticBuilder.Append(
                                FormattableString.Invariant( $"<span class=\"diagLine-{diagnostic.Severity}\">{diagnostic.Severity} {diagnostic.Id}: " ) );

                            HtmlEncode( diagnosticBuilder, textSpan, message );
                            diagnosticBuilder.Append( "</span>\n" );
                        }
                    }

                    var docTitle = classifiedSpan.Title;

                    if ( options.AddTitles && docTitle == null )
                    {
                        var generatingAspect = classifiedSpan.GeneratingAspect;

                        docTitle = classifiedSpan.Classification switch
                        {
                            TextSpanClassification.Dynamic => "Dynamic member.",
                            TextSpanClassification.CompileTime => "Compile-time code.",
                            TextSpanClassification.RunTime => "Run-time code.",
                            TextSpanClassification.TemplateKeyword => "Meta API.",
                            TextSpanClassification.CompileTimeVariable => "Compile-time variable.",
                            TextSpanClassification.GeneratedCode when generatingAspect != null =>
                                $"Generated by {generatingAspect}.",
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
                        codeLineBuilder.Append( FormattableString.Invariant( $" class=\"{string.Join( " ", classes )}\"" ) );
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

        if ( options.Epilogue != null )
        {
            finalBuilder.Append( options.Epilogue );
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
        HtmlCodeWriterOptions options,
        IHtmlCodeWriter htmlCodeWriter,
        IPartialCompilation partialCompilation,
        Func<string, SyntaxTreeKind> getSyntaxTreeKind,
        Func<string, FileDiffInfo?>? getDiffInfo = null,
        bool includeDiagnostics = false,
        IEnumerable<Diagnostic>? additionalDiagnostics = null,
        Func<Diagnostic, Compilation, bool>? isSuppressed = null,
        CancellationToken cancellationToken = default )
    {
        var compilation = partialCompilation.Compilation;

        ILookup<string, Diagnostic>? diagnosticsBySyntaxTree;

        if ( includeDiagnostics )
        {
            additionalDiagnostics ??= [];

            var allDiagnostics = additionalDiagnostics.Concat( compilation.GetDiagnostics() );

            // Filter out suppressed diagnostics.
            if ( isSuppressed != null )
            {
                allDiagnostics = allDiagnostics.Where( d => !isSuppressed( d, compilation ) );
            }

            diagnosticsBySyntaxTree = allDiagnostics
                .ToLookup( d => d.Location.SourceTree?.FilePath ?? "" );
        }
        else
        {
            diagnosticsBySyntaxTree = null;
        }

        var workspace = new AdhocWorkspace();

        var assemblyName = compilation.AssemblyName ?? throw new InvalidOperationException( "Compilation has no assembly name." );

        var projectId = ProjectId.CreateNewId( assemblyName );

        var projectPath = options.ProjectPath;

        if ( string.IsNullOrEmpty( projectPath ) )
        {
            throw new InvalidOperationException( "HtmlCodeWriterOptions.ProjectPath must be set when calling WriteAllDiffAsync." );
        }

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            assemblyName,
            assemblyName,
            "C#",
            projectPath,
            compilationOptions: compilation.Options,
            metadataReferences: compilation.References );

        var project = workspace.AddProject( projectInfo );

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            var document = project.AddDocument( syntaxTree.FilePath, await syntaxTree.GetRootAsync( cancellationToken ), null, syntaxTree.FilePath );
            project = document.Project;
        }

        var projectDirectory = Path.GetFullPath( Path.GetDirectoryName( projectPath )! );
        var outputDirectory = Path.Combine( projectDirectory, "obj", "html", options.TargetFramework );

        foreach ( var document in project.Documents )
        {
            var documentPath = document.FilePath ?? throw new InvalidOperationException( "Document has no file path." );
            var syntaxTreeKind = getSyntaxTreeKind( documentPath );

            var htmlExtension = syntaxTreeKind switch
            {
                SyntaxTreeKind.Introduced => ".i.cs.html",
                SyntaxTreeKind.Source => ".cs.html",
                SyntaxTreeKind.Transformed => ".t.cs.html",
                _ => throw new AssertionFailedException()
            };

            string outputPath;

            if ( syntaxTreeKind == SyntaxTreeKind.Introduced )
            {
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

                getDiffInfo?.Invoke( documentPath );
            }

            var outputSubdirectory = Path.GetDirectoryName( outputPath ) ?? throw new InvalidOperationException( "Invalid output path." );
            Directory.CreateDirectory( outputSubdirectory );

#if NET6_0_OR_GREATER
            await using var textWriter = new StreamWriter( outputPath );
#else
            using var textWriter = new StreamWriter( outputPath );
#endif

            var diagnosticsForDocument = includeDiagnostics ? diagnosticsBySyntaxTree![documentPath].ToImmutableArray() : ImmutableArray<Diagnostic>.Empty;

            await htmlCodeWriter.WriteAsync( document, textWriter, options, diagnosticsForDocument, cancellationToken );
        }
    }

    public async Task WriteAllDiffAsync(
        IPartialCompilation inputCompilation,
        IPartialCompilation outputCompilation,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? additionalDiagnostics = null,
        Func<Diagnostic, Compilation, bool>? isSuppressed = null,
        CancellationToken cancellationToken = default )
    {
        await WriteAllAsync(
            options,
            this,
            inputCompilation,
            _ => SyntaxTreeKind.Source,
            p => GetDiffInfoForPath( p, true ),
            true,
            additionalDiagnostics,
            isSuppressed,
            cancellationToken );

        await WriteAllAsync(
            options,
            this,
            outputCompilation,
            GetOutputSyntaxTreeKind,
            p => GetDiffInfoForPath( p, false ),
            cancellationToken: cancellationToken );

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