// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTestHelpers.TestClasses;

#pragma warning disable VSTHRD200

public class DiagnosticAnalyzerTestsBase( ITestOutputHelper logger ) : UnitTestClass( logger )
{
    protected async Task<List<Diagnostic>> RunAnalyzer( string code, string? dependencyCode = null )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );
        using var testContext = this.CreateTestContext( additionalServices );

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );

        string[]? references = null;

        if ( dependencyCode != null )
        {
            workspaceProvider.AddOrUpdateProject(
                testContext,
                "dependency_project",
                new Dictionary<string, string>() { ["dependency_code.cs"] = dependencyCode } );

            references = ["dependency_project"];
            var dependencyCompilation = await workspaceProvider.GetProject( "dependency_project" ).GetCompilationAsync();
            var dependencyDiagnostics = dependencyCompilation!.GetDiagnostics();
            Assert.Empty( dependencyDiagnostics.Where( x => x.Severity == DiagnosticSeverity.Error ) );
        }

        workspaceProvider.AddOrUpdateProject( testContext, "project", new Dictionary<string, string>() { ["code.cs"] = code }, projectReferences: references );
        var compilation = await workspaceProvider.GetProject( "project" ).GetCompilationAsync();
        var syntaxTree = await workspaceProvider.GetDocument( "project", "code.cs" ).GetSyntaxTreeAsync();
        var semanticModel = compilation!.GetSemanticModel( syntaxTree! );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );
        var analysisContext = new TestSemanticModelAnalysisContext( semanticModel, testContext.ProjectOptions );

        analyzer.AnalyzeSemanticModel( analysisContext );

        return analysisContext.ReportedDiagnostics;
    }
}