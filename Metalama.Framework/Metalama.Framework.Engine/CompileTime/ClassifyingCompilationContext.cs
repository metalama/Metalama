// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Adds a <see cref="SymbolClassifier"/> to a <see cref="CompilationContext"/>.
/// </summary>
internal sealed class ClassifyingCompilationContext
{
    public ClassifyingCompilationContext( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        this.CompilationContext = compilationContext;
        this.SymbolClassifier = SymbolClassifier.GetSymbolClassifier( serviceProvider, compilationContext );
    }

    public CompilationContext CompilationContext { get; }

    public SymbolClassifier SymbolClassifier { get; }

    public SemanticModelProvider SemanticModelProvider => this.CompilationContext.SemanticModelProvider;

    public ReflectionMapper ReflectionMapper => this.CompilationContext.ReflectionMapper;

    public Compilation SourceCompilation => this.CompilationContext.Compilation;

    public SafeSymbolComparer SymbolComparer => this.CompilationContext.SymbolComparer;
}