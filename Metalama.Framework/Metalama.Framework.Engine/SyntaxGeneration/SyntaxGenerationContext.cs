// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public sealed class SyntaxGenerationContext
{
    private readonly CompilationContext? _compilationContext;

    internal string EndOfLine { get; }

    internal SyntaxGenerationOptions Options { get; }

    private Compilation Compilation => this.CompilationContext.Compilation;

    public ContextualSyntaxGenerator SyntaxGenerator { get; }

    internal CompilationContext CompilationContext => this._compilationContext ?? throw new InvalidOperationException();

    internal bool HasCompilationContext => this._compilationContext != null;

    internal bool IsPartial { get; }

    internal ReflectionMapper ReflectionMapper => this.CompilationContext.ReflectionMapper;

    private LanguageVersion LanguageVersion => this.Compilation.GetLanguageVersion();

    internal bool RequiresStructFieldInitialization => this.LanguageVersion < (LanguageVersion) 1100;

    [Memo]
    internal bool SupportsInitAccessors => this.Compilation.GetTypeByMetadataName( typeof(IsExternalInit).FullName! ) != null;

    // Should only be called by CompilationContext
    internal SyntaxGenerationContext(
        CompilationContext compilationContext,
        bool isNullOblivious,
        bool isPartial,
        SyntaxGenerationOptions syntaxGenerationOptions,
        string endOfLine )
    {
        this._compilationContext = compilationContext;
        this.IsPartial = isPartial;
        this.Options = syntaxGenerationOptions;
        this.EndOfLine = endOfLine;
        this.SyntaxGenerator = new ContextualSyntaxGenerator( this, !isNullOblivious );
    }

    internal static SyntaxGenerationContext Contextless { get; } =
        new( false, false, new SyntaxGenerationOptions( CodeFormattingOptions.Default ), "\r\n" );

    [Memo]
    internal SyntaxTrivia ElasticEndOfLineTrivia => SyntaxFactory.ElasticEndOfLine( this.EndOfLine );

    public SyntaxTriviaList ElasticEndOfLineTriviaList => this.Options.TriviaMatters ? new SyntaxTriviaList( this.ElasticEndOfLineTrivia ) : default;

    [Memo]
    internal SyntaxTriviaList TwoElasticEndOfLinesTriviaList
        => this.Options.TriviaMatters ? new SyntaxTriviaList( this.ElasticEndOfLineTrivia, this.ElasticEndOfLineTrivia ) : default;

    private SyntaxGenerationContext(
        bool isNullOblivious,
        bool isPartial,
        SyntaxGenerationOptions syntaxGenerationOptions,
        string endOfLine )
    {
        this.IsPartial = isPartial;
        this.Options = syntaxGenerationOptions;
        this.EndOfLine = endOfLine;
        this.SyntaxGenerator = new ContextualSyntaxGenerator( this, !isNullOblivious );
    }

    public override string ToString() => $"SyntaxGenerator Compilation={this.Compilation.AssemblyName}, NullAware={this.SyntaxGenerator.IsNullAware}";

    // used for debug assert
    // ReSharper disable once UnusedMember.Global
    internal bool Equals( SyntaxGenerationContext? other )
        => other != null &&
           this.CompilationContext == other.CompilationContext &&
           this.SyntaxGenerator.IsNullAware == other.SyntaxGenerator.IsNullAware &&
           this.IsPartial == other.IsPartial;
}