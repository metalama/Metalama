// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Simulates a design-time editing session: an initial compilation followed by an arbitrary number of edits, each of
/// which produces a new <see cref="Compilation"/> that is then submitted to the design-time pipeline.
/// </summary>
/// <remarks>
/// <para>
/// An edit is applied with <see cref="Compilation.ReplaceSyntaxTree"/> rather than by building a new compilation from
/// the complete source, because that is what Roslyn does when the user types in the editor. The syntax trees of the
/// files that were not edited therefore remain the same instances across versions, and only the edited tree and the
/// <see cref="Compilation"/> itself are new. A test that rebuilt the whole compilation on each iteration would
/// exaggerate the amount of garbage produced and would not reproduce the situation in which the leak is reported.
/// </para>
/// <para>
/// The metadata references of the initial compilation are preserved for the same reason: Roslyn reuses reference
/// objects across edits, and creating new ones would make every version differ in its references, which is a
/// different scenario from the one under test.
/// </para>
/// </remarks>
internal sealed class DesignTimeEditingSimulator
{
    private readonly TestContext _testContext;
    private readonly TestDesignTimeAspectPipelineFactory _factory;
    private readonly CSharpParseOptions _parseOptions;

    /// <summary>
    /// Gets the compilation that represents the current state of the simulated editor.
    /// </summary>
    /// <remarks>
    /// The property is private for the same reason as <see cref="Edit"/>: a test that read it into a local variable
    /// would keep that version of the compilation alive for the rest of its own method.
    /// </remarks>
    private Compilation CurrentCompilation { get; set; }

    /// <summary>
    /// Gets the number of edits applied since the session was created.
    /// </summary>
    public int EditCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeEditingSimulator"/> class.
    /// </summary>
    /// <param name="testContext">The test context used to create compilations.</param>
    /// <param name="factory">The pipeline factory that plays the role of the design-time host.</param>
    /// <param name="assemblyName">The assembly name, which must be stable across edits so that every version maps to the same project.</param>
    /// <param name="code">The initial content of the project, indexed by file name.</param>
    public DesignTimeEditingSimulator(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        string assemblyName,
        IReadOnlyDictionary<string, string> code )
    {
        this._testContext = testContext;
        this._factory = factory;
        this.CurrentCompilation = testContext.CreateCSharpCompilation( code, assemblyName: assemblyName );
        this._parseOptions = (CSharpParseOptions) this.CurrentCompilation.SyntaxTrees.First().Options;
    }

    /// <summary>
    /// Replaces the content of a single file, producing a new <see cref="Compilation"/> in the same way as Roslyn does
    /// when the user types.
    /// </summary>
    /// <param name="fileName">The path of the file to replace, as given to the constructor.</param>
    /// <param name="newContent">The new content of the file.</param>
    /// <returns>The new compilation, which also becomes <see cref="CurrentCompilation"/>.</returns>
    /// <remarks>
    /// The method is private because a caller that stores the returned compilation in a local variable keeps that
    /// version alive for the rest of its own method, which is precisely what these tests must avoid. Callers use
    /// <see cref="ApplyEdit"/> or <see cref="EditAndExecute"/> instead.
    /// </remarks>
    private Compilation Edit( string fileName, string newContent )
    {
        var oldTree = this.CurrentCompilation.SyntaxTrees.Single(
            t => string.Equals( t.FilePath, fileName, StringComparison.OrdinalIgnoreCase ) );

        var newTree = CSharpSyntaxTree.ParseText( newContent, this._parseOptions, fileName, encoding: oldTree.Encoding );

        this.CurrentCompilation = this.CurrentCompilation.ReplaceSyntaxTree( oldTree, newTree );
        this.EditCount++;

        return this.CurrentCompilation;
    }

    /// <summary>
    /// Runs the design-time pipeline on <see cref="CurrentCompilation"/>, in the same way as the diagnostic analyzer
    /// and the source generator do when Roslyn reports a new compilation.
    /// </summary>
    /// <remarks>
    /// The result is intentionally not returned. Holding a result on the caller stack would keep the corresponding
    /// compilation alive and would mask the very retention that the tests are looking for.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public void Execute()
    {
        if ( !this._factory.TryExecute( this._testContext.ProjectOptions, this.CurrentCompilation, default, out _ ) )
        {
            throw new InvalidOperationException(
                $"The design-time pipeline failed on edit {this.EditCount}. A memory test requires a pipeline that runs successfully." );
        }
    }

    /// <summary>
    /// Applies an edit and runs the pipeline, without exposing the resulting compilation to the caller.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public void ApplyEdit( string fileName, string newContent )
    {
        this.Edit( fileName, newContent );
        this.Execute();
    }

    /// <summary>
    /// Applies an edit and runs the pipeline, returning a weak reference to the compilation that was analyzed.
    /// </summary>
    /// <remarks>
    /// The compilation is never returned by a strong reference, so that the caller cannot accidentally keep it alive.
    /// The method is not inlinable, so that the local variable holding the compilation belongs to a stack frame that
    /// no longer exists when the caller resumes. This matters because a debug build keeps every local alive until the
    /// end of the method that declares it.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public WeakReference EditAndExecute( string fileName, string newContent )
    {
        var compilation = this.Edit( fileName, newContent );
        this.Execute();

        return new WeakReference( compilation );
    }

    /// <summary>
    /// Returns a weak reference to <see cref="CurrentCompilation"/>, without exposing a strong reference to it.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public WeakReference GetWeakReferenceToCurrentCompilation() => new( this.CurrentCompilation );

    /// <summary>
    /// Returns weak references to the syntax trees of <see cref="CurrentCompilation"/>, without exposing strong
    /// references to them.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public WeakReference[] GetWeakReferencesToCurrentSyntaxTrees()
        => this.CurrentCompilation.SyntaxTrees.Select( t => new WeakReference( t ) ).ToArray();

    /// <summary>
    /// Returns a weak reference to the syntax tree of a given file in <see cref="CurrentCompilation"/>, without
    /// exposing a strong reference to it.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public WeakReference GetWeakReferenceToSyntaxTree( string fileName )
        => new(
            this.CurrentCompilation.SyntaxTrees.Single(
                t => string.Equals( t.FilePath, fileName, StringComparison.OrdinalIgnoreCase ) ) );

    /// <summary>
    /// Gets the pipeline that the factory has created for the current compilation.
    /// </summary>
    public DesignTimeAspectPipeline GetPipeline() => this._factory.CreatePipeline( this.CurrentCompilation );
}
