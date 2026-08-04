// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Tests that a diagnostic reported on a declaration does not retain the version of the project in which it was
/// reported.
/// </summary>
/// <remarks>
/// <para>
/// The design-time pipeline keeps the diagnostics it produced in the <c>SyntaxTreePipelineResult</c> of the file they
/// were reported on, so that it can serve them again without re-analysing that file. The results of the files the
/// pipeline did not re-analyse are carried forward from one version of the project to the next, therefore a
/// diagnostic reported on a file the user never edits survives for the whole editing session.
/// </para>
/// <para>
/// A <c>DiagnosticDefinition</c> is formatted lazily: the arguments given to <c>WithArguments</c> are stored in the
/// <see cref="Microsoft.CodeAnalysis.DiagnosticDescriptor"/> of the resulting diagnostic and are formatted only when
/// the message is requested. An argument that is a declaration of the code model is therefore held by the stored
/// diagnostic, and a declaration reaches its compilation.
/// </para>
/// <para>
/// Reporting a diagnostic about a declaration, passing that declaration as an argument so that the message can name
/// it, is the single most common thing that a validator does, and it is what the reference validation of
/// Metalama.Extensions.Validation does on every reference it rejects. The first two tests below are a matched pair
/// over the same aspect, differing only in whether the argument is the declaration or its name.
/// </para>
/// <para>
/// The measured cost is one retained version per project, not growth over the length of the session; see
/// <see cref="DiagnosticWithDeclarationArgument_RetentionDoesNotGrowWithTheNumberOfEditedFiles"/>, which passes. The
/// retained version is nevertheless the oldest of the session and is held in addition to the current one, so on a
/// large solution the cost is one whole stale compilation per project that reports such a diagnostic. The defect is
/// tracked by issue #1799.
/// </para>
/// </remarks>
public sealed class DiagnosticArgumentMemoryLeakTests : DesignTimeTestBase
{
    private const string _aspectFileName = "Aspect.cs";

    /// <summary>
    /// The name of the file the diagnostics are reported on. No test edits it, so its result, and the diagnostics it
    /// holds, are produced during the first run and then survive the whole session.
    /// </summary>
    private const string _anchorFileName = "Anchor.cs";

    /// <summary>
    /// The name of the run-time file that the tests edit.
    /// </summary>
    private const string _targetFileName = "Target.cs";

    public DiagnosticArgumentMemoryLeakTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Returns the aspect that reports a warning on every method of its target type.
    /// </summary>
    /// <param name="argumentExpression">
    /// The expression that produces the argument of the diagnostic, which is the only difference between the two
    /// cases.
    /// </param>
    /// <param name="argumentType">The type parameter of the diagnostic definition, which follows from the argument.</param>
    private static string GetAspectCode( string argumentExpression, string argumentType )
        => $$"""
             using Metalama.Framework.Aspects;
             using Metalama.Framework.Code;
             using Metalama.Framework.Diagnostics;
             using System.Linq;

             public class ValidateAttribute : TypeAspect
             {
                 private static readonly DiagnosticDefinition<{{argumentType}}> _warning =
                     new( "MY001", Severity.Warning, "Warning on {0}." );

                 public override void BuildAspect( IAspectBuilder<INamedType> builder )
                     => builder.Outbound.SelectMany( t => t.Methods ).ReportDiagnostic( m => _warning.WithArguments( {{argumentExpression}} ) );
             }
             """;

    private const string _anchorCode = """
                                       [Validate]
                                       public class Anchor
                                       {
                                           public int Value;

                                           public int GetValue() => this.Value;

                                           public void SetValue( int value ) => this.Value = value;
                                       }
                                       """;

    private static string GetTargetCode( int version )
        => $$"""
             public class Target
             {
                 public int Method() => {{version}};
             }
             """;

