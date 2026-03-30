// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.DiagnosticSuppressing;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

#pragma warning disable VSTHRD200

public sealed class DiagnosticSuppressorTests : UnitTestClass
{
    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );
        services.AddGlobalService( provider => new DesignTimeExtensionManager( provider ) );
    }

    private async Task<(ImmutableArray<Diagnostic> Diagnostics, List<Suppression> Suppressions, IReadOnlyList<Diagnostic> RemainingDiagnostics)>
        ExecuteSuppressorAsync( string code, string diagnosticId )
    {
        using var testContext = this.CreateTestContext();

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code } );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var diagnostics = compilation!.GetDiagnostics();

        var suppressor = new TheDiagnosticSuppressor( pipelineFactory.ServiceProvider );
        var analysisContext = new TestSuppressionAnalysisContext( compilation, diagnostics, testContext.ProjectOptions );

        suppressor.ReportSuppressions(
            analysisContext,
            ImmutableDictionary<string, SuppressionDescriptor>.Empty.Add( diagnosticId, new SuppressionDescriptor( diagnosticId, diagnosticId, "Because" ) ) );

        var suppressedDiagnostics = analysisContext.ReportedSuppressions.SelectAsReadOnlyCollection( x => x.SuppressedDiagnostic ).ToHashSet();
        var remainingDiagnostics = diagnostics.Where( d => d.Severity != DiagnosticSeverity.Hidden && !suppressedDiagnostics.Contains( d ) ).ToReadOnlyList();

        return (diagnostics, analysisContext.ReportedSuppressions, remainingDiagnostics);
    }

    [Fact]
    public async Task SuppressVariableLevelWarning()
    {
        const string code = """
                            using Metalama.Framework.Advising; 
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            namespace Metalama.Framework.Tests.AspectTests.Aspects.Suppressions.Methods
                            {
                                public class SuppressWarningAttribute : MethodAspect
                                {
                                    private static readonly SuppressionDefinition _suppression1 = new( "CS0219" );

                                    public override void BuildAspect( IAspectBuilder<IMethod> builder )
                                    {
                                        builder.Diagnostics.Suppress( _suppression1, builder.Target );
                                    }
                                }

                                // <target>
                                internal class TargetClass
                                {
                                    [SuppressWarning]
                                    private void M2( string m )
                                    {
                                        var x = 0;
                                    }

                                    // CS0219 expected
                                    private void M1( string m )
                                    {
                                        var x = 0;
                                    }
                                }
                            }
                            """;

        var suppressions = await this.ExecuteSuppressorAsync( code, "CS0219" );

        var suppression = Assert.Single( suppressions.Suppressions );

        Assert.Equal( "code.cs(25,17): warning CS0219: The variable 'x' is assigned but its value is never used", suppression.SuppressedDiagnostic.ToString() );
    }

    [Fact]
    public async Task SuppressFieldLevelWarning()
    {
        const string code = """
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            namespace Metalama.Framework.Tests.AspectTests.Aspects.Suppressions.Methods
                            {
                                public class SuppressWarningAttribute : FieldAspect
                                {
                                    private static readonly SuppressionDefinition _suppression1 = new( "CS0169" );

                                    public override void BuildAspect( IAspectBuilder<IField> builder )
                                    {
                                        builder.Diagnostics.Suppress( _suppression1, builder.Target );
                                    }
                                }

                                // <target>
                                internal class TargetClass
                                {
                                    [SuppressWarning]
                                    int _field;
                                }
                            }
                            """;

        var suppressions = await this.ExecuteSuppressorAsync( code, "CS0169" );

        var suppression = Assert.Single( suppressions.Suppressions );

        Assert.Equal( "code.cs(21,13): warning CS0169: The field 'TargetClass._field' is never used", suppression.SuppressedDiagnostic.ToString() );
    }

    [Fact]
    public async Task ParametricSuppression()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;
                            using System.Linq;

                            class SuppressWarningAttribute : ConstructorAspect
                            {
                                private static readonly SuppressionDefinition _suppression = new("CS8618");

                                public override void BuildAspect(IAspectBuilder<IConstructor> builder)
                                {
                                    builder.Diagnostics.Suppress(
                                        _suppression.WithFilter(static diag => diag.Arguments.Any(arg => arg is string s && s == "o1")), builder.Target);
                                }
                            }

                            class TargetClass
                            {
                                object o1;
                                object o2;

                                [SuppressWarning]
                                public TargetClass() { }
                            }

                            class AnotherClass
                            {
                                object o1;
                                object o2;

                                public AnotherClass() { }
                            }
                            """;

        var suppressions = await this.ExecuteSuppressorAsync( code, "CS8618" );

        var suppression = Assert.Single( suppressions.Suppressions );

#if ROSLYN_4_12_0_OR_GREATER // The diagnostic message has changed between Roslyn 4.8 and 4.12
        Assert.Equal(
            "code.cs(24,12): warning CS8618: Non-nullable field 'o1' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.",
            suppression.SuppressedDiagnostic.ToString() );
#endif
    }

    [Fact]
    public async Task SuppressOuterScope()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            public class SuppressWarningAttribute : TypeAspect
                            {
                                private static readonly SuppressionDefinition _suppression1 = new( "CS0169" );

                                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                {
                                    builder.Diagnostics.Suppress( _suppression1, builder.Target );
                                }
                            }

                            [SuppressWarning]
                            internal class TargetClass
                            {
                                int _field;
                            }
                            """;

        var suppressions = await this.ExecuteSuppressorAsync( code, "CS0169" );

        var suppression = Assert.Single( suppressions.Suppressions );

        Assert.Equal( "code.cs(19,9): warning CS0169: The field 'TargetClass._field' is never used", suppression.SuppressedDiagnostic.ToString() );
    }

    [Fact]
    public async Task StaleUserProfileDoesNotSuppressDiagnosticAndReportsLAMA0306()
    {
        // Regression test for #726: When the user profile (SupportedSuppressionDescriptors) is stale/empty
        // but the suppression is defined in the compile-time project's DiagnosticManifest:
        // (a) The suppressor does NOT suppress the original diagnostic (CS0169) because Roslyn's
        //     DiagnosticSuppressor contract requires suppressions to be declared in SupportedSuppressions.
        //     When Roslyn hosts the suppressor, it filters diagnostics before calling ReportSuppressions,
        //     so diagnostics not in SupportedSuppressions are never passed to the method.
        // (b) The analyzer reports LAMA0306 to tell the user to restart their IDE.
        const string code = """
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            namespace Metalama.Framework.Tests.AspectTests.Aspects.Suppressions.Methods
                            {
                                public class SuppressWarningAttribute : FieldAspect
                                {
                                    // CS0169: "The field is never used"
                                    private static readonly SuppressionDefinition _suppression1 = new( "CS0169" );

                                    public override void BuildAspect( IAspectBuilder<IField> builder )
                                    {
                                        builder.Diagnostics.Suppress( _suppression1, builder.Target );
                                    }
                                }

                                // <target>
                                internal class TargetClass
                                {
                                    [SuppressWarning]
                                    int _field;
                                }
                            }
                            """;

        // The TestUserDiagnosticRegistrationService returns empty SupportedSuppressionDescriptors,
        // simulating a stale user profile where CS0169 suppression has not been registered yet.
        using var testContext = this.CreateTestContext();

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code } );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var diagnostics = compilation!.GetDiagnostics();

        // (a) Verify the suppressor does NOT suppress CS0169 when the user profile is empty.
        // In production, Roslyn would not even pass CS0169 to ReportSuppressions because it's
        // not declared in SupportedSuppressions. Here we simulate that by passing an empty
        // supportedSuppressionDescriptors dictionary — the suppressor correctly skips the
        // suppression because it's not in the supported set.
        var suppressor = new TheDiagnosticSuppressor( pipelineFactory.ServiceProvider );
        var suppressionContext = new TestSuppressionAnalysisContext( compilation, diagnostics, testContext.ProjectOptions );

        suppressor.ReportSuppressions( suppressionContext, ImmutableDictionary<string, SuppressionDescriptor>.Empty );

        Assert.Empty( suppressionContext.ReportedSuppressions );

        // (b) Verify the analyzer reports LAMA0306 because the suppression is not in the user profile.
        var syntaxTree = await workspaceProvider.GetDocument( "project", "code.cs" ).GetSyntaxTreeAsync();
        var semanticModel = compilation.GetSemanticModel( syntaxTree! );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );
        var analyzerContext = new TestSemanticModelAnalysisContext( semanticModel, testContext.ProjectOptions );

        analyzer.AnalyzeSemanticModel( analyzerContext );

        Assert.Contains( analyzerContext.ReportedDiagnostics, d => d.Id == "LAMA0306" );
    }

    [Fact]
    public async Task SuppressTemplateWarnings()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            internal sealed class SomeAspect : TypeAspect
                            {
                                [Template]
                                protected void SuspendInvariants()
                                {
                                }

                                [Template]
                                private static dynamic CommandProperty { get; }

                                [Introduce]
                                private static dynamic Field;

                                [Introduce]
                                private readonly object _logger;

                                [Introduce]
                                private void M()
                                {
                                    _ = this._logger.ToString();
                                }

                            }
                            """;

        var result = await this.ExecuteSuppressorAsync( code, "CS0628" );

        Assert.Empty( result.RemainingDiagnostics );
    }

    [Fact]
    public async Task SuppressCA1822OnTemplateMethod()
    {
        // CA1822 is an analyzer diagnostic ("Member does not access instance data and can be marked as static"),
        // which is a false positive on template methods because the template is transformed at compile time.
        const string code = """
                            using Metalama.Framework.Aspects;

                            internal class SomeAspect : TypeAspect
                            {
                                [Template]
                                public void TemplateMethod()
                                {
                                    System.Console.WriteLine("Hello");
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code } );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();

        // Create a synthetic CA1822 diagnostic at the template method location.
        var syntaxTree = compilation!.SyntaxTrees.Single();
        var root = await syntaxTree.GetRootAsync();
        var methodDeclaration = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Single( m => m.Identifier.Text == "TemplateMethod" );

        var ca1822Descriptor = new DiagnosticDescriptor(
            "CA1822",
            "Mark members as static",
            "Member '{0}' does not access instance data and can be marked as static",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true );

        var ca1822Diagnostic = Diagnostic.Create(
            ca1822Descriptor,
            Location.Create( syntaxTree, methodDeclaration.Identifier.Span ),
            "TemplateMethod" );

        var allDiagnostics = compilation.GetDiagnostics().Add( ca1822Diagnostic );

        var suppressor = new TheDiagnosticSuppressor( pipelineFactory.ServiceProvider );
        var analysisContext = new TestSuppressionAnalysisContext( compilation, allDiagnostics, testContext.ProjectOptions );

        suppressor.ReportSuppressions(
            analysisContext,
            ImmutableDictionary<string, SuppressionDescriptor>.Empty.Add(
                "CA1822",
                new SuppressionDescriptor( "CA1822", "CA1822", "Because" ) ) );

        var suppression = Assert.Single( analysisContext.ReportedSuppressions );
        Assert.Equal( "CA1822", suppression.SuppressedDiagnostic.Id );
    }

    [Fact]
    public async Task DoNotSuppressCA1822OnNonTemplateMethod()
    {
        // CA1822 should NOT be suppressed on non-template methods.
        const string code = """
                            using Metalama.Framework.Aspects;

                            internal class SomeAspect : TypeAspect
                            {
                                public void RegularMethod()
                                {
                                    System.Console.WriteLine("Hello");
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code } );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();

        // Create a synthetic CA1822 diagnostic at the non-template method location.
        var syntaxTree = compilation!.SyntaxTrees.Single();
        var root = await syntaxTree.GetRootAsync();
        var methodDeclaration = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Single( m => m.Identifier.Text == "RegularMethod" );

        var ca1822Descriptor = new DiagnosticDescriptor(
            "CA1822",
            "Mark members as static",
            "Member '{0}' does not access instance data and can be marked as static",
            "Performance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true );

        var ca1822Diagnostic = Diagnostic.Create(
            ca1822Descriptor,
            Location.Create( syntaxTree, methodDeclaration.Identifier.Span ),
            "RegularMethod" );

        var allDiagnostics = compilation.GetDiagnostics().Add( ca1822Diagnostic );

        var suppressor = new TheDiagnosticSuppressor( pipelineFactory.ServiceProvider );
        var analysisContext = new TestSuppressionAnalysisContext( compilation, allDiagnostics, testContext.ProjectOptions );

        suppressor.ReportSuppressions(
            analysisContext,
            ImmutableDictionary<string, SuppressionDescriptor>.Empty.Add(
                "CA1822",
                new SuppressionDescriptor( "CA1822", "CA1822", "Because" ) ) );

        Assert.Empty( analysisContext.ReportedSuppressions );
    }
}