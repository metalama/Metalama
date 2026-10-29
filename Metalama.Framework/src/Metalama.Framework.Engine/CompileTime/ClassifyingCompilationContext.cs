// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Adds a <see cref="SymbolClassifier"/> to a <see cref="CompilationContext"/>.
/// </summary>
internal sealed class ClassifyingCompilationContext
{
    private readonly IProjectOptions? _projectOptions;

    public ClassifyingCompilationContext( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        this.CompilationContext = compilationContext;
        this.SymbolClassifier = SymbolClassifier.GetSymbolClassifier( serviceProvider, compilationContext );
        this._projectOptions = serviceProvider.GetService<IProjectOptions>();
    }

    public CompilationContext CompilationContext { get; }

    public SymbolClassifier SymbolClassifier { get; }

    /// <summary>
    /// Gets the checker by which <c>ImmutabilityValidator</c> verifies that the static members of compile-time code are
    /// durable (LAMA0892). It caches verdicts keyed by the symbols of this compilation, so it is shared by the validation
    /// of every syntax tree and lives exactly as long as the compilation.
    /// </summary>
    [Memo]
    public StaticMemberDurabilityChecker StaticMemberDurabilityChecker
        => new( this._projectOptions?.DurableTypes ?? [], this._projectOptions?.NonDurableTypes ?? [] );

    public SemanticModelProvider SemanticModelProvider => this.CompilationContext.SemanticModelProvider;

    public ReflectionMapper ReflectionMapper => this.CompilationContext.ReflectionMapper;

    public Compilation SourceCompilation => this.CompilationContext.Compilation;

    public bool ReferencesMetalamaFramework => this.CompilationContext.ReferencesMetalamaFramework;

    public SafeSymbolComparer SymbolComparer => this.CompilationContext.SymbolComparer;
}