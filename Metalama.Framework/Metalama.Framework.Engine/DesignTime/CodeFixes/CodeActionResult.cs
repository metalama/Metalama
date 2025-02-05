// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.DesignTime.CodeFixes;

[JsonObject]
public sealed class CodeActionResult
{
    public ImmutableArray<SerializableSyntaxTree> SyntaxTreeChanges { get; }

    public ImmutableArray<string>? ErrorMessages { get; }

    public bool IsSuccessful => this.ErrorMessages == null;

    [JsonConstructor]
    private CodeActionResult( ImmutableArray<SerializableSyntaxTree> syntaxTreeChanges, ImmutableArray<string>? errorMessages = null )
    {
        this.SyntaxTreeChanges = syntaxTreeChanges;
        this.ErrorMessages = errorMessages;
    }

    public static CodeActionResult Success( ImmutableArray<SerializableSyntaxTree> syntaxTreeChanges ) => new( syntaxTreeChanges );

    public static CodeActionResult Success( IEnumerable<SyntaxTree> modifiedTrees )
        => Success( modifiedTrees.Select( JsonSerializationHelper.CreateSerializableSyntaxTree ).ToImmutableArray() );

    public static CodeActionResult Error( string message ) => Error( [message] );

    public static CodeActionResult Error( IEnumerable<string> messages ) => new( ImmutableArray<SerializableSyntaxTree>.Empty, messages.ToImmutableArray() );

    public static CodeActionResult Error( Diagnostic diagnostic ) => Error( [diagnostic] );

    public static CodeActionResult Error( IEnumerable<Diagnostic> diagnostic )
        => Error( diagnostic.Where( d => d.Severity == DiagnosticSeverity.Error ).Select( d => d.GetLocalizedMessage() ) );

    public static CodeActionResult Empty { get; } = new( ImmutableArray<SerializableSyntaxTree>.Empty );

    public async ValueTask<Solution> ApplyAsync( Microsoft.CodeAnalysis.Project project, ILogger logger, bool format, CancellationToken cancellationToken )
    {
        if ( !this.IsSuccessful )
        {
            throw new InvalidOperationException();
        }

        var solution = project.Solution;
        var documentIds = new List<DocumentId>( this.SyntaxTreeChanges.Length );

        // Apply changes.
        foreach ( var change in this.SyntaxTreeChanges )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = project.Documents.FirstOrDefault( x => x.FilePath == change.FilePath );

            if ( document == null )
            {
                logger?.Warning?.Log( $"Cannot map changes to solution: Cannot find document '{change.FilePath}'." );

                continue;
            }

            solution = solution.WithDocumentSyntaxRoot( document.Id, change.ToSyntaxNode( cancellationToken ) );
            documentIds.Add( document.Id );
        }

        if ( format )
        {
            solution = await new CodeFormatter().FormatAsync(
                solution,
                documentIds,
                null,
                false,
                cancellationToken );
        }

        return solution;
    }
}