// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Represents the result of a test run.
/// </summary>
[PublicAPI]
internal class TestResult : IDisposable
{
    private static readonly Regex _cleanCallStackRegex = new( " in (.*):line \\d+" );

    private readonly List<TestSyntaxTree> _syntaxTrees = new();
    private bool _frozen;
    private ConcurrentBag<ScopedSuppression>? _diagnosticSuppressions = new();

    public TestInput? TestInput { get; set; }

    public IDiagnosticBag InputCompilationDiagnostics { get; } = new DiagnosticBag();

    public IDiagnosticBag OutputCompilationDiagnostics { get; } = new DiagnosticBag();

    public IDiagnosticBag CompileTimeCompilationDiagnostics { get; } = new DiagnosticBag();

    public IDiagnosticBag PipelineDiagnostics { get; } = new DiagnosticBag();

    // We don't add the CompileTimeCompilationDiagnostics to Diagnostics because they are already in PipelineDiagnostics.
    public IEnumerable<Diagnostic> Diagnostics
    {
        get
        {
            var minimalSeverity = this.TestInput?.Options.ReportOutputWarnings == true ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error;

            var allDiagnostics = this.OutputCompilationDiagnostics.Where( d => d.Severity >= minimalSeverity )
                .Concat( this.PipelineDiagnostics )
                .Concat( this.InputCompilationDiagnostics );

            return allDiagnostics.Where( MustBeReported );

            bool MustBeReported( Diagnostic d )
            {
                if ( d.Id == "CS1701" )
                {
                    // Ignore warning CS1701: Assuming assembly reference "Assembly Name #1" matches "Assembly Name #2", you may need to supply runtime policy.
                    // This warning is ignored by MSBuild anyway.
                    return false;
                }

                if ( this.TestInput?.ShouldIgnoreDiagnostic( d.Id ) == true )
                {
                    return false;
                }

                foreach ( var suppression in this.DiagnosticSuppressions )
                {
                    if ( suppression.Matches( d, this.InputCompilation!, filter => filter() ) )
                    {
                        return false;
                    }

                    if ( suppression.Matches( d, this.OutputCompilation!, filter => filter() ) )
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public ConcurrentBag<ScopedSuppression> DiagnosticSuppressions => this._diagnosticSuppressions ?? throw new ObjectDisposedException( nameof(TestResult) );

    public void AddDiagnosticSuppressions( IEnumerable<ScopedSuppression> suppressions )
    {
        foreach ( var suppression in suppressions )
        {
            this.DiagnosticSuppressions.Add( suppression );
        }
    }

    public IReadOnlyList<TestSyntaxTree> SyntaxTrees => this._syntaxTrees;

    /// <summary>
    /// Gets the primary error message emitted during test run.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets or sets the input test project (before transformation).
    /// </summary>
    public Project? InputProject { get; set; }

    /// <summary>
    /// Gets or sets the input test project (after transformation).
    /// </summary>
    public Project? OutputProject { get; set; }

    /// <summary>
    /// Gets or sets the initial compilation of the test project.
    /// </summary>
    public Compilation? InputCompilation { get; set; }

    /// <summary>
    /// Gets or sets the result compilation of the test project.
    /// </summary>
    public Compilation? OutputCompilation { get; set; }

    /// <summary>
    /// Gets a value indicating whether the test run succeeded.
    /// </summary>
    public bool Success { get; private set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the output run-time code should be included in the result of
    ///  <see cref="BuildSyntaxTreesForComparison"/>.
    /// </summary>
    public bool HasOutputCode { get; set; }

    public Compilation? CompileTimeCompilation { get; private set; }

    public ICompilation? InitialCompilationModel { get; set; }

    internal PartialCompilation? IntermediateLinkerCompilation { get; set; }

    public string? ProgramOutput { get; set; }

    public TestContext? TestContext { get; set; }

    internal async Task AddInputDocumentAsync( Document document, string? path )
        => this._syntaxTrees.Add( await TestSyntaxTree.CreateAsync( path, document, this ) );

    private static string CleanMessage( string text )
    {
        // Remove local-specific stuff.
        text = _cleanCallStackRegex.Replace( text, "" );

        // Comment all lines but the first one.
        var lines = text.Split( '\n' );

        if ( lines.Length == 1 )
        {
            return text;
        }

        lines[0] = lines[0].Trim( '\r' );

        for ( var i = 1; i < lines.Length; i++ )
        {
            if ( lines[i].TrimStart().StartsWith( "at ", StringComparison.Ordinal ) )
            {
                // Remove exception stacks because they are different in debug and release builds.
                lines[i] = "<>";
            }
            else
            {
                lines[i] = "// " + lines[i].Trim( '\r' );
            }
        }

        return string.Join( Environment.NewLine, lines.Where( l => l != "<>" ) );
    }

    internal async Task SetCompileTimeCompilationAsync( Compilation compilation, IReadOnlyDictionary<string, string> compileTimeToSourceMap )
    {
        if ( this.InputCompilation == null ||
             this.TestInput == null ||
             this.InputProject == null )
        {
            throw new InvalidOperationException( "The object has not bee properly initialized." );
        }

        this.CompileTimeCompilation = compilation;

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            if ( CompileTimeConstants.IsPredefinedSyntaxTree( syntaxTree.FilePath ) )
            {
                // This is the "Intrinsics" syntax tree.
                continue;
            }

            var syntaxNode = await syntaxTree.GetRootAsync();

            // Format the output code.

            if ( compileTimeToSourceMap.TryGetValue( syntaxTree.FilePath, out var sourcePath ) )
            {
                var testTree = this.SyntaxTrees.SingleOrDefault(
                    t =>
                    {
                        var sourceFileName = Path.GetFileName( sourcePath );

                        return Path.GetFileName( t.InputPath ) == sourceFileName;
                    } );

                testTree?.SetCompileTimeCode( syntaxNode, syntaxTree.FilePath );
            }
        }
    }

    internal async Task SetOutputCompilationAsync( Compilation runTimeCompilation )
    {
        if ( this.InputCompilation == null ||
             this.TestInput == null ||
             this.InputProject == null )
        {
            throw new InvalidOperationException( "The object has not been properly initialized." );
        }

        var introducedSyntaxTreePaths = this.InputCompilation.SyntaxTrees.Select( t => t.FilePath ).ToHashSet();

        foreach ( var syntaxTree in runTimeCompilation.SyntaxTrees )
        {
            TestSyntaxTree? testSyntaxTree;

            if ( !introducedSyntaxTreePaths.Contains( syntaxTree.FilePath ) )
            {
                testSyntaxTree = TestSyntaxTree.CreateIntroduced( this );

                this._syntaxTrees.Add( testSyntaxTree );
            }
            else
            {
                testSyntaxTree = this.SyntaxTrees.SingleOrDefault(
                    x => x.InputDocument != null && StringComparer.Ordinal.Equals( x.InputDocument.FilePath, syntaxTree.FilePath ) );

                if ( testSyntaxTree == null )
                {
                    // This is the "Intrinsics" syntax tree.
                    continue;
                }
            }

            // Format the output code.
            await testSyntaxTree.SetRunTimeCodeAsync( await syntaxTree.GetRootAsync() );
        }
    }

    public void SetFailed( string reason, Exception? exception = null )
    {
        if ( this._frozen )
        {
            return;
        }

        this._frozen = true;

        this.Success = false;
        this.ErrorMessage = reason;

        if ( exception != null )
        {
            this.ErrorMessage += Environment.NewLine + exception;
        }
    }

    private string GetTextUnderDiagnostic( Diagnostic diagnostic )
    {
        var syntaxTree = diagnostic.Location.SourceTree;

        // If we don't have the a SyntaxTree, find it from the input compilation.

        syntaxTree ??= this.InputCompilation?.SyntaxTrees.SingleOrDefault( t => t.FilePath == diagnostic.Location.GetLineSpan().Path );

        var text = syntaxTree?.GetText()
            .GetSubText( diagnostic.Location.SourceSpan )
            .ToString()
            .ReplaceOrdinal( "\r\n", " " )
            .ReplaceOrdinal( "\n", " " )
            .ReplaceOrdinal( "\n", " " )
            .ReplaceOrdinal( "\t", " " ) ?? "";

        while ( text.ContainsOrdinal( "  " ) )
        {
            text = text.ReplaceOrdinal( "  ", " " );
        }

        return text;
    }

    private (ISymbol? Symbol, string CompilationName) GetDeclarationUnderDiagnostic( Diagnostic diagnostic )
    {
        var syntaxTree = diagnostic.Location.SourceTree;

        if ( syntaxTree == null )
        {
            return default;
        }

        var compilation =
            new[] { (this.InputCompilation, "input"), (this.OutputCompilation, "output") }.FirstOrDefault( c => c.Item1!.ContainsSyntaxTree( syntaxTree ) );

        if ( compilation.Item1 == null )
        {
            return default;
        }

        var semanticModel = compilation.Item1.GetSemanticModel( syntaxTree );

        var node = syntaxTree.GetRoot().FindNode( diagnostic.Location.SourceSpan, getInnermostNodeForTie: true );

        for ( var n = node; n != null; n = n.Parent )
        {
            var symbol = semanticModel.GetDeclaredSymbol( node );

            if ( symbol != null )
            {
                return (symbol, compilation.Item2);
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the content of the <c>.t.cs</c> file, i.e. the output transformed code with comments
    /// for diagnostics.
    /// </summary>
    public void BuildSyntaxTreesForComparison()
    {
        if ( this.TestInput == null )
        {
            throw new InvalidOperationException();
        }

        bool IsSyntaxTreeIncluded( TestSyntaxTree testSyntaxTree )
        {
            switch ( testSyntaxTree.Kind )
            {
                case TestSyntaxTreeKind.Introduced:
                    return true;

                case TestSyntaxTreeKind.Default:
                    // Include the primary file and any secondary file that includes <target>.
                    return testSyntaxTree.ShortName == this.TestInput.TestName
                           || testSyntaxTree.OutputRunTimeSyntaxRoot?.SyntaxTree.GetText().ToString().ContainsOrdinal( "<target>" ) == true;

                default:
                    return false;
            }
        }

        // Adding the syntax of the transformed run-time code, but only if the pipeline was successful.
        var outputSyntaxTrees =
            this.SyntaxTrees
                .Where( IsSyntaxTreeIncluded )
                .OrderBy( x => x.FilePath.Length )
                .ThenBy( x => x.FilePath, StringComparer.InvariantCultureIgnoreCase )
                .ToArray();

        if ( outputSyntaxTrees.Length == 0 )
        {
            return;
        }

        var primaryOutputTree = outputSyntaxTrees.Single( x => x.ShortName == this.TestInput.TestName );

        var outputTreesByFilePath = outputSyntaxTrees
            .ToDictionary( x => x.FilePath, x => x );

        // Assign diagnostics to syntax trees.
        var diagnosticsBySyntaxTree = new Dictionary<TestSyntaxTree, List<Diagnostic>>();

        foreach ( var diagnostic in this.Diagnostics )
        {
            var diagnosticsSourceFilePath = diagnostic.Location.SourceTree?.FilePath;

            if ( diagnosticsSourceFilePath != null && outputTreesByFilePath.TryGetValue( diagnosticsSourceFilePath, out var diagnosticSourceSyntaxTree ) )
            {
                if ( !diagnosticsBySyntaxTree.TryGetValue( diagnosticSourceSyntaxTree, out var diagnostics ) )
                {
                    diagnostics = new List<Diagnostic>();
                    diagnosticsBySyntaxTree.Add( diagnosticSourceSyntaxTree, diagnostics );
                }

                diagnostics.Add( diagnostic );
            }
            else if ( primaryOutputTree != null )
            {
                if ( !diagnosticsBySyntaxTree.TryGetValue( primaryOutputTree, out var diagnostics ) )
                {
                    diagnostics = new List<Diagnostic>();
                    diagnosticsBySyntaxTree.Add( primaryOutputTree, diagnostics );
                }

                diagnostics.Add( diagnostic );
            }
        }

        foreach ( var testSyntaxTree in outputSyntaxTrees )
        {
            var consolidatedCompilationUnit = SyntaxFactory.CompilationUnit();

            if ( this.HasOutputCode && testSyntaxTree is { OutputRunTimeSyntaxRoot: not null } && this.TestInput.Options.RemoveOutputCode != true )
            {
                // Adding syntax annotations for the output compilation. We cannot add syntax annotations for diagnostics
                // on the input compilation because they would potentially not map properly to the output compilation.
                var outputSyntaxRoot = FormattedCodeWriter.AddDiagnosticAnnotations(
                    testSyntaxTree.OutputRunTimeSyntaxRoot,
                    testSyntaxTree.InputPath,
                    this.OutputCompilationDiagnostics.ToArray() );

                if ( this.TestInput.Options.ExcludeAssemblyAttributes != true )
                {
                    // Add assembly-level custom attributes. We do not include AspectOrder because this would pollute many tests.
                    consolidatedCompilationUnit = consolidatedCompilationUnit.WithAttributeLists(
                        consolidatedCompilationUnit.AttributeLists.AddRange(
                            outputSyntaxRoot.AttributeLists.Where( a => !a.ToString().ContainsOrdinal( "AspectOrder" ) ) ) );
                }

                // Find nodes annotated with // <target> or with a comment containing <target>. If there is none, the test output is the whole tree
                // passed to this method.

                var outputMembers =
                    outputSyntaxRoot
                        .DescendantNodesAndSelf( _ => true )
                        .OfType<MemberDeclarationSyntax>()
                        .Where(
                            m => m.GetLeadingTrivia().ToString().ContainsOrdinal( "<target>" ) ||
                                 m.ChildTokens().FirstOrDefault().LeadingTrivia.ToString().ContainsOrdinal( "<target>" ) ||
                                 m.AttributeLists.Any( a => a.GetLeadingTrivia().ToString().ContainsOrdinal( "<target>" ) ) )
                        .Cast<SyntaxNode>()
                        .ToArray();

                if ( outputMembers is [] )
                {
                    // There is no <target> member.

                    if ( testSyntaxTree == primaryOutputTree || testSyntaxTree.Kind is TestSyntaxTreeKind.Introduced )
                    {
                        // If this is the primary syntax tree, include the complete output.
                        outputMembers = [outputSyntaxRoot];
                    }
                    else
                    {
                        // Otherwise, skip the syntax tree.
                        continue;
                    }
                }

                for ( var i = 0; i < outputMembers.Length; i++ )
                {
                    switch ( outputMembers[i] )
                    {
                        case MemberDeclarationSyntax member:
                            if ( i != outputMembers.Length - 1 )
                            {
                                consolidatedCompilationUnit =
                                    consolidatedCompilationUnit.AddMembers(
                                        member.WithoutTrivia().WithTrailingTrivia( SyntaxFactory.ElasticLineFeed, SyntaxFactory.ElasticLineFeed ) );
                            }
                            else
                            {
                                consolidatedCompilationUnit = consolidatedCompilationUnit.AddMembers( member.WithoutTrivia() );
                            }

                            break;

                        case CompilationUnitSyntax compilationUnit:
                            consolidatedCompilationUnit = consolidatedCompilationUnit
                                .AddMembers( compilationUnit.Members.ToArray() )
                                .AddUsings( compilationUnit.Usings.ToArray() );

                            break;

                        case ExpressionStatementSyntax statementSyntax when statementSyntax.ToString() == "":
                            // This is an empty statement
                            consolidatedCompilationUnit = consolidatedCompilationUnit
                                .WithLeadingTrivia( statementSyntax.GetLeadingTrivia().AddRange( consolidatedCompilationUnit.GetLeadingTrivia() ) );

                            break;

                        default:
                            throw new InvalidOperationException( $"Don't know how to add a {outputMembers[i].Kind()} to the compilation unit." );
                    }
                }
            }

            // Adding the diagnostics as trivia.
            List<SyntaxTrivia> comments = new();

            if ( diagnosticsBySyntaxTree.TryGetValue( testSyntaxTree, out var diagnosticsForOutputTree ) )
            {
                if ( !this.Success && (this.TestInput!.Options.ReportErrorMessage.GetValueOrDefault()
                                       || diagnosticsForOutputTree.All( c => c.Severity != DiagnosticSeverity.Error )) )
                {
                    comments.Add( SyntaxFactory.Comment( $"// {this.ErrorMessage} \n" ) );
                }

                // We exclude LAMA0222 from the results because it contains randomly-generated info and tests need to be deterministic.
                comments.AddRange(
                    diagnosticsForOutputTree
                        .Where(
                            d => d.Id != "LAMA0222" &&
                                 (this.TestInput!.Options.IncludeAllSeverities.GetValueOrDefault()
                                  || d.Severity >= DiagnosticSeverity.Warning) && !this.TestInput.ShouldIgnoreDiagnostic( d.Id ) )
                        .OrderBy( d => d.Location.SourceSpan.Start )
                        .ThenBy( d => d.GetMessage( CultureInfo.InvariantCulture ), StringComparer.Ordinal )
                        .SelectMany( this.GetDiagnosticComments )
                        .Select( SyntaxFactory.Comment )
                        .ToReadOnlyList() );
            }
            else
            {
                if ( !this.Success )
                {
                    comments.Add( SyntaxFactory.Comment( $"// {this.ErrorMessage} \n" ) );
                }
                else if ( !consolidatedCompilationUnit.ChildNodes().Any() )
                {
                    // This can happen when the test does not want to include the output code.
                    comments.Add( SyntaxFactory.Comment( $"// The compilation was successful.\n" ) );
                }
            }

            consolidatedCompilationUnit = consolidatedCompilationUnit.WithLeadingTrivia( comments );

            // Individual trees should be formatted, so we don't need to format again.
            testSyntaxTree.OutputRunTimeSyntaxTreeForComparison =
                CSharpSyntaxTree.Create(
                    consolidatedCompilationUnit,
                    path: Path.GetFileName(
                        testSyntaxTree.FilePath
                        ?? throw new InvalidOperationException( "Output syntax tree has no path" ) ) );
        }
    }

    private IEnumerable<string> GetDiagnosticComments( Diagnostic d )
    {
        var message = $"// {d.Severity} {d.Id}";

        var testInputOptions = this.TestInput!.Options;

        if ( testInputOptions.IncludeLineNumberInDiagnosticReport == true )
        {
            message +=
                $" at line {d.Location.GetLineSpan().StartLinePosition.Line + 1}";
        }
        else
        {
            message += $" on `{this.GetTextUnderDiagnostic( d )}`";
        }

        if ( testInputOptions.IncludeDeclarationInDiagnosticReport == true )
        {
            var symbol = this.GetDeclarationUnderDiagnostic( d );

            if ( symbol.Symbol != null )
            {
                message += $" in `{symbol.Symbol}` ({symbol.CompilationName} compilation)";
            }
        }

        if ( testInputOptions.RemoveDiagnosticMessage != true )
        {
            message += $": `{CleanMessage( d.GetMessage( CultureInfo.InvariantCulture ) )}`";
        }
        else
        {
            message += ".";
        }

        message += "\n";

        yield return message;

        foreach ( var codeFix in DiagnosticCodeFixHelper.GetCodeFixTitles( d ) )
        {
            yield return $"//    CodeFix: {codeFix}`\n";
        }
    }

    private TestSyntaxTree? PrimaryTestSyntaxTree => this.SyntaxTrees.FirstOrDefault( t => t.Kind is TestSyntaxTreeKind.Default );

    [Obsolete( "Use SyntaxTrees[...].ExpectedTransformedSourceText" )]
    public string? ExpectedTransformedSourceText => this.PrimaryTestSyntaxTree?.ExpectedTransformedCodeText;

    [Obsolete( "Use SyntaxTrees[...].ActualTransformedNormalizedSourceText" )]
    public string? ActualTransformedNormalizedSourceText => this.PrimaryTestSyntaxTree?.ActualTransformedCodePath;

    [Obsolete( "Use SyntaxTrees[...].ActualTransformedSourceTextForStorage" )]
    public string? ActualTransformedSourceTextForStorage => this.PrimaryTestSyntaxTree?.ActualTransformedSourceTextForStorage;

    [Obsolete( "Use SyntaxTrees[...].ActualTransformedSourcePath" )]
    public string? ActualTransformedSourcePath => this.PrimaryTestSyntaxTree?.ActualTransformedCodePath;

    [Obsolete( "Use SyntaxTrees[...].ExpectedTransformedSourcePath" )]
    public string? ExpectedTransformedSourcePath => this.PrimaryTestSyntaxTree?.ExpectedTransformedCodePath;

    public void Dispose()
    {
        // This is to make sure that we have no reference to the compile-time assembly.
        // A Task<TestResult> may be non-collectable for some time after task completion (not sure why), so disposing
        // the TestResult makes sure we don't have a GC reference to the assembly even if we still have a reference to the TestResult.

        this._frozen = true;

        this._syntaxTrees.Clear();
        this.OutputCompilation = null;
        this.OutputProject = null;
        this.InputCompilation = null;
        this.InputProject = null;
        this.IntermediateLinkerCompilation = null;
        this.InitialCompilationModel = null;

        // Diagnostics and suppressions may have reference to declarations, and must be collected too.
        this.PipelineDiagnostics.Clear();
        this.InputCompilationDiagnostics.Clear();
        this.OutputCompilationDiagnostics.Clear();
        this.CompileTimeCompilationDiagnostics.Clear();
        this._diagnosticSuppressions = null!;

        this.TestContext?.Dispose();
        this.TestContext = null;
    }
}