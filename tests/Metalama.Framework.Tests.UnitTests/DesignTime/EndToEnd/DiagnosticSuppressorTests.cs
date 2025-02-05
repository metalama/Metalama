// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    private async Task<List<Suppression>> ExecuteSuppressorAsync( string code, string diagnosticId )
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

        return analysisContext.ReportedSuppressions;
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

        var suppression = Assert.Single( suppressions );

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

        var suppression = Assert.Single( suppressions );

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

        var suppression = Assert.Single( suppressions );

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

        var suppression = Assert.Single( suppressions );

        Assert.Equal( "code.cs(19,9): warning CS0169: The field 'TargetClass._field' is never used", suppression.SuppressedDiagnostic.ToString() );
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

                            }
                            """;

        var suppressions = await this.ExecuteSuppressorAsync( code, "CS0628" );

        var suppression = Assert.Single( suppressions );

        Assert.Equal(
            "code.cs(9,20): warning CS0628: 'SomeAspect.SuspendInvariants()': new protected member declared in sealed type",
            suppression.SuppressedDiagnostic.ToString() );
    }
}