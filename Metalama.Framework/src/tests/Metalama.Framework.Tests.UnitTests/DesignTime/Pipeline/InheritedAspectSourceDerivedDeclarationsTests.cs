// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests the inherited-aspect source of a consuming project when the declarations derived from the target of the
/// inherited aspect are members introduced by an aspect.
/// </summary>
/// <remarks>
/// <para>
/// These tests were written while investigating https://github.com/metalama/Metalama/issues/2048, which reports a
/// <c>SymbolNotFoundException</c> raised by <c>IntroducedMember.DeclaringType</c> under
/// <c>SourceMember.GetDerivedDeclarationsCore</c>, itself called from
/// <c>TransitivePipelineContributorSource.InheritedAspectSourceImpl.CollectAspectInstancesAsync</c>.
/// </para>
/// <para>
/// None of these scenarios reproduces that exception. They are kept because the path they exercise had no
/// coverage: <c>GetDerivedDeclarationsCore</c> reading the declaring type of an introduced member, from the
/// inherited-aspect source of a project that consumes an inheritable aspect of a referenced project.
/// </para>
/// </remarks>
public sealed class InheritedAspectSourceDerivedDeclarationsTests : DesignTimePipelineTestsBase
{
    public InheritedAspectSourceDerivedDeclarationsTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// The referenced project. The inheritable aspect reports a diagnostic on its target, which is how the tests
    /// observe that the aspect reached a declaration of the consuming project.
    /// </summary>
    private const string _libraryCode = """
                                        using Metalama.Framework.Advising;
                                        using Metalama.Framework.Aspects;
                                        using Metalama.Framework.Code;
                                        using Metalama.Framework.Diagnostics;

                                        [Inheritable]
                                        public class ReportAspect : MethodAspect
                                        {
                                            private static readonly DiagnosticDefinition<string> _applied =
                                                new( "MY001", Severity.Warning, "ReportAspect applied to {0}." );

                                            public override void BuildAspect( IAspectBuilder<IMethod> builder )
                                            {
                                                builder.Diagnostics.Report( _applied.WithArguments( builder.Target.Name ) );
                                            }
                                        }

                                        public class BaseClass
                                        {
                                            [ReportAspect]
                                            public virtual void Method() { }
                                        }

                                        public interface IService
                                        {
                                            [ReportAspect]
                                            void Serve();
                                        }
                                        """;

    /// <summary>
    /// The consuming project declares the override in source.
    /// </summary>
    private const string _appSourceOverrideCode = """
                                                  public class DerivedClass : BaseClass
                                                  {
                                                      public override void Method() { }
                                                  }
                                                  """;

    /// <summary>
    /// The consuming project does not declare the override in source. One of its own aspects introduces it, and
    /// is ordered before the inheritable aspect, so that the introduced member is already part of the code model
    /// when the inherited-aspect source enumerates the derived declarations.
    /// </summary>
    private const string _appOverrideIntroducedCode = """
                                                      using Metalama.Framework.Aspects;

                                                      [assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceOverrideAspect), typeof(ReportAspect) )]

                                                      public class IntroduceOverrideAspect : TypeAspect
                                                      {
                                                          [Introduce( WhenExists = OverrideStrategy.Override )]
                                                          public virtual void Method() { }
                                                      }

                                                      [IntroduceOverrideAspect]
                                                      public partial class DerivedClass : BaseClass { }
                                                      """;

    /// <summary>
    /// The same consuming project with a member added to the derived type, which is the edit that
    /// <see cref="IntroducedDerivedDeclarationAcrossCompilationVersions"/> applies between its two runs.
    /// </summary>
    private const string _appOverrideIntroducedAndEditedCode = """
                                                               using Metalama.Framework.Aspects;

                                                               [assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceOverrideAspect), typeof(ReportAspect) )]

                                                               public class IntroduceOverrideAspect : TypeAspect
                                                               {
                                                                   [Introduce( WhenExists = OverrideStrategy.Override )]
                                                                   public virtual void Method() { }
                                                               }

                                                               [IntroduceOverrideAspect]
                                                               public partial class DerivedClass : BaseClass
                                                               {
                                                                   public int More() => 42;
                                                               }
                                                               """;

    /// <summary>
    /// The implementation of the interface member of the referenced project is introduced by an aspect of the
    /// consuming project instead of being declared in source.
    /// </summary>
    private const string _appInterfaceIntroducedCode = """
                                                       using Metalama.Framework.Advising;
                                                       using Metalama.Framework.Aspects;
                                                       using Metalama.Framework.Code;

                                                       [assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ImplementServiceAspect), typeof(ReportAspect) )]

                                                       public class ImplementServiceAspect : TypeAspect
                                                       {
                                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                                           {
                                                               builder.ImplementInterface( typeof(IService) );
                                                           }

                                                           [InterfaceMember]
                                                           public void Serve() { }
                                                       }

                                                       [ImplementServiceAspect]
                                                       public partial class ServiceImpl { }
                                                       """;

