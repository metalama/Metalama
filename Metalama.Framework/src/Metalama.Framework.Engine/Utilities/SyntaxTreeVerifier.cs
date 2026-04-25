// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// Provides utilities for verifying syntax trees.
/// </summary>
internal sealed class SyntaxTreeVerifier
{
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    public SyntaxTreeVerifier( ProjectServiceProvider serviceProvider )
    {
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
    }

    /// <summary>
    /// Verifies that a compilation's syntax trees can be round-trip parsed without errors.
    /// This checks for "hidden" problems where the text of the source code appears valid,
    /// but reparsing it reveals syntax errors (e.g., missing whitespace).
    /// </summary>
    /// <param name="compilation">The compilation to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing a success flag and any diagnostics found. If successful, diagnostics will be null.</returns>
    public async Task<(bool isSuccessful, DiagnosticBag? diagnostics)> VerifyAsync(
        Compilation compilation,
        CancellationToken cancellationToken )
    {
        var diagnosticBag = new DiagnosticBag();

        // Process all syntax trees in parallel
        await this._concurrentTaskRunner.RunConcurrentlyAsync(
            compilation.SyntaxTrees,
            syntaxTree => VerifySyntaxTree( syntaxTree, diagnosticBag ),
            cancellationToken );

        if ( diagnosticBag.Count == 0 )
        {
            return (true, null);
        }
        else
        {
            return (false, diagnosticBag);
        }
    }

    private static void VerifySyntaxTree( SyntaxTree syntaxTree, DiagnosticBag diagnostics )
    {
        SyntaxNode parsedFromText;

        // Use ToFullString(), not ToString(): ToString() drops the leading trivia of the first token
        // and the trailing trivia of the last token. For a source file that starts with a directive
        // like '#if BENCHMARK' (held as leading trivia of the first 'using' token), ToString() would
        // strip the opening directive while leaving the closing '#endif' (which lives as leading trivia
        // of the EOF token), producing a spurious CS1028 'Unexpected preprocessor directive' on reparse.
        var sourceText = syntaxTree.GetRoot().ToFullString();

        try
        {
            // Parse the syntax tree's text representation back into a syntax tree
            parsedFromText = CSharpSyntaxTree.ParseText(
                    sourceText,
                    path: syntaxTree.FilePath,
                    encoding: Encoding.UTF8,
                    options: (CSharpParseOptions) syntaxTree.Options )
                .GetRoot();
        }
        catch ( Exception ex )
        {
            diagnostics.Report(
                GeneralDiagnosticDescriptors.GeneratedCodeParseFailed.CreateRoslynDiagnostic(
                    null,
                    (syntaxTree.FilePath, ex.GetType().Name, ex.Message) ) );

            return;
        }

        // Check for syntax errors in the parsed tree
        foreach ( var diagnostic in parsedFromText.GetDiagnostics() )
        {
            if ( diagnostic.Severity == DiagnosticSeverity.Error )
            {
                // Find the problematic node for debugging
                var node = parsedFromText.FindNode( diagnostic.Location.SourceSpan );
                var nodeText = node.ToString();

                // Truncate the problematic code if too long
                if ( nodeText.Length > 100 )
                {
                    nodeText = nodeText.Substring( 0, 97 ) + "...";
                }

                var lineSpan = diagnostic.Location.GetLineSpan();

                diagnostics.Report(
                    GeneralDiagnosticDescriptors.GeneratedCodeSyntaxError.CreateRoslynDiagnostic(
                        diagnostic.Location,
                        (syntaxTree.FilePath,
                         diagnostic.GetMessage( CultureInfo.InvariantCulture ),
                         lineSpan.StartLinePosition.Line + 1,
                         lineSpan.StartLinePosition.Character + 1,
                         nodeText) ) );
            }
        }
    }
}