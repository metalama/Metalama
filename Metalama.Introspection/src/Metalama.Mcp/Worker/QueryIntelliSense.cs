// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Computes completion, quick info, signature help and diagnostics for the query that the user edits in a served page. It
/// analyzes the same source as <see cref="QueryCompiler"/>, with the same references and options, and never runs it.
/// </summary>
/// <remarks>
/// Positions in requests and answers are offsets in the code of the query. They are mapped to the generated source by the
/// offset at which <see cref="QueryCompiler.GenerateSource(string,bool,out int)"/> inserts the code.
/// </remarks>
internal sealed partial class QueryIntelliSense : IDisposable
{
    private const int _maxCompletionItems = 2000;

    // Members of the generated class that are not globals of a query.
    private static readonly ImmutableHashSet<string> _hiddenCompletions =
        ["ExecuteAsync", "MemberwiseClone", "Finalize", QueryCompiler.QueryTypeName];

    private static readonly SymbolDisplayFormat _signatureFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .WithParameterOptions(
            SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue
            | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeExtensionThis );

    private readonly QueryCompiler _compiler;
    private AdhocWorkspace? _workspace;
    private DocumentId? _documentId;
    private LastCompletion? _lastCompletion;

    public QueryIntelliSense( QueryCompiler compiler )
    {
        this._compiler = compiler;
    }

    private sealed record LastCompletion( string Code, int Position, Document Document, CompletionList List );

    private sealed record QueryDocument( Document Document, int CodeStart, int CodeLength )
    {
        public int ToSource( int position ) => this.CodeStart + Math.Clamp( position, 0, this.CodeLength );

        public bool IsInCode( TextSpan span ) => span.Start >= this.CodeStart && span.End <= this.CodeStart + this.CodeLength;
    }

    /// <summary>
    /// Builds the document of the query. The workspace and its project are built once, so that Roslyn reuses the symbols of
    /// the references; each call replaces only the text.
    /// </summary>
    private QueryDocument GetDocument( string code, bool? asExpression = null )
    {
        var isExpression = asExpression ?? QueryCompiler.TryGetExpression( code, out _ );
        var text = isExpression && QueryCompiler.TryGetExpression( code, out var expression ) ? expression : code;

        // As QueryCompiler.CompileCore does for an expression of type void.
        if ( asExpression == false && !text.TrimEnd().EndsWith( ';' ) )
        {
            text += ";";
        }

        var source = QueryCompiler.GenerateSource( text, isExpression, out var codeStart );

        if ( this._workspace == null )
        {
            this._workspace = new AdhocWorkspace( MefHostServices.DefaultHost );

            var project = this._workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "Query",
                    "Query",
                    LanguageNames.CSharp,
                    compilationOptions: QueryCompiler.CompilationOptions,
                    parseOptions: QueryCompiler.ParseOptions,
                    metadataReferences: WithDocumentation( this._compiler.References ) ) );

