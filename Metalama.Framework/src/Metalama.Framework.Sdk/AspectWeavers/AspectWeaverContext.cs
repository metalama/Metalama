// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Options;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.AspectWeavers;

/// <summary>
/// Provides context and services for <see cref="IAspectWeaver"/> implementations to transform Roslyn compilations.
/// </summary>
/// <remarks>
/// <para>
/// This context is passed to <see cref="IAspectWeaver.TransformAsync"/> and provides:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="AspectInstances"/>: The aspect instances to process (keyed by target <see cref="ISymbol"/>).</description></item>
/// <item><description><see cref="Compilation"/>: The current <see cref="IPartialCompilation"/> to read and modify.</description></item>
/// <item><description>Helper methods like <see cref="RewriteAspectTargetsAsync"/> for common transformation patterns.</description></item>
/// <item><description><see cref="GeneratedCodeAnnotation"/>: The annotation to apply to generated syntax nodes.</description></item>
/// </list>
/// <para>
/// <b>Formatting:</b> Your weaver does not need to format output code. Metalama handles formatting at the end of the pipeline.
/// However, your weaver must annotate generated nodes using methods from <see cref="Formatting.FormattingAnnotations"/>.
/// </para>
/// </remarks>
/// <seealso cref="IAspectWeaver"/>
/// <seealso cref="IPartialCompilation"/>
/// <seealso cref="Formatting.FormattingAnnotations"/>
/// <seealso href="@aspect-weavers"/>
[CompileTime]
[PublicAPI]
public sealed partial class AspectWeaverContext
{
    private readonly Action<Diagnostic> _addDiagnostic;
    private readonly ISdkDeclarationFactory _declarationFactory;
    private readonly IHierarchicalOptionsManager _optionsManager;

    private IPartialCompilation _compilation;

    public ProjectServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the <see cref="IAspectClass"/> metadata for the aspect type being processed.
    /// </summary>
    public IAspectClass AspectClass { get; }

    /// <summary>
    /// Gets the dictionary of aspect instances that must be processed, keyed by their target <see cref="ISymbol"/>.
    /// </summary>
    /// <remarks>
    /// <para>Iterate over this dictionary to process each aspect instance. Use the <see cref="ISymbol"/> key to locate
    /// the target declaration's syntax via <see cref="ISymbol.DeclaringSyntaxReferences"/>.</para>
    /// <para>To map the Metalama code model to an <see cref="ISymbol"/>, use extension methods in <see cref="SymbolExtensions"/>.</para>
    /// </remarks>
    public IReadOnlyDictionary<ISymbol, IAspectInstance> AspectInstances { get; }

    /// <summary>
    /// Gets the current <see cref="IProject"/> being compiled.
    /// </summary>
    public IProject Project { get; }