    /// <summary>
    /// Runs an editing session over a project whose aspect reports diagnostics with the given argument, and returns a
    /// weak reference to the compilation of the first version, which is the version the diagnostics are reported in.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private WeakReference RunEditingSession(
        TestContext testContext,
        TestDesignTimeAspectPipelineFactory factory,
        string sessionName,
        string argumentExpression,
        string argumentType )
    {
        var code = new Dictionary<string, string>
        {
            [_aspectFileName] = GetAspectCode( argumentExpression, argumentType ),
            [_anchorFileName] = _anchorCode,
            [_targetFileName] = GetTargetCode( 0 )
        };

        var simulator = new DesignTimeEditingSimulator( testContext, factory, sessionName, code );

        var initialCompilation = simulator.GetWeakReferenceToCurrentCompilation();
        simulator.Execute();

        const int editCount = 10;

        for ( var version = 1; version <= editCount; version++ )
        {
            simulator.ApplyEdit( _targetFileName, GetTargetCode( version ) );
        }

        var pipeline = simulator.GetPipeline();
        var diagnosticCount = 0;

        foreach ( var result in pipeline.AspectPipelineResult.SyntaxTreeResults )
        {
            diagnosticCount += result.Value.Diagnostics.Length;
        }

        this.TestOutput.WriteLine( $"The pipeline holds {diagnosticCount} diagnostic(s) after {editCount} edits." );

        // Without a diagnostic that survived the session there is nothing whose argument could retain anything, and
        // both cases below would hold for a reason unrelated to the property under test.
        Assert.True(
            diagnosticCount > 0,
            "The aspect reported no diagnostic that survived the session, therefore the test did not exercise the "
            + "retention it was supposed to exercise." );

        return initialCompilation;
    }

    /// <summary>
    /// Verifies that a diagnostic whose argument is the name of the declaration does not retain the version of the
    /// project it was reported in.
    /// </summary>
    /// <remarks>
    /// This is the positive control of the pair. It establishes that the session releases the first version when the
    /// diagnostic carries no code-model object, so that a failure of
    /// <see cref="DiagnosticWithDeclarationArgument_DoesNotRetainTheCompilation"/> is attributable to the argument and
    /// to nothing else.
    /// </remarks>
    [Fact]
    public void DiagnosticWithStringArgument_DoesNotRetainTheCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, "StringArgument", "m.Name", "string" );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation in which a diagnostic with a string argument was reported",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that a diagnostic whose argument is the declaration itself does not retain the version of the project
    /// it was reported in.
    /// </summary>
    [Fact]
    public void DiagnosticWithDeclarationArgument_DoesNotRetainTheCompilation()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var initialCompilation = this.RunEditingSession( testContext, factory, "DeclarationArgument", "m", "IDeclaration" );

        MemoryLeakAssert.Collected(
            initialCompilation,
            "The compilation in which a diagnostic with a declaration argument was reported",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// Verifies that the number of versions retained by the stored diagnostics does not grow with the number of
    /// distinct files the user edits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test states how much the defect costs, which the two tests above do not, and it passes while
    /// <see cref="DiagnosticWithDeclarationArgument_DoesNotRetainTheCompilation"/> fails. The combination is the
    /// informative result: the cost of the defect is a constant, one retained version per project, and not growth
    /// over the length of the session. Editing a different file each time was the arrangement most likely to
    /// accumulate versions, because the results are kept per file and each file's entry could have held the version in
    /// which that file was last analysed, and it does not.
    /// </para>
    /// <para>
    /// A failure here would be a considerably worse defect than the one the pair above reports, therefore this test is
    /// worth keeping even though it passes today.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( 4 )]
    [InlineData( 10 )]
    public void DiagnosticWithDeclarationArgument_RetentionDoesNotGrowWithTheNumberOfEditedFiles( int fileCount )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilations = RunMultiFileEditingSession( testContext, factory, fileCount );

        MemoryLeakAssert.AtMostAlive(
            compilations,
            _allowedSurvivingVersions,
            $"compilations of a session in which {fileCount} different validated files were edited in turn",
            ("pipelineFactory", factory) );
    }

    /// <summary>
    /// The number of versions of the project that the design-time host is allowed to retain, for the same reasons as
    /// in <see cref="DesignTimePipelineMemoryLeakTests"/>.
    /// </summary>
    private const int _allowedSurvivingVersions = 2;

    /// <summary>
    /// Returns the content of one of the several validated files that
    /// <see cref="DiagnosticWithDeclarationArgument_RetentionDoesNotGrowWithTheNumberOfEditedFiles"/> edits in turn.
    /// </summary>
    private static string GetValidatedCode( int index, int version )
        => $$"""
             [Validate]
             public class Validated{{index}}
             {
                 public int Method() => {{version}};
             }
             """;

    /// <summary>
    /// Runs a session that edits each of several validated files once, and returns weak references to the version
    /// produced by each edit.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference[] RunMultiFileEditingSession( TestContext testContext, TestDesignTimeAspectPipelineFactory factory, int fileCount )
    {
        var code = new Dictionary<string, string> { [_aspectFileName] = GetAspectCode( "m", "IDeclaration" ) };

        for ( var i = 0; i < fileCount; i++ )
        {
            code[$"Validated{i}.cs"] = GetValidatedCode( i, 0 );
        }

        var simulator = new DesignTimeEditingSimulator( testContext, factory, $"MultiFile{fileCount}", code );
        simulator.Execute();

        var compilations = new WeakReference[fileCount];

        // Each file is edited once, in turn, so that the result of each is produced in a different version of the
        // project. This is what an ordinary editing session looks like over a working day.
        for ( var i = 0; i < fileCount; i++ )
        {
            compilations[i] = simulator.EditAndExecute( $"Validated{i}.cs", GetValidatedCode( i, 1 ) );
        }

        return compilations;
    }
}
