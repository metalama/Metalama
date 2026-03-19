// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

#pragma warning disable VSTHRD200

public sealed class DiagnosticAnalyzerTests( ITestOutputHelper logger ) : DiagnosticAnalyzerTestsBase( logger )
{
    [Fact]
    public async Task Nothing()
    {
        var diagnostics = await this.RunAnalyzer( "" );
        Assert.Empty( diagnostics );
    }

    [Fact]
    public async Task CSharpErrorIsNotReported()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 

                            class TheAspect : TypeAspect
                            {
                               SomeError;
                            }

                            """;

        var diagnostics = await this.RunAnalyzer( code );

        // CSharp errors should not be reported at design time.
        Assert.Empty( diagnostics );
    }

    [Fact]
    public async Task CompileTimeMetalamaDiagnosticsAreReported()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 

                            class TheAspect : OverrideMethodAspect
                            {
                               public override dynamic? OverrideMethod()
                               {
                                  // The following line is an error.
                                  meta.InsertComment( meta.Proceed() );
                               }
                            }


                            """;

        var diagnostics = await this.RunAnalyzer( code );

        // CSharp errors should not be reported at design time.
        Assert.Equal( TemplatingDiagnosticDescriptors.ScopeMismatch.Id, Assert.Single( diagnostics ).Id );
    }

    [Fact]
    public async Task TemplateAnnotatorDiagnosticsAreReported()
    {
        const string code = """
                            using System;
                            using Metalama.Framework;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 

                            class TheAspect : OverrideMethodAspect
                            {
                                public override dynamic? OverrideMethod()
                                {
                                    int i = meta.CompileTime(0);

                                    if (meta.Target.Parameters[0].Value)
                                    {
                                        // The following line is an error.
                                        i = 1;
                                    }

                                    return null;
                                }
                            }
                            """;

        var diagnostics = await this.RunAnalyzer( code );

        Assert.Equal( TemplatingDiagnosticDescriptors.CannotSetCompileTimeVariableInRunTimeConditionalBlock.Id, Assert.Single( diagnostics ).Id );
    }

    [Fact]
    public async Task UserError()
    {
        static string GetCode( string extraCode )
            => $$"""
                 using Metalama.Framework.Advising;
                 using Metalama.Framework.Aspects; 
                 using Metalama.Framework.Code;
                 using Metalama.Framework.Diagnostics;

                 class ErrorAspect : TypeAspect
                 {
                     static readonly DiagnosticDefinition _error = new( "MLTEST", Severity.Error, "Error!" );

                     public override void BuildAspect( IAspectBuilder<INamedType> builder )
                     {
                         builder.Diagnostics.Report( _error );
                         {{extraCode}}
                     }
                 }

                 [ErrorAspect]
                 class C {}
                 """;

        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService( true ) );
        using var testContext = this.CreateTestContext( additionalServices );

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = GetCode( "" ) } );
        var compilation1 = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var syntaxTree1 = await workspaceProvider.GetDocument( "project", "code.cs" ).GetSyntaxTreeAsync();
        var semanticModel1 = compilation1!.GetSemanticModel( syntaxTree1! );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );

        var analysisContext1 = new TestSemanticModelAnalysisContext( semanticModel1, testContext.ProjectOptions );
        analyzer.AnalyzeSemanticModel( analysisContext1 );
        var diagnostic1 = Assert.Single( analysisContext1.ReportedDiagnostics );

        Assert.Equal(
            string.Format( CultureInfo.InvariantCulture, DesignTimeDiagnosticDescriptors.UserError.MessageFormat, "MLTEST", "Error!" ),
            diagnostic1.GetLocalizedMessage() );

        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = GetCode( "// whatever" ) } );
        var compilation2 = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var syntaxTree2 = await workspaceProvider.GetDocument( "project", "code.cs" ).GetSyntaxTreeAsync();
        var semanticModel2 = compilation2!.GetSemanticModel( syntaxTree2! );

        var analysisContext2 = new TestSemanticModelAnalysisContext( semanticModel2, testContext.ProjectOptions );
        analyzer.AnalyzeSemanticModel( analysisContext2 );
        var diagnostic2 = Assert.Single( analysisContext2.ReportedDiagnostics );

        Assert.Equal(
            string.Format( CultureInfo.InvariantCulture, DesignTimeDiagnosticDescriptors.UserError.MessageFormat, "MLTEST", "Error!" ),
            diagnostic2.GetLocalizedMessage() );

        Assert.Equal( diagnostic1.Id, diagnostic2.Id );
        Assert.Equal( diagnostic1.Descriptor.Id, diagnostic2.Descriptor.Id );
        Assert.Equal( diagnostic1.Descriptor.Title, diagnostic2.Descriptor.Title );
        Assert.Equal( diagnostic1.Descriptor.HelpLinkUri, diagnostic2.Descriptor.HelpLinkUri );

        Assert.Equal(
            diagnostic1.Descriptor.MessageFormat.ToString( CultureInfo.InvariantCulture ),
            diagnostic2.Descriptor.MessageFormat.ToString( CultureInfo.InvariantCulture ) );

        Assert.Equal( diagnostic1.Descriptor.Category, diagnostic2.Descriptor.Category );
        Assert.Equal( diagnostic1.Descriptor.DefaultSeverity, diagnostic2.Descriptor.DefaultSeverity );
        Assert.Equal( diagnostic1.Descriptor.IsEnabledByDefault, diagnostic2.Descriptor.IsEnabledByDefault );
        Assert.Equal( diagnostic1.Descriptor.CustomTags, diagnostic2.Descriptor.CustomTags );
        Assert.Equal( diagnostic1.Severity, diagnostic2.Severity );
        Assert.Equal( diagnostic1.DefaultSeverity, diagnostic2.DefaultSeverity );
        Assert.Equal( diagnostic1.WarningLevel, diagnostic2.WarningLevel );
        Assert.Equal( diagnostic1.Properties, diagnostic2.Properties );
    }

    [Fact]
    public async Task SuppressionFromDependencyReportsLAMA0306WhenNotInUserProfile()
    {
        // Regression test for #726: When a dependency defines a SuppressionDefinition (e.g., for IDE0051 "Private member is unused")
        // and the suppression is NOT in the user profile (because the IDE was started before the suppression was registered),
        // LAMA0306 should be reported to tell the user to restart their IDE.
        const string dependencyCode = """
                                      using Metalama.Framework.Advising;
                                      using Metalama.Framework.Aspects;
                                      using Metalama.Framework.Code;
                                      using Metalama.Framework.Diagnostics;

                                      namespace TestDependency;

                                      [CompileTime]
                                      internal static class Suppressions
                                      {
                                          // IDE0051: "Private member is unused"
                                          public static readonly SuppressionDefinition SuppressIDE0051 = new("IDE0051");
                                      }

                                      public class SuppressWarningAttribute : MethodAspect
                                      {
                                          public override void BuildAspect(IAspectBuilder<IMethod> builder)
                                          {
                                              builder.Diagnostics.Suppress(Suppressions.SuppressIDE0051, builder.Target);
                                          }
                                      }
                                      """;

        const string code = """
                            using TestDependency;

                            class TargetClass
                            {
                                [SuppressWarning]
                                private void UnusedMethod() { }
                            }
                            """;

        var diagnostics = await this.RunAnalyzer( code, dependencyCode );

        // LAMA0306 should be reported because the suppression is not in the user profile (TestUserDiagnosticRegistrationService
        // returns empty SupportedSuppressionDescriptors, simulating a stale user profile). The user should restart their IDE.
        Assert.Contains( diagnostics, d => d.Id == "LAMA0306" );
    }

    [Fact]
    public async Task UserWarningIsWrappedWhenNotRegistered()
    {
        // Regression test for #686: When a user diagnostic has not been registered in the user profile,
        // the diagnostic is wrapped into a LAMA030x ID. Verify the wrapping preserves the message and
        // diagnostic properties.
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Diagnostics;

                            class WarningAspect : TypeAspect
                            {
                                static readonly DiagnosticDefinition _warning = new( "MLTEST", Severity.Warning, "Warning!" );

                                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                {
                                    builder.Diagnostics.Report( _warning );
                                }
                            }

                            [WarningAspect]
                            class C {}
                            """;

        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService( true ) );
        using var testContext = this.CreateTestContext( additionalServices );

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );
        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code } );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var syntaxTree = await workspaceProvider.GetDocument( "project", "code.cs" ).GetSyntaxTreeAsync();
        var semanticModel = compilation!.GetSemanticModel( syntaxTree! );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );

        var analysisContext = new TestSemanticModelAnalysisContext( semanticModel, testContext.ProjectOptions );
        analyzer.AnalyzeSemanticModel( analysisContext );
        var diagnostic = Assert.Single( analysisContext.ReportedDiagnostics );

        // The diagnostic should have been wrapped to LAMA0302 (UserWarning) since MLTEST is not registered.
        Assert.Equal( DesignTimeDiagnosticDescriptors.UserWarning.Id, diagnostic.Id );

        // Verify the original diagnostic message is preserved in the wrapped message.
        Assert.Contains( "MLTEST", diagnostic.GetLocalizedMessage(), StringComparison.Ordinal );
        Assert.Contains( "Warning!", diagnostic.GetLocalizedMessage(), StringComparison.Ordinal );
    }
}