    /// <summary>
    /// Gets or sets the compilation being transformed.
    /// </summary>
    /// <remarks>
    /// <para>You can modify the compilation by either:</para>
    /// <list type="bullet">
    /// <item><description>Using helper methods like <see cref="RewriteAspectTargetsAsync"/> which automatically update this property.</description></item>
    /// <item><description>Setting this property directly with a new <see cref="IPartialCompilation"/> derived from the current value using methods like <see cref="IPartialCompilation.WithSyntaxTreeTransformations"/>.</description></item>
    /// </list>
    /// <para>The new compilation must be derived from the initial value; setting an unrelated compilation throws <see cref="ArgumentOutOfRangeException"/>.</para>
    /// </remarks>
    public IPartialCompilation Compilation
    {
        get => this._compilation;

        set
        {
            if ( ((IPartialCompilationInternal) value).InitialCompilation != ((IPartialCompilationInternal) this._compilation).InitialCompilation )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The compilation must have been derived from the initial value of the Compilation property." );
            }

            this._compilation = value;
        }
    }

    public ICompilationServices CompilationServices { get; }

    public T GetOptions<T>( ISymbol symbol )
        where T : IHierarchicalOptions, new()
    {
        if ( !this._declarationFactory.TryGetDeclaration( symbol, out var declaration ) )
        {
            return new T();
        }

        return (T) this._optionsManager.GetOptions( declaration, typeof(T) ) ?? new T();
    }

    private CancellationToken GetCancellationToken( in CancellationToken cancellationToken )
        => cancellationToken == default ? this.CancellationToken : cancellationToken;

    /// <summary>
    /// Rewrites all syntax trees in the compilation using a shared <see cref="CSharpSyntaxRewriter"/>.
    /// </summary>
    /// <param name="rewriter">A thread-safe <see cref="CSharpSyntaxRewriter"/>. Since syntax trees are processed in parallel,
    /// this rewriter must be safe to call concurrently from multiple threads.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <remarks>
    /// This method processes all syntax trees in the compilation in parallel and automatically updates the <see cref="Compilation"/> property.
    /// Use this when you need to transform code throughout the entire compilation, not just aspect targets.
    /// </remarks>
    public async Task RewriteSyntaxTreesAsync( CSharpSyntaxRewriter rewriter, CancellationToken cancellationToken = default )
        => this.Compilation = await this.Compilation.RewriteSyntaxTreesAsync(
            rewriter,
            this.ServiceProvider,
            this.GetCancellationToken( cancellationToken ) );

    /// <summary>
    /// Rewrites all syntax trees in the compilation using a factory that creates a <see cref="CSharpSyntaxRewriter"/> per syntax tree.
    /// </summary>
    /// <param name="rewriterFactory">A delegate that creates a <see cref="CSharpSyntaxRewriter"/> given the root <see cref="SyntaxNode"/> of a syntax tree.
    /// Called once for each <see cref="SyntaxTree"/> in the compilation.
    /// If the delegate returns the same rewriter instance for multiple trees, that instance must be thread-safe.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <remarks>
    /// <para>This method processes all syntax trees in the compilation in parallel and automatically updates the <see cref="Compilation"/> property.</para>
    /// <para>Use this overload when each syntax tree requires its own rewriter state. For a shared rewriter, use <see cref="RewriteSyntaxTreesAsync(CSharpSyntaxRewriter, CancellationToken)"/>.</para>
    /// </remarks>
    public async Task RewriteSyntaxTreesAsync( Func<SyntaxNode, CSharpSyntaxRewriter> rewriterFactory, CancellationToken cancellationToken = default )
        => this.Compilation = await this.Compilation.RewriteSyntaxTreesAsync(
            rewriterFactory,
            this.ServiceProvider,
            this.GetCancellationToken( cancellationToken ) );

    /// <summary>
    /// Rewrites only the syntax nodes targeted by aspects, using a thread-safe <see cref="CSharpSyntaxRewriter"/>.
    /// </summary>
    /// <param name="rewriter">A thread-safe <see cref="CSharpSyntaxRewriter"/> whose <c>Visit</c> method is invoked for each declaration
    /// that is the target of an aspect in <see cref="AspectInstances"/>. For partial classes or methods, the <c>Visit</c> method is invoked
    /// for each partial declaration separately.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <remarks>
    /// <para>This method is more efficient than <see cref="RewriteSyntaxTreesAsync(CSharpSyntaxRewriter, CancellationToken)"/> when you only need to transform
    /// declarations that have the aspect applied, because it only visits targeted declarations rather than the entire syntax tree.</para>
    /// <para>The method processes syntax trees in parallel. Your rewriter must be thread-safe.</para>
    /// <para>The <see cref="Compilation"/> property is automatically updated after the transformation.</para>
    /// </remarks>
    public async Task RewriteAspectTargetsAsync( CSharpSyntaxRewriter rewriter, CancellationToken cancellationToken = default )
    {
        cancellationToken = this.GetCancellationToken( cancellationToken );

        var taskScheduler = this.ServiceProvider.GetRequiredService<IConcurrentTaskRunner>();

        var nodesBySyntaxTree = new ConcurrentDictionary<SyntaxTree, ConcurrentQueue<SyntaxReference>>();

        await taskScheduler.RunConcurrentlyAsync( this.AspectInstances, ProcessAspectInstance, cancellationToken );

        void ProcessAspectInstance( KeyValuePair<ISymbol, IAspectInstance> aspectInstance )
        {
            var symbol = aspectInstance.Value.TargetDeclaration.GetSymbol( this._compilation.Compilation );

            if ( symbol != null )
            {
                foreach ( var syntaxReference in symbol.DeclaringSyntaxReferences )
                {
                    nodesBySyntaxTree
                        .GetOrAdd( syntaxReference.SyntaxTree, _ => new ConcurrentQueue<SyntaxReference>() )
                        .Enqueue( syntaxReference );
                }
            }
        }

        ConcurrentLinkedList<SyntaxTreeTransformation> modifiedSyntaxTrees = new();

        await taskScheduler.RunConcurrentlyAsync( nodesBySyntaxTree, ProcessSyntaxTreeAsync, cancellationToken );

        async Task ProcessSyntaxTreeAsync( KeyValuePair<SyntaxTree, ConcurrentQueue<SyntaxReference>> group )
        {
            cancellationToken.ThrowIfCancellationRequested();

            var oldTree = @group.Key;
            var outerRewriter = new Rewriter( group.Value.Select( r => r.GetSyntax() ).ToImmutableHashSet(), rewriter );
            var oldRoot = await oldTree.GetRootAsync( cancellationToken );
            var newRoot = outerRewriter.Visit( oldRoot )!;

            if ( oldRoot != newRoot )
            {
                modifiedSyntaxTrees.Add( SyntaxTreeTransformation.ReplaceTree( oldTree, oldTree.WithRootAndOptions( newRoot, oldTree.Options ) ) );
            }
        }

        this.Compilation = this.Compilation.WithSyntaxTreeTransformations( modifiedSyntaxTrees.ToList() );
    }

    internal AspectWeaverContext(
        IAspectClass aspectClass,
        IReadOnlyDictionary<ISymbol, IAspectInstance> aspectInstances,
        IPartialCompilation compilation,
        Action<Diagnostic> addDiagnostic,
        ProjectServiceProvider serviceProvider,
        IProject project,
        SyntaxAnnotation generatedCodeAnnotation,
        ICompilationServices compilationServices,
        ISdkDeclarationFactory declarationFactory,
        IHierarchicalOptionsManager optionsManager,
        CancellationToken cancellationToken )
    {
        this.AspectClass = aspectClass;
        this.AspectInstances = aspectInstances;
        this._compilation = compilation;
        this._addDiagnostic = addDiagnostic;
        this._declarationFactory = declarationFactory;
        this.Project = project;
        this.GeneratedCodeAnnotation = generatedCodeAnnotation;
        this.CancellationToken = cancellationToken;
        this._optionsManager = optionsManager;
        this.ServiceProvider = serviceProvider;
        this.CompilationServices = compilationServices;
    }

    /// <summary>
    /// Reports a <see cref="Diagnostic"/> (error, warning, or informational message) to the compilation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report. Create using <c>Diagnostic.Create()</c> with a <see cref="DiagnosticDescriptor"/>.</param>
    public void ReportDiagnostic( Diagnostic diagnostic ) => this._addDiagnostic( diagnostic );

    /// <summary>
    /// Gets the <see cref="SyntaxAnnotation"/> that must be applied to all code generated by the weaver.
    /// </summary>
    /// <remarks>
    /// <para>Apply this annotation to generated syntax nodes using <see cref="FormattingAnnotations.WithGeneratedCodeAnnotation{T}(T, SyntaxAnnotation)"/>.</para>
    /// <para>This annotation enables Metalama to distinguish generated code from source code for formatting, diff views, and IDE features.</para>
    /// <para>For source code nodes within generated code, use <see cref="FormattingAnnotations.WithSourceCodeAnnotation{T}(T)"/> instead.</para>
    /// </remarks>
    /// <seealso cref="FormattingAnnotations"/>
    public SyntaxAnnotation GeneratedCodeAnnotation { get; }

    public CancellationToken CancellationToken { get; }

    // TODO: add support for suppressions.
}