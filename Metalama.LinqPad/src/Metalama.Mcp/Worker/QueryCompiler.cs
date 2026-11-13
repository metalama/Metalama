// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Compiles the C# of a query into an assembly that derives a class from <see cref="QueryBase"/>. Compiled queries are
/// cached by a hash of their text.
/// </summary>
internal sealed class QueryCompiler
{
    public const string QueryFileName = "query.cs";
    public const string QueryTypeName = "__MetalamaQuery";

    /// <summary>
    /// The namespaces imported by a query: those of the LINQPad driver, plus common BCL namespaces.
    /// </summary>
    public static ImmutableArray<string> Namespaces { get; } =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Threading.Tasks",
        "Metalama.Framework.Workspaces",
        "Metalama.Framework.Code",
        "Metalama.Framework.Code.Collections",
        "Metalama.Framework.Introspection",
        "Metalama.Framework.Diagnostics",
        "Metalama.Framework.Metrics",
        "Metalama.Extensions.Metrics"
    ];

    private readonly ConcurrentDictionary<string, CompiledQuery> _cache = new( StringComparer.Ordinal );
    private readonly Lazy<ImmutableArray<MetadataReference>> _references = new( GetReferences );

    public CompiledQuery Compile( string code )
    {
        var hash = Convert.ToHexString( SHA256.HashData( Encoding.UTF8.GetBytes( code ) ) );

        return this._cache.GetOrAdd( hash, _ => this.CompileCore( code ) );
    }

    private CompiledQuery CompileCore( string code )
    {
        if ( !TryGetExpression( code, out var expression ) )
        {
            return this.Emit( GenerateSource( code, false ) );
        }

        var asExpression = this.Emit( GenerateSource( expression, true ) );

        if ( asExpression.Assembly != null )
        {
            return asExpression;
        }

        // An expression of type void, such as Console.WriteLine( 1 ), is a statement.
        var asStatements = this.Emit( GenerateSource( code.TrimEnd().EndsWith( ';' ) ? code : code + ";", false ) );

        return asStatements.Assembly != null ? asStatements : asExpression;
    }

    private CompiledQuery Emit( string source )
    {
        var syntaxTree = CSharpSyntaxTree.ParseText( source, new CSharpParseOptions( LanguageVersion.Preview ), QueryFileName, Encoding.UTF8 );

        var compilation = CSharpCompilation.Create(
            "MetalamaQuery_" + Guid.NewGuid().ToString( "N" ),
            [syntaxTree],
            this._references.Value,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Disable,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: false ) );

        using var stream = new MemoryStream();
        var result = compilation.Emit( stream );

        var diagnostics = result.Diagnostics
            .Where( d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning )
            .Select( ToQueryDiagnostic )
            .ToImmutableArray();

        return new CompiledQuery( result.Success ? stream.ToArray() : null, diagnostics );
    }

    /// <summary>
    /// Wraps the code in a class. <c>#line</c> directives make diagnostics point at the code as the agent wrote it.
    /// </summary>
    internal static string GenerateSource( string code, bool isExpression )
    {
        var builder = new StringBuilder();

        foreach ( var ns in Namespaces )
        {
            builder.Append( "using " ).Append( ns ).AppendLine( ";" );
        }

        builder.AppendLine( "#pragma warning disable CS0162, CS1998, CS8019" );
        builder.AppendLine( "#line hidden" );
        builder.Append( "public sealed class " ).Append( QueryTypeName ).AppendLine( " : global::Metalama.Mcp.Worker.QueryBase" );
        builder.AppendLine( "{" );
        builder.AppendLine( "    protected override async global::System.Threading.Tasks.Task<object> ExecuteAsync()" );
        builder.AppendLine( "    {" );

        if ( isExpression )
        {
            builder.AppendLine( "        return (object)(" );
            builder.AppendLine( "#line 1 \"" + QueryFileName + "\"" );
            builder.AppendLine( code );
            builder.AppendLine( "#line hidden" );
            builder.AppendLine( "        );" );
        }
        else
        {
            builder.AppendLine( "#line 1 \"" + QueryFileName + "\"" );
            builder.AppendLine( code );
            builder.AppendLine( "#line hidden" );
            builder.AppendLine( "        return null;" );
        }

        builder.AppendLine( "    }" );
        builder.AppendLine( "}" );

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether the code is a single expression, possibly followed by a semicolon.
    /// </summary>
    internal static bool TryGetExpression( string code, out string expression )
    {
        foreach ( var candidate in new[] { code, code.TrimEnd().TrimEnd( ';' ) } )
        {
            var parsed = SyntaxFactory.ParseExpression( candidate );

            if ( !parsed.ContainsDiagnostics && parsed.FullSpan.Length == candidate.Length && parsed is not IdentifierNameSyntax { Identifier.Text: "" } )
            {
                expression = candidate;

                return true;
            }
        }

        expression = code;

        return false;
    }

    private static QueryDiagnostic ToQueryDiagnostic( Diagnostic diagnostic )
    {
        var span = diagnostic.Location.GetMappedLineSpan();
        var isInQuery = span.Path == QueryFileName;

        return new QueryDiagnostic(
            diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
            diagnostic.Id,
            isInQuery ? span.StartLinePosition.Line + 1 : null,
            isInQuery ? span.StartLinePosition.Character + 1 : null,
            diagnostic.GetMessage( System.Globalization.CultureInfo.InvariantCulture ) );
    }

    /// <summary>
    /// References the assemblies of the worker, which include the .NET runtime, Roslyn and Metalama, so that a query sees the
    /// same types as the loaded workspace.
    /// </summary>
    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var paths = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

        if ( AppContext.GetData( "TRUSTED_PLATFORM_ASSEMBLIES" ) is string trustedAssemblies )
        {
            foreach ( var path in trustedAssemblies.Split( Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries ) )
            {
                paths.TryAdd( Path.GetFileName( path ), path );
            }
        }

        foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
        {
            if ( !assembly.IsDynamic && !assembly.IsCollectible && !string.IsNullOrEmpty( assembly.Location ) )
            {
                paths.TryAdd( Path.GetFileName( assembly.Location ), assembly.Location );
            }
        }

        return
        [
            ..paths.Values
                .Where( p => p.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) && File.Exists( p ) )
                .Select( p => (MetadataReference) MetadataReference.CreateFromFile( p ) )
        ];
    }
}

/// <summary>
/// The result of compiling a query: the assembly, or <c>null</c> when compilation failed, and the diagnostics.
/// </summary>
internal sealed record CompiledQuery( byte[]? Assembly, ImmutableArray<QueryDiagnostic> Diagnostics );

/// <summary>
/// A compiler diagnostic, with its position in the code of the query, or no position when it is in generated code.
/// </summary>
internal sealed record QueryDiagnostic( string Severity, string Id, int? Line, int? Column, string Message )
{
    public override string ToString()
        => this.Line != null ? $"{this.Severity} {this.Id} ({this.Line},{this.Column}): {this.Message}" : $"{this.Severity} {this.Id}: {this.Message}";
}
