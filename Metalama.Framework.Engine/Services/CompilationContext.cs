// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Factories;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Services;

#pragma warning disable CA1822

public sealed class CompilationContext : ICompilationServices, ITemplateReflectionContext
{
    private readonly ConcurrentDictionary<SyntaxGenerationContextCacheKey, SyntaxGenerationContext> _syntaxGenerationContextCache = new();
    private static int _nextId;

    internal CompilationContext( Compilation compilation )
    {
        this.Compilation = compilation;
    }

    private readonly int _id = Interlocked.Increment( ref _nextId );

    [Memo]
    internal ResolvingCompileTimeTypeFactory CompileTimeTypeFactory => new( this.SerializableTypeIdResolver );

    [Memo]
    internal CompilationComparers Comparers => new( this.Compilation );

    public Compilation Compilation { get; }

    CompilationContext ITemplateReflectionContext.CompilationContext => this;

    CompilationModel ITemplateReflectionContext.GetCompilationModel( ICompilation sourceCompilation )
        =>

            // When the current CompilationContext is used for reflecting the template code
            // (because the template is defined in source code, so it does not have its own ITemplateReflectionContext),
            // we use the source compilation.
            (CompilationModel) sourceCompilation;

    public bool IsCacheable => false;

    IReflectionMapper ICompilationServices.ReflectionMapper => this.ReflectionMapper;

    [Memo]
    internal ReflectionMapper ReflectionMapper => new( this.Compilation );

    [Memo]
    public SerializableTypeIdResolverForSymbol SerializableTypeIdResolver => new( this );

    [Memo]
    internal SemanticModelProvider SemanticModelProvider => this.Compilation.GetSemanticModelProvider();

    [Memo]
    public SafeSymbolComparer SymbolComparer => new( this );

    [Memo]
    internal IEqualityComparer<ISymbol> SymbolComparerIncludingNullability => new SafeSymbolComparer( this, SymbolEqualityComparer.IncludeNullability );

    [Memo]

    internal ImmutableDictionary<AssemblyIdentity, IAssemblySymbol> Assemblies
        => this.Compilation.SourceModule.ReferencedAssemblySymbols.Concat( this.Compilation.Assembly ).ToImmutableDictionary( x => x.Identity, x => x );

    [Memo]
    internal IEqualityComparer<IEvent> EventComparer => new MemberComparer<IEvent>( this.Comparers.Default );

    [Memo]
    internal IEqualityComparer<IField> FieldComparer => new MemberComparer<IField>( this.Comparers.Default );

    [Memo]
    internal IEqualityComparer<IFieldOrProperty> FieldOrPropertyComparer => new MemberComparer<IFieldOrProperty>( this.Comparers.Default );

    [Memo]
    internal IEqualityComparer<IIndexer> IndexerComparer => new MemberComparer<IIndexer>( this.Comparers.Default );

    [Memo]
    internal IEqualityComparer<IMethod> MethodComparer => new MemberComparer<IMethod>( this.Comparers.Default );

    [Memo]
    internal IEqualityComparer<IProperty> PropertyComparer => new MemberComparer<IProperty>( this.Comparers.Default );

    internal SyntaxGenerationContext GetSyntaxGenerationContext( SyntaxGenerationOptions options, SyntaxNode node )
        => this.GetSyntaxGenerationContext( options, node.SyntaxTree, node.SpanStart );

    [PublicAPI]
    internal SyntaxGenerationContext GetSyntaxGenerationContext( SyntaxGenerationOptions options, IRef reference )
        => reference switch
        {
            ISymbolRef symbolRef => this.GetSyntaxGenerationContext( options, symbolRef.Symbol.GetPrimaryDeclarationSyntax().AssertNotNull() ),
            IIntroducedRef builtDeclarationRef => this.GetSyntaxGenerationContext(
                options,
                builtDeclarationRef.GetClosestContainingSymbol().GetPrimaryDeclarationSyntax().AssertNotNull() ),
            _ => throw new AssertionFailedException()
        };

    internal SyntaxGenerationContext GetSyntaxGenerationContext(
        SyntaxGenerationOptions options,
        SyntaxTree tree,
        int nodeSpanStart,
        bool isPartial = false )
    {
        bool? isNullOblivious = null;

        if ( this.Compilation.ContainsSyntaxTree( tree ) )
        {
            var semanticModel = this.Compilation.GetCachedSemanticModel( tree );
            var nullableContext = semanticModel.GetNullableContext( nodeSpanStart );
            isNullOblivious = (nullableContext & NullableContext.AnnotationsEnabled) == 0;
        }

        return this.GetSyntaxGenerationContext( options, isPartial, isNullOblivious );
    }

    internal SyntaxGenerationContext GetSyntaxGenerationContext(
        SyntaxGenerationOptions options,
        bool isPartial = false,
        bool? isNullOblivious = null,
        string? endOfLine = null )
    {
        endOfLine ??= "\r\n";

        isNullOblivious ??= (((CSharpCompilation) this.Compilation).Options.NullableContextOptions & NullableContextOptions.Annotations)
                            == 0;

        var cacheKey = new SyntaxGenerationContextCacheKey( isNullOblivious.Value, isPartial, endOfLine, options );

        if ( this._syntaxGenerationContextCache.TryGetValue( cacheKey, out var context ) )
        {
            return context;
        }

        return this._syntaxGenerationContextCache.GetOrAdd(
            cacheKey,
            k => new SyntaxGenerationContext(
                this,
                k.IsNullOblivious,
                k.IsPartial,
                k.Options,
                k.EndOfLine ) );
    }

    private record struct SyntaxGenerationContextCacheKey( bool IsNullOblivious, bool IsPartial, string EndOfLine, SyntaxGenerationOptions Options );

    [Memo]
    internal SymbolTranslator SymbolTranslator => new( this );

    public override string ToString() => $"{this.GetType().Name} #{this._id}, Assembly={this.Compilation.AssemblyName}";
}