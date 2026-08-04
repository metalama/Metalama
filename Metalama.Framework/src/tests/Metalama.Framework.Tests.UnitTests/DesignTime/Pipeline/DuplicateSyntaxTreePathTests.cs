// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests for https://github.com/metalama/Metalama/issues/1742: Metalama indexes the syntax trees of a compilation by
/// <see cref="SyntaxTree.FilePath"/> and assumes the path identifies the tree. Roslyn makes no such guarantee.
/// </summary>
/// <remarks>
/// <para>
/// The condition does not arise from the command-line compiler, which deduplicates source files by normalized path,
/// reports <c>CS2002</c> and drops the duplicate. It arises from Roslyn Workspaces, which has no equivalent step: the
/// project system creates one document per <c>Compile</c> item, so overlapping globs, an explicit <c>Include</c> that
/// repeats a glob, <c>Link</c> metadata pointing two items at one file, or a shared project all produce two
/// <see cref="SyntaxTree"/> objects with one path in one <see cref="Compilation"/>. Nothing in the user's project is
/// invalid, and nothing Metalama does can prevent it.
/// </para>
/// <para>
/// Each test names the site it covers. They are grouped here rather than spread over the files they exercise, because
/// the value of the set is that the sites disagree: two of them tolerate the condition, four throw, and one tolerates
/// it in its own index while leaving the compilation it describes unchanged.
/// </para>
/// </remarks>
public sealed class DuplicateSyntaxTreePathTests : UnitTestClass
{
    public DuplicateSyntaxTreePathTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _duplicatePath = "Duplicated.cs";

    /// <summary>
    /// The two trees declare different types, so the compilation has no C# error. This is the faithful shape of a
    /// generated document whose hint name lands on the path of a real file, and it keeps the tests free of the
    /// <c>CS0101</c> noise that identical content would produce.
    /// </summary>
    private const string _firstCode = "public class FirstType { }";

    private const string _secondCode = "public class SecondType { }";

    /// <summary>
    /// Builds a compilation holding two distinct syntax trees that share <see cref="_duplicatePath"/>. Roslyn accepts
    /// this: <see cref="Compilation.AddSyntaxTrees(SyntaxTree[])"/> rejects only the same instance twice.
    /// </summary>
    private static (CSharpCompilation Compilation, SyntaxTree First, SyntaxTree Second) CreateCompilationWithDuplicatePath(
        TestContext testContext )
    {
        var parseOptions = testContext.GetCompilationParseOptions();

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string> { [_duplicatePath] = _firstCode } );
        var first = compilation.SyntaxTrees.Single( t => t.FilePath == _duplicatePath );

        var second = SyntaxFactory.ParseSyntaxTree( _secondCode, path: _duplicatePath, options: parseOptions );

