// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class SourceTransformerTests : UnitTestClass
{
    private TestTransformerContext RunTransformer( string code, string? dependencyCode = null, bool warnAsErrors = false )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );
        using var testContext = this.CreateTestContext( additionalServices );

        var compilation = testContext.CreateCSharpCompilation( code, dependencyCode, warnAsErrors: warnAsErrors );
        var context = new TestTransformerContext( compilation, testContext.ProjectOptions, testContext.ServiceProvider.Global );
        SourceTransformer.Execute( context );

        return context;
    }

    [Theory]
    [InlineData( false )]
    [InlineData( true )]
    public void TestSuppressions( bool warnAsErrors )
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

                            }
                            """;

        var context = this.RunTransformer( code, warnAsErrors: warnAsErrors );
        var diagnostics = context.Compilation.GetDiagnostics();

        var suppressionRunner = new DiagnosticFilterRunner(
            context.Compilation,
            tree => context.Compilation.GetSemanticModel( tree ),
            context.ReportedSuppressions );

        var survivingDiagnostics =
            diagnostics.Where(
                    d =>
                        d.Severity >= DiagnosticSeverity.Warning &&
                        !context.ReportedSuppressions.Any(
                            s => d.Id == s.Descriptor.SuppressedDiagnosticId &&
                                 suppressionRunner.TryGetSuppression( d, default, out _ ) ) )
                .ToList();

        Assert.Empty( survivingDiagnostics );
    }
}