            this._documentId = this._workspace.AddDocument( project.Id, QueryCompiler.QueryFileName, SourceText.From( "" ) ).Id;
        }

        var solution = this._workspace.CurrentSolution.WithDocumentText( this._documentId!, SourceText.From( source, Encoding.UTF8 ) );

        return new QueryDocument( solution.GetDocument( this._documentId! )!, codeStart, Math.Min( code.Length, text.Length ) );
    }

    /// <summary>
    /// Adds the XML documentation that ships next to an assembly, so that quick info and completion details show summaries.
    /// </summary>
    private static IEnumerable<MetadataReference> WithDocumentation( ImmutableArray<MetadataReference> references )
        => references.Select(
            r => r is PortableExecutableReference { FilePath: { } path } && File.Exists( Path.ChangeExtension( path, ".xml" ) )
                ? MetadataReference.CreateFromFile( path, documentation: XmlDocumentationProvider.CreateFromFile( Path.ChangeExtension( path, ".xml" ) ) )
                : r );

    public async Task<JsonObject> CompleteAsync( string code, int position, CancellationToken cancellationToken )
    {
        var query = this.GetDocument( code );
        var service = CompletionService.GetService( query.Document );
        var list = service == null ? null : await service.GetCompletionsAsync( query.Document, query.ToSource( position ), cancellationToken: cancellationToken );

        if ( list == null || !query.IsInCode( list.Span ) )
        {
            this._lastCompletion = null;

            return new JsonObject { ["items"] = new JsonArray() };
        }

        this._lastCompletion = new LastCompletion( code, position, query.Document, list );

        var items = new JsonArray();
        var index = -1;

        foreach ( var item in list.ItemsList )
        {
            index++;

            if ( _hiddenCompletions.Contains( item.DisplayText ) )
            {
                continue;
            }

            items.Add(
                new JsonObject
                {
                    ["index"] = index,
                    ["label"] = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                    ["insertText"] = item.DisplayText,
                    ["filterText"] = item.FilterText,
                    ["sortText"] = item.SortText,
                    ["kind"] = GetCompletionKind( item.Tags )
                } );

            if ( items.Count >= _maxCompletionItems )
            {
                break;
            }
        }

        return new JsonObject
        {
            ["items"] = items, ["start"] = list.Span.Start - query.CodeStart, ["end"] = list.Span.End - query.CodeStart
        };
    }

    /// <summary>
    /// Describes one item of the last completion list. Descriptions are computed for the selected item only, because a list
    /// can have thousands of items.
    /// </summary>
    public async Task<JsonObject> DescribeCompletionAsync( string code, int position, int index, CancellationToken cancellationToken )
    {
        if ( this._lastCompletion is not { } last || last.Code != code || last.Position != position || index < 0
             || index >= last.List.ItemsList.Count )
        {
            return new JsonObject();
        }

        var service = CompletionService.GetService( last.Document );
        var description = service == null ? null : await service.GetDescriptionAsync( last.Document, last.List.ItemsList[index], cancellationToken );

        if ( description == null || string.IsNullOrWhiteSpace( description.Text ) )
        {
            return new JsonObject();
        }

        var lines = description.Text.Split( '\n', 2 );

        return new JsonObject { ["detail"] = lines[0].TrimEnd( '\r' ), ["documentation"] = lines.Length > 1 ? lines[1].Trim() : null };
    }

    public async Task<JsonObject> HoverAsync( string code, int position, CancellationToken cancellationToken )
    {
        var query = this.GetDocument( code );
        var service = QuickInfoService.GetService( query.Document );
        var info = service == null ? null : await service.GetQuickInfoAsync( query.Document, query.ToSource( position ), cancellationToken );

        if ( info == null || !query.IsInCode( info.Span ) || info.Sections.IsDefaultOrEmpty )
        {
            return new JsonObject();
        }

        var markdown = new StringBuilder();

        foreach ( var section in info.Sections )
        {
            if ( string.IsNullOrWhiteSpace( section.Text ) )
            {
                continue;
            }

            if ( markdown.Length > 0 )
            {
                markdown.Append( "\n\n" );
            }

            if ( section.Kind == QuickInfoSectionKinds.Description )
            {
                markdown.Append( "```csharp\n" ).Append( section.Text ).Append( "\n```" );
            }
            else
            {
                markdown.Append( EscapeMarkdown( section.Text ) );
            }
        }

        return new JsonObject
        {
            ["markdown"] = markdown.ToString(), ["start"] = info.Span.Start - query.CodeStart, ["end"] = info.Span.End - query.CodeStart
        };
    }

    /// <summary>
    /// Computes signature help from the semantic model. The signature help service of Roslyn is internal, and queries call
    /// methods and constructors, which the semantic model covers.
    /// </summary>
    public async Task<JsonObject> SignatureAsync( string code, int position, CancellationToken cancellationToken )
    {
        var query = this.GetDocument( code );
        var sourcePosition = query.ToSource( position );
        var root = await query.Document.GetSyntaxRootAsync( cancellationToken );
        var model = await query.Document.GetSemanticModelAsync( cancellationToken );

        if ( root == null || model == null )
        {
            return new JsonObject();
        }

        var arguments = root.FindToken( Math.Max( sourcePosition - 1, 0 ) )
            .Parent?.AncestorsAndSelf()
            .OfType<ArgumentListSyntax>()
            .FirstOrDefault(
                a => a.OpenParenToken.Span.End <= sourcePosition && (a.CloseParenToken.IsMissing || sourcePosition <= a.CloseParenToken.SpanStart) );

        if ( arguments == null || !query.IsInCode( arguments.OpenParenToken.Span ) )
        {
            return new JsonObject();
        }

        IReadOnlyList<IMethodSymbol> candidates;
        IMethodSymbol? chosen;

        switch ( arguments.Parent )
        {
            case InvocationExpressionSyntax invocation:
                candidates = model.GetMemberGroup( invocation.Expression, cancellationToken ).OfType<IMethodSymbol>().ToList();
                chosen = model.GetSymbolInfo( invocation, cancellationToken ).Symbol as IMethodSymbol;

                break;

            case BaseObjectCreationExpressionSyntax creation:
                var type = model.GetTypeInfo( creation, cancellationToken ).Type as INamedTypeSymbol;
                candidates = type?.InstanceConstructors.Where( c => model.IsAccessible( sourcePosition, c ) ).ToList() ?? [];
                chosen = model.GetSymbolInfo( creation, cancellationToken ).Symbol as IMethodSymbol;

                break;

            default:
                return new JsonObject();
        }

        if ( candidates.Count == 0 )
        {
            return new JsonObject();
        }

        var argumentIndex = arguments.Arguments.GetSeparators().Count( s => s.SpanStart < sourcePosition );
        var signatures = new JsonArray();
        var activeSignature = -1;

        for ( var i = 0; i < candidates.Count; i++ )
        {
            var method = candidates[i];
            signatures.Add( CreateSignature( method ) );

            if ( activeSignature < 0 && (SymbolEqualityComparer.Default.Equals( method, chosen )
                                         || SymbolEqualityComparer.Default.Equals( method.ReducedFrom ?? method.OriginalDefinition, chosen?.ReducedFrom ?? chosen?.OriginalDefinition )) )
            {
                activeSignature = i;
            }
        }

        if ( activeSignature < 0 )
        {
            activeSignature = Math.Max( 0, candidates.ToList().FindIndex( m => m.Parameters.Length > argumentIndex || m.Parameters.LastOrDefault()?.IsParams == true ) );
        }

        return new JsonObject { ["signatures"] = signatures, ["activeSignature"] = activeSignature, ["activeParameter"] = argumentIndex };
    }

    private static JsonObject CreateSignature( IMethodSymbol method )
    {
        var label = new StringBuilder();

        if ( method.MethodKind != MethodKind.Constructor )
        {
            label.Append( method.ReturnType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) ).Append( ' ' );
            label.Append( method.ContainingType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) ).Append( '.' );
            label.Append( method.Name );

            if ( method.IsGenericMethod )
            {
                label.Append( '<' ).Append( string.Join( ", ", method.TypeArguments.Select( t => t.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) ) ) ).Append( '>' );
            }
        }
        else
        {
            label.Append( method.ContainingType.ToDisplayString( SymbolDisplayFormat.MinimallyQualifiedFormat ) );
        }

        label.Append( '(' );
        var parameters = new JsonArray();

        for ( var i = 0; i < method.Parameters.Length; i++ )
        {
            if ( i > 0 )
            {
                label.Append( ", " );
            }

            var start = label.Length;
            label.Append( method.Parameters[i].ToDisplayString( _signatureFormat ) );
            parameters.Add( new JsonObject { ["start"] = start, ["end"] = label.Length } );
        }

        label.Append( ')' );

        return new JsonObject { ["label"] = label.ToString(), ["parameters"] = parameters, ["documentation"] = GetSummary( method ) };
    }

    [GeneratedRegex( @"<summary>(?<text>.*?)</summary>", RegexOptions.Singleline )]
    private static partial Regex SummaryRegex();

    [GeneratedRegex( @"<(see|paramref|typeparamref)\s+(cref|name|langword)=""(?:[A-Z]:)?(?<name>[^""]+)""\s*/>" )]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex( @"<[^>]+>" )]
    private static partial Regex TagRegex();

    [GeneratedRegex( @"\s+" )]
    private static partial Regex WhitespaceRegex();

    private static string? GetSummary( ISymbol symbol )
    {
        var xml = (symbol as IMethodSymbol)?.ReducedFrom?.GetDocumentationCommentXml() ?? symbol.GetDocumentationCommentXml();

        if ( string.IsNullOrEmpty( xml ) || SummaryRegex().Match( xml ) is not { Success: true } match )
        {
            return null;
        }

        var text = ReferenceRegex().Replace( match.Groups["text"].Value, m => m.Groups["name"].Value.Split( '.' ).Last() );
        text = TagRegex().Replace( text, "" );

        return WhitespaceRegex().Replace( System.Net.WebUtility.HtmlDecode( text ), " " ).Trim();
    }

    /// <summary>
    /// Gets the errors and warnings of the query, as Run would report them. The expression form is tried first, then the
    /// statements form, as <see cref="QueryCompiler"/> does.
    /// </summary>
    public async Task<JsonObject> DiagnosticsAsync( string code, CancellationToken cancellationToken )
    {
        var diagnostics = await this.GetDiagnosticsAsync( code, null, cancellationToken );

        if ( diagnostics.Any( d => d.Severity == DiagnosticSeverity.Error ) && QueryCompiler.TryGetExpression( code, out _ ) )
        {
            var asStatements = await this.GetDiagnosticsAsync( code, false, cancellationToken );

            if ( !asStatements.Any( d => d.Severity == DiagnosticSeverity.Error ) )
            {
                diagnostics = asStatements;
            }
        }

        var markers = new JsonArray();

        foreach ( var diagnostic in diagnostics )
        {
            var span = diagnostic.Location.GetMappedLineSpan();

            if ( span.Path != QueryCompiler.QueryFileName )
            {
                continue;
            }

            markers.Add(
                new JsonObject
                {
                    ["severity"] = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
                    ["id"] = diagnostic.Id,
                    ["message"] = diagnostic.GetMessage( System.Globalization.CultureInfo.InvariantCulture ),
                    ["startLine"] = span.StartLinePosition.Line + 1,
                    ["startColumn"] = span.StartLinePosition.Character + 1,
                    ["endLine"] = span.EndLinePosition.Line + 1,
                    ["endColumn"] = span.EndLinePosition.Character + 1
                } );
        }

        return new JsonObject { ["markers"] = markers };
    }

    private async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync( string code, bool? asExpression, CancellationToken cancellationToken )
    {
        var query = this.GetDocument( code, asExpression );
        var model = await query.Document.GetSemanticModelAsync( cancellationToken );

        return model == null
            ? []
            : model.GetDiagnostics( cancellationToken: cancellationToken )
                .Where( d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning && !d.IsSuppressed )
                .ToList();
    }

    /// <summary>
    /// Maps the tags of a Roslyn completion item to a kind that the page maps to Monaco.
    /// </summary>
    private static string GetCompletionKind( ImmutableArray<string> tags )
    {
        foreach ( var tag in tags )
        {
            switch ( tag )
            {
                case "Method" or "ExtensionMethod" or "Operator":
                    return "method";

                case "Property" or "Indexer":
                    return "property";

                case "Field":
                    return "field";

                case "Local" or "Parameter" or "RangeVariable":
                    return "variable";

                case "Class" or "Record":
                    return "class";

                case "Structure" or "RecordStruct":
                    return "struct";

                case "Interface":
                    return "interface";

                case "Enum":
                    return "enum";

                case "EnumMember":
                    return "enumMember";

                case "Delegate":
                    return "function";

                case "Event":
                    return "event";

                case "Namespace":
                    return "module";

                case "Keyword" or "Intrinsic":
                    return "keyword";

                case "Constant":
                    return "constant";

                case "TypeParameter":
                    return "typeParameter";

                case "Snippet":
                    return "snippet";
            }
        }

        return "text";
    }

    public void Dispose() => this._workspace?.Dispose();

    private static string EscapeMarkdown( string text ) => Regex.Replace( text, @"([\\`*_{}\[\]<>#|])", @"\$1" );
}