        return (compilation.AddSyntaxTrees( second ), first, second);
    }

    /// <summary>
    /// Site 1, the fatal one. <c>GetIndexedSyntaxTreesCore</c> builds the index with <c>ToImmutableDictionary</c>,
    /// which throws <see cref="System.ArgumentException"/> on the second tree. The throw happens inside a
    /// <c>WeakCache</c> factory, so nothing is memoized and every later request for the same compilation repeats it.
    /// </summary>
    [Fact]
    public void GetIndexedSyntaxTreesTolerates()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        var index = compilation.GetIndexedSyntaxTrees();

        Assert.True( index.ContainsKey( DocumentKey.FromPath( _duplicatePath ) ) );
    }

    /// <summary>
    /// The same site reached through the entry point that fails in production.
    /// <c>DesignTimeAspectPipeline.ExecuteAsync</c> calls <see cref="PartialCompilation.CreateComplete"/> whenever the
    /// pipeline status is <c>Default</c>, which is the case on the first execution for a project and for every
    /// referenced project, so a single duplicated path stops the pipeline before it has produced anything.
    /// </summary>
    [Fact]
    public void CreateCompleteTolerates()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        Assert.NotEmpty( partialCompilation.SyntaxTreeCollection );
    }

    /// <summary>
    /// The invariant that distinguishes a correct fix from a fix that trades a crash for wrong output: the set of
    /// trees a <see cref="PartialCompilation"/> exposes must cover the trees of the <see cref="Compilation"/> it
    /// carries.
    /// </summary>
    /// <remarks>
    /// <c>LinkerInjectionStep</c> rewrites <c>SyntaxTrees.Values</c>. A tree that stays in the Roslyn compilation
    /// while being absent from that collection keeps its declarations in the code model and is never rewritten, so
    /// aspects apply to some declarations of the project and silently do not apply to others. Making the index merely
    /// tolerant, by keeping the first tree per path and dropping the rest, produces exactly that state. Removing the
    /// dropped trees from the compilation as well is what keeps the two in agreement.
    /// </remarks>
    [Fact]
    public void CompletePartialCompilationCoversItsCompilation()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        Assert.Equal(
            partialCompilation.Compilation.SyntaxTrees.OrderBy( t => t.ToString() ),
            partialCompilation.SyntaxTreeCollection.OrderBy( t => t.ToString() ) );
    }

    /// <summary>
    /// The same invariant for the partial path. Site 2, <c>GetClosure.AddTree</c>, already tolerates the condition by
    /// keeping the first tree of a path and discarding the rest, so this compilation reaches the pipeline with a tree
    /// that no stage will visit.
    /// </summary>
    [Fact]
    public void PartialCompilationCoversItsCompilation()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, first, second) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreatePartial( compilation, new[] { first, second } );

        Assert.Equal(
            partialCompilation.Compilation.SyntaxTrees.OrderBy( t => t.ToString() ),
            partialCompilation.SyntaxTreeCollection.OrderBy( t => t.ToString() ) );
    }

    /// <summary>
    /// The consequence of the previous test, stated as behaviour rather than as a set comparison: a rewrite of every
    /// syntax tree of the partial compilation must leave no tree of the resulting compilation unrewritten.
    /// <see cref="PartialCompilationExtensions.UpdateSyntaxTrees(Metalama.Framework.Engine.CodeModel.IPartialCompilation,System.Func{SyntaxTree,SyntaxTree},System.Threading.CancellationToken)"/>
    /// is the public analogue of the loop the linker runs.
    /// </summary>
    [Fact]
    public void EverySyntaxTreeIsRewritten()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, first, second) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreatePartial( compilation, new[] { first, second } );

        const string marker = "// rewritten";

        var rewritten = partialCompilation.UpdateSyntaxTrees(
            tree => tree.WithRootAndOptions(
                tree.GetCompilationUnitRoot().WithLeadingTrivia( SyntaxFactory.Comment( marker ) ),
                tree.Options ) );

        var notRewritten = rewritten.Compilation.SyntaxTrees
            .Where( t => t.FilePath == _duplicatePath && !t.ToString().ContainsOrdinal( marker ) )
            .ToList();

        Assert.Empty( notRewritten );
    }

    /// <summary>
    /// Sites 3 and 4, the diff layer, which is the only place that already detects the condition and reports it. It
    /// resolves the duplicate in the version index by keeping the first tree of a path, but
    /// <c>ProjectVersion.CompilationToAnalyze</c> removes only the trees generated by Metalama, so the compilation the
    /// pipeline then analyses still holds both. That mismatch is what leaves every downstream index describing a
    /// compilation it does not match.
    /// </summary>
    [Fact]
    public void CompilationToAnalyzeHasUniquePaths()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        var projectVersion = ProjectVersion.Create(
            compilation,
            compilation.GetProjectKey(),
            new DiffStrategy( isTest: true, detectCompileTimeCode: true, detectPartialTypes: true ),
            serviceProvider: testContext.ServiceProvider.Underlying );

        var duplicatedPaths = projectVersion.CompilationToAnalyze.SyntaxTrees
            .GroupBy( t => t.FilePath )
            .Where( g => g.Count() > 1 )
            .Select( g => g.Key )
            .ToList();

        Assert.Empty( duplicatedPaths );
    }

    /// <summary>
    /// The user-visible failure: the design-time pipeline of a project holding one duplicated path never completes, so
    /// the project has no Metalama diagnostic, no code lens and no generated document, on the first execution and on
    /// every one after it.
    /// </summary>
    [Fact]
    public void DesignTimePipelineSucceeds()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );
        Assert.NotNull( result );
    }

    /// <summary>
    /// The code model must describe exactly the compilation the pipeline analyzes, and no more. Under the one-document
    /// model the second tree is not a second document, so its declarations are absent from both. The alternative, in
    /// which the type is visible to aspects while its tree is never rewritten, is the silent failure that
    /// <see cref="EverySyntaxTreeIsRewritten"/> guards against.
    /// </summary>
    /// <remarks>
    /// This is the case in which the model costs something: the two trees here declare different types, which is the
    /// shape of a generated document whose hint name lands on the path of a real file, so a real declaration is
    /// dropped. That is why the condition is reported to the user rather than merely resolved. See
    /// <see cref="DesignTimePipelineReportsTheDuplicatePath"/>.
    /// </remarks>
    [Fact]
    public void CodeModelMatchesTheAnalyzedCompilation()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, _, _) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreateComplete( compilation );

        var compilationModel = CompilationModel.CreateInitialInstance(
            new ProjectModel( compilation, testContext.ServiceProvider ),
            partialCompilation );

        var typeNames = compilationModel.Types.SelectAsReadOnlyCollection( t => t.Name ).OrderBy( n => n );

        Assert.Equal( new[] { "FirstType" }, typeNames );
    }

    /// <summary>
    /// The condition is resolved without failing the project, but it is a project-file defect that costs the user a
    /// declaration, so it must be reported where the user can see it.
    /// </summary>
    [Fact]
    public void DesignTimePipelineReportsTheDuplicatePath()
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );

        using var testContext = this.CreateTestContext( additionalServices );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var (compilation, first, _) = CreateCompilationWithDuplicatePath( testContext );

        var analyzer = new TheDiagnosticAnalyzer( factory.ServiceProvider );

        var analysisContext = new TestSemanticModelAnalysisContext( compilation.GetSemanticModel( first ), testContext.ProjectOptions );

        analyzer.AnalyzeSemanticModel( analysisContext );

        Assert.Contains(
            analysisContext.ReportedDiagnostics,
            d => d.Id == "LAMA0307" && d.GetMessage().ContainsOrdinal( _duplicatePath ) );
    }

    /// <summary>
    /// <see cref="PartialCompilation.Update"/> resolved the tree a <c>SyntaxTreeTransformation</c> replaces by its
    /// path, while <c>Compilation.ReplaceSyntaxTree</c>, which it calls, resolves it by identity. The transformation
    /// carries <c>OldTree</c>, so the identity is available and the path lookup was unnecessary. Replacing the tree
    /// that represents the document must update the index and the compilation consistently.
    /// </summary>
    [Fact]
    public void UpdateReplacesTheNamedTree()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, first, second) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreatePartial( compilation, new[] { first, second } );

        var tree = Assert.Single( partialCompilation.SyntaxTreeCollection );

        var replacement = SyntaxFactory.ParseSyntaxTree(
            "public class Replaced { }",
            path: _duplicatePath,
            options: testContext.GetCompilationParseOptions() );

        var updated = partialCompilation.Update( new[] { SyntaxTreeTransformation.ReplaceTree( tree, replacement ) } );

        Assert.Contains( replacement, updated.SyntaxTreeCollection );

        Assert.Equal(
            updated.Compilation.SyntaxTrees.OrderBy( t => t.ToString() ),
            updated.SyntaxTreeCollection.OrderBy( t => t.ToString() ) );
    }

    /// <summary>
    /// A transformation that names a syntax tree the partial compilation does not hold must be rejected where it is
    /// named. Under the one-document model, a tree that lost its path to an earlier tree is not part of the analysed
    /// compilation, so replacing it is a programming error rather than an operation with an ambiguous result.
    /// </summary>
    [Fact]
    public void UpdateRejectsATreeThatIsNotInTheCompilation()
    {
        using var testContext = this.CreateTestContext();

        var (compilation, first, second) = CreateCompilationWithDuplicatePath( testContext );

        var partialCompilation = PartialCompilation.CreatePartial( compilation, new[] { first, second } );

        var absentTree = partialCompilation.SyntaxTreeCollection.Contains( first ) ? second : first;

        var replacement = SyntaxFactory.ParseSyntaxTree(
            "public class Replaced { }",
            path: _duplicatePath,
            options: testContext.GetCompilationParseOptions() );

        Assert.Throws<KeyNotFoundException>(
            () => partialCompilation.Update( new[] { SyntaxTreeTransformation.ReplaceTree( absentTree, replacement ) } ) );
    }
}