    /// <summary>
    /// Verifies that the inheritable aspect of the referenced project is applied to the override that the
    /// consuming project declares in source. This is the case that must work, and it establishes that the
    /// inherited-aspect source does reach the consuming project in this test setup.
    /// </summary>
    [Fact]
    public void DerivedDeclarationDeclaredInSourceReceivesTheAspect() => this.RunTwoProjects( _appSourceOverrideCode );

    /// <summary>
    /// Verifies that the inheritable aspect of the referenced project is applied to the override that an aspect
    /// of the consuming project introduces.
    /// </summary>
    /// <remarks>
    /// This is the test that reads <c>IntroducedMember.DeclaringType</c> under
    /// <c>SourceMember.GetDerivedDeclarationsCore</c>, which is the frame that issue #2048 reports. The reference
    /// resolves here, and the aspect is applied.
    /// </remarks>
    [Fact]
    public void DerivedDeclarationIntroducedByAnAspectReceivesTheAspect() => this.RunTwoProjects( _appOverrideIntroducedCode );

    /// <summary>
    /// Verifies that the pipeline of the consuming project completes when the implementation of an interface
    /// member that carries an inheritable aspect is introduced by an aspect.
    /// </summary>
    /// <remarks>
    /// The inheritable aspect is not applied to the introduced implementation, which is why this test asserts
    /// only that the pipeline completes. Whether it should be applied is a separate question from issue #2048 and
    /// is deliberately not decided here.
    /// </remarks>
    [Fact]
    public void InterfaceImplementationIntroducedByAnAspectDoesNotBreakThePipeline()
        => this.RunTwoProjects( _appInterfaceIntroducedCode, assertApplied: false );

    /// <summary>
    /// Runs the pipeline of the consuming project twice, editing the file that declares the derived type between
    /// the two runs, so that the second run resolves against a version of the project that is not the one of the
    /// first run.
    /// </summary>
    [Fact]
    public void IntroducedDerivedDeclarationAcrossCompilationVersions()
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        var library = testContext.CreateCSharpCompilation( _libraryCode, assemblyName: "Library" );

        var app = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["app.cs"] = _appOverrideIntroducedCode },
            assemblyName: "App",
            additionalReferences: [library.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out _ ) );
        Assert.True( pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out var appResult1 ) );
        Assert.Contains( "MY001", DumpResults( appResult1 ), StringComparison.Ordinal );

        var app2 = ReplaceFile( app, "app.cs", _appOverrideIntroducedAndEditedCode );

        Assert.True( pipelineFactory.TryExecute( appContext.ProjectOptions, app2, default, out var appResult2 ) );
        Assert.Contains( "MY001", DumpResults( appResult2 ), StringComparison.Ordinal );
    }

    /// <summary>
    /// Runs the pipeline of the referenced project and then the pipeline of the consuming project, and asserts
    /// that the consuming project completes and, when <paramref name="assertApplied"/> is <c>true</c>, that the
    /// inheritable aspect reported its diagnostic there.
    /// </summary>
    private void RunTwoProjects( string appCode, bool assertApplied = true )
    {
        using var testContext = this.CreateTestContext();
        using var libraryContext = this.CreateTestContext();
        using var appContext = this.CreateTestContext();

        var library = testContext.CreateCSharpCompilation( _libraryCode, assemblyName: "Library" );

        var app = testContext.CreateCSharpCompilation(
            appCode,
            assemblyName: "App",
            additionalReferences: [library.ToMetadataReference()] );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( libraryContext.ProjectOptions, library, default, out _ ) );
        Assert.True( pipelineFactory.TryExecute( appContext.ProjectOptions, app, default, out var appResult ) );

        var dump = DumpResults( appResult );
        this.TestOutput.WriteLine( dump );

        if ( assertApplied )
        {
            Assert.Contains( "MY001", dump, StringComparison.Ordinal );
        }
    }

    /// <summary>
    /// Returns <paramref name="compilation"/> with the syntax tree of the given path replaced by the given code.
    /// </summary>
    private static CSharpCompilation ReplaceFile( CSharpCompilation compilation, string path, string newCode )
    {
        var originalTree = compilation.SyntaxTrees.Single( t => t.FilePath == path );
        var parseOptions = (CSharpParseOptions) originalTree.Options;
        var newTree = CSharpSyntaxTree.ParseText( newCode, parseOptions, path: path );

        return compilation.ReplaceSyntaxTree( originalTree, newTree );
    }
}
