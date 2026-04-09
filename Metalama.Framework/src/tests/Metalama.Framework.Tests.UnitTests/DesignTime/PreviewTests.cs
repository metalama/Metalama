// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Metalama.Framework.Tests.UnitTestHelpers.Helpers;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class PreviewTests : PreviewTestsBase
{
    private const string _mainProjectName = "master";

    public PreviewTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public async Task WithAspect()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}
",
            ["target.cs"] = "[MyAspect] class C {}"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithInheritedAspect()
    {
        var masterCode = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[Inheritable]
class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}
",
            ["target.cs"] = "[MyAspect] public class C {}"
        };

        var dependentCode = new Dictionary<string, string>() { ["inherited.cs"] = "class D : C {}" };

        var result = await this.RunPreviewAsync( dependentCode, "inherited.cs", masterCode );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithInheritedAspectChange()
    {
        using var testContext = this.CreateTestContext();
        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var masterCode1 = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[Inheritable]
class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod1() {}
}
",
            ["target.cs"] = "[MyAspect] public class C {}"
        };

        var dependentCode = new Dictionary<string, string>() { ["inherited.cs"] = "class D : C {}" };

        var serviceProvider = testContext.ServiceProvider.Global.WithService( pipelineFactory );

        var result1 = await RunPreviewAsync( testContext, serviceProvider, dependentCode, "inherited.cs", masterCode1 );

        Assert.Contains( "IntroducedMethod1", result1, StringComparison.Ordinal );

        var masterCode2 = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[Inheritable]
class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod2() {}
}
",
            ["target.cs"] = "[MyAspect] public class C {}"
        };

        var result2 = await RunPreviewAsync( testContext, serviceProvider, dependentCode, "inherited.cs", masterCode2 );

        Assert.Contains( "IntroducedMethod2", result2, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithProjectReload()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}
",
            ["target.cs"] = "[MyAspect] class C {}"
        };

        using var testContext = this.CreateTestContext();
        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var result = await RunPreviewAsync(
            testContext,
            testContext.ServiceProvider.Global.WithService( pipelineFactory ),
            code,
            "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );

        var workspaceProvider = testContext.ServiceProvider.Global.GetRequiredService<TestWorkspaceProvider>();
        var workspace = workspaceProvider.Workspace;
        var solution = workspace.CurrentSolution;

        solution = solution.RemoveProject( workspaceProvider.GetProject( _mainProjectName ).Id );

        if ( !workspace.TryApplyChanges( solution ) )
        {
            throw new InvalidOperationException( "Removing project failed." );
        }

        result = await RunPreviewAsync(
            testContext,
            testContext.ServiceProvider.Global.WithService( pipelineFactory ),
            code,
            "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task NamespaceIntroductionWithAspect()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using System;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Code.DeclarationBuilders;

                            public class MyAttribute : Attribute;

                            public class TypeIntroductionsAttribute : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    var ns = builder.With(builder.Target.Compilation).WithNamespace("NS");

                                    var introducedClass = ns.IntroduceClass("Introduced");

                                    introducedClass.IntroduceField("f", typeof(int));

                                    introducedClass.IntroduceAttribute(AttributeConstruction.Create(typeof(MyAttribute)));
                                }
                            }
                            """,
            ["target.cs"] = "[TypeIntroductions] class Target;"
        };

        var result = await this.RunPreviewAsync( code, "NS.Introduced.cs" );

        Assert.Contains( "namespace NS", result, StringComparison.Ordinal );
        Assert.Contains( "[My]", result, StringComparison.Ordinal );
        Assert.Contains( "class Introduced", result, StringComparison.Ordinal );
        Assert.Contains( "int f;", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task AssemblyAttributeIntroduction()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using System;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Code.DeclarationBuilders;

                            [assembly: AttributeIntroductionsAttribute]

                            public class MyAttribute : Attribute;


                            public class AttributeIntroductionsAttribute : CompilationAspect
                            {
                                public override void BuildAspect(IAspectBuilder<ICompilation> builder)
                                {
                                    builder.IntroduceAttribute(AttributeConstruction.Create(typeof(MyAttribute)));
                                }
                            }
                            """
        };

        var result = await this.RunPreviewAsync( code, "MetalamaAssemblyAttributes.cs" );

        Assert.Contains( "[assembly: My]", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task OtherTransformationsAreNotExecuted()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using System;
                            using Metalama.Framework.Aspects;

                            class Aspect : OverrideMethodAspect
                            {
                                public override dynamic? OverrideMethod()
                                {
                                    if (meta.Target.Method.Name != "M1")
                                    {
                                        // Throw exception at compile-time.
                                        meta.CompileTime(((object)null).ToString());
                                    }

                                    Console.WriteLine("from aspect");

                                    return meta.Proceed();
                                }
                            }
                            """,
            ["target1.cs"] = """
                             class Target1
                             {
                                 [Aspect]
                                 void M1() {}
                             }
                             """,
            ["target2.cs"] = """
                             class Target2
                             {
                                 [Aspect]
                                 void M2() {}
                             }
                             """
        };

        var result = await this.RunPreviewAsync( code, "target1.cs" );

        Assert.Contains( """Console.WriteLine("from aspect");""", result, StringComparison.Ordinal );

        var ex = await Assert.ThrowsAsync<EmptyException>( () => this.RunPreviewAsync( code, "target2.cs" ) );
        Assert.Contains( "error LAMA0041", ex.Message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task OtherTypeIntroductionsAreNotExecuted()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;

                            class Aspect : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    var ns = builder.With(builder.Target.Compilation).WithNamespace("NS");

                                    for (int i = 1; i <= 2; i++)
                                    {
                                        var introducedClass = ns.IntroduceClass($"Introduced{i}");

                                        introducedClass.IntroduceMethod(nameof(M));
                                    }
                                }

                                [Template]
                                void M()
                                {
                                    if (meta.Target.Type.Name != "Introduced1")
                                    {
                                        // Throw exception at compile-time.
                                        meta.CompileTime(((object)null).ToString());
                                    }
                                }
                            }
                            """,
            ["target.cs"] = "[Aspect] class Target;"
        };

        var result = await this.RunPreviewAsync( code, "NS.Introduced1.cs" );

        Assert.Contains( "void M()", result, StringComparison.Ordinal );

        var ex = await Assert.ThrowsAsync<EmptyException>( () => this.RunPreviewAsync( code, "NS.Introduced2.cs" ) );
        Assert.Contains( "error LAMA0041", ex.Message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ExistingAssemblyAttribute()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using System;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;

                            class IntroduceTypeAttribute : CompilationAspect
                            {
                                public override void BuildAspect(IAspectBuilder<ICompilation> builder)
                                {
                                    var ns = builder.Advice.WithNamespace(builder.Target.GlobalNamespace, "MyNamespace");
                                    var c = ns.IntroduceClass("MyClass").Declaration;
                                    builder.Advice.IntroduceMethod(c, nameof(SayHello));
                                }

                                [Template]
                                public void SayHello()
                                {
                                    Console.WriteLine("Hello");
                                }
                            }
                            """,
            ["target.cs"] = """
                            using MyNamespace;

                            [assembly: IntroduceType]

                            Console.WriteLine("Hello, World!");

                            new MyClass().SayHello();
                            """
        };

        var result = await this.RunPreviewAsync( code, "MyNamespace.MyClass.cs" );

        Assert.Contains( """Console.WriteLine("Hello");""", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task ExecutionScenario()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Project;

                            public class Aspect : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    var executionScenario = MetalamaExecutionContext.Current.ExecutionScenario;
                                    string scenarioDetails = $"scenario: {executionScenario.Name}, captures non-observable: {executionScenario.CapturesNonObservableTransformations}";

                                    builder.IntroduceField("scenario", typeof(string), buildField: fieldBuilder => fieldBuilder.InitializerExpression = TypedConstant.Create(scenarioDetails));
                                }
                            }
                            """,
            ["target.cs"] = """
                            [Aspect]
                            class Target
                            {
                            }
                            """
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "scenario: Preview, captures non-observable: True", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WhenFrameworkNotEnabled_ReturnsSpecificError()
    {
        var code = new Dictionary<string, string>()
        {
            ["target.cs"] = "class C {}"
        };

        using var testContext = this.CreateTestContext();

        // Create a project options factory that returns options with IsFrameworkEnabled = false,
        // simulating a project that only references Metalama.Framework.Sdk without Metalama.Framework.
        var disabledProjectOptions = new FrameworkDisabledProjectOptions( testContext.ProjectOptions );
        var disabledProjectOptionsFactory = new FrameworkDisabledProjectOptionsFactory( disabledProjectOptions );

        var serviceProviderWithDisabledFramework = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithService( disabledProjectOptionsFactory, allowOverride: true );

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory(
            testContext,
            serviceProviderWithDisabledFramework );

        var serviceProvider = testContext.ServiceProvider.Global.WithService( pipelineFactory );

        var workspace = testContext.ServiceProvider.Global.GetRequiredService<TestWorkspaceProvider>();
        var projectKey = workspace.AddOrUpdateProject( testContext, _mainProjectName, code );

        var service = new TransformationPreviewServiceImpl( serviceProvider );
        var result = await service.PreviewTransformationAsync( projectKey, "target.cs", testContext.CancellationToken );

        Assert.False( result.IsSuccessful );
        Assert.NotNull( result.ErrorMessages );
        Assert.Single( result.ErrorMessages );

        // The error message should NOT be the confusing "not fully loaded yet" message.
        Assert.DoesNotContain( "not been fully loaded yet", result.ErrorMessages[0], StringComparison.OrdinalIgnoreCase );

        // It should indicate that the Metalama framework is not enabled/referenced.
        Assert.Contains( "not enabled", result.ErrorMessages[0], StringComparison.OrdinalIgnoreCase );
    }

    private sealed class FrameworkDisabledProjectOptions : ProjectOptionsWrapper
    {
        public FrameworkDisabledProjectOptions( IProjectOptions wrapped ) : base( wrapped ) { }

        public override bool IsFrameworkEnabled => false;
    }

    private sealed class FrameworkDisabledProjectOptionsFactory : IProjectOptionsFactory
    {
        private readonly IProjectOptions _projectOptions;

        public FrameworkDisabledProjectOptionsFactory( IProjectOptions projectOptions )
        {
            this._projectOptions = projectOptions;
        }

        public IProjectOptions GetProjectOptions( AnalyzerConfigOptions options, TransformerOptions? transformerOptions = null )
            => this._projectOptions;
    }

    [Fact]
    public async Task WithProjectFabric()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Fabrics;

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}


class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender ) => amender.SelectMany( c=>c.Types ).AddAspect<MyAspect>();
} 
",
            ["target.cs"] = "class C {}"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithProjectFabricAndOptions()
    {
        var code = new Dictionary<string, string>()
        {
            ["options.cs"] = OptionsTestHelper.OptionsCode,
            ["aspect.cs"] =
                """

                using Metalama.Framework.Advising;
                using Metalama.Framework.Aspects;
                using Metalama.Framework.Fabrics;
                using Metalama.Framework.Code;
                using Metalama.Framework.Options;

                class MyAspect : TypeAspect
                {
                    [Introduce]
                    public string Field = meta.Target.Type.Enhancements().GetOptions<MyOptions>().Value;
                }


                class Fabric : ProjectFabric
                {
                    public override void AmendProject( IProjectAmender amender )
                    {
                        amender.SetOptions( o => new MyOptions { Value = "TheValue" } );
                        amender.SelectMany( c=>c.Types ).AddAspect<MyAspect>();
                    }
                }

                """,
            ["target.cs"] = "class C {}"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "Field = \"TheValue\"", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithTypeFabric()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}



",
            ["target.cs"] = @"

using Metalama.Framework.Fabrics;
using Metalama.Framework.Aspects;

class C {


class Fabric : TypeFabric
{
    public override void AmendType( ITypeAmender amender ) => amender.AddAspect<MyAspect>();
} 

}"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task WithNamespaceFabric()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

namespace Ns;

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}



",
            ["fabric.cs"] = @"
using Metalama.Framework.Fabrics;
using Metalama.Framework.Aspects;

namespace Ns;

class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender ) => amender.SelectMany( c=>c.Types ).AddAspect<MyAspect>();
} 

",
            ["target.cs"] = @"
namespace Ns;

class C {}"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        Assert.Contains( "IntroducedMethod", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task NamespaceIntroductionWithFabric()
    {
        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = """
                            using System;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Code.DeclarationBuilders;

                            public class MyAttribute : Attribute;

                            public class TypeIntroductionsAttribute : CompilationAspect
                            {
                                public override void BuildAspect(IAspectBuilder<ICompilation> builder)
                                {
                                    var ns = builder.With(builder.Target.GlobalNamespace).WithChildNamespace("NS");

                                    var introducedClass = ns.IntroduceClass("Introduced");

                                    introducedClass.IntroduceField("f", typeof(int));

                                    introducedClass.IntroduceAttribute(AttributeConstruction.Create(typeof(MyAttribute)));
                                }
                            }
                            """,
            ["fabric.cs"] = """
                            using Metalama.Framework.Fabrics;
                            using Metalama.Framework.Aspects;

                            class Fabric : ProjectFabric
                            {
                                public override void AmendProject(IProjectAmender amender)
                                {
                                    amender.AddAspect<TypeIntroductionsAttribute>();
                                }
                            }
                            """
        };

        var result = await this.RunPreviewAsync( code, "NS.Introduced.cs" );

        Assert.Contains( "namespace NS", result, StringComparison.Ordinal );
        Assert.Contains( "[My]", result, StringComparison.Ordinal );
        Assert.Contains( "class Introduced", result, StringComparison.Ordinal );
        Assert.Contains( "int f;", result, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DesignTimeDisabled_ReturnsSpecificErrorMessage()
    {
        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

class MyAspect : TypeAspect
{
   [Introduce]
   void IntroducedMethod() {}
}
",
            ["target.cs"] = "[MyAspect] class C {}"
        };

        using var testContext = this.CreateTestContext();

        // Wrap the project options to disable design-time.
        var designTimeDisabledOptions = new DesignTimeDisabledProjectOptions( testContext.ProjectOptions );

        // Replace the project options factory so it returns options with IsDesignTimeEnabled=false.
        var serviceProvider = (GlobalServiceProvider) testContext.ServiceProvider.Global.Underlying
            .WithService( new TestProjectOptionsFactory( designTimeDisabledOptions ), allowOverride: true );

        var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext, serviceProvider );
        serviceProvider = serviceProvider.WithService( pipelineFactory );

        var workspace = testContext.ServiceProvider.Global.GetRequiredService<TestWorkspaceProvider>();
        var projectKey = workspace.AddOrUpdateProject( testContext, _mainProjectName, code );

        var service = new TransformationPreviewServiceImpl( serviceProvider );
        var result = await service.PreviewTransformationAsync( projectKey, "target.cs", default( CancellationToken ) );

        Assert.False( result.IsSuccessful );
        Assert.NotNull( result.ErrorMessages );
        Assert.NotEmpty( result.ErrorMessages );
        Assert.Contains( "MetalamaDesignTimeEnabled", result.ErrorMessages[0], StringComparison.Ordinal );
    }

    private sealed class DesignTimeDisabledProjectOptions : ProjectOptionsWrapper
    {
        public override bool IsDesignTimeEnabled => false;

        public DesignTimeDisabledProjectOptions( IProjectOptions underlying ) : base( underlying ) { }
    }

    // Exercises the Diff/Preview scenario where an IInitializable implementer exists in the current project.
    // This verifies that the linker's OnInitializedCallSiteFinder runs in preview and wraps `new TargetCode()`
    // call sites in InitializableExtensions.WithInitialize(...). The target type explicitly implements
    // IInitializable at source level because, in design-time preview, the partial compilation's observability
    // filter prevents aspect-introduced interfaces on non-observed trees from appearing in the intermediate
    // compilation — so we rely on a source-level declaration here.
    [Fact]
    public async Task WithInitializerAspect_SingleProject()
    {
        var code = new Dictionary<string, string>()
        {
            ["target_type.cs"] = @"
using Metalama.Framework.RunTime.Initialization;

public class TargetCode : IInitializable
{
    public int Value { get; set; }
    public virtual void Initialize( InitializationContext context = default ) { }
}
",
            ["target.cs"] = @"
public class Caller
{
    public void Method()
    {
        var t = new TargetCode();
    }
}
"
        };

        var result = await this.RunPreviewAsync( code, "target.cs" );

        // When the initializable-type flag is correctly propagated, the linker wraps the `new TargetCode()`
        // call site in WithInitialize(...). If the flag were incorrectly `false`, the walker would be
        // skipped and the `new TargetCode()` expression would remain unwrapped.
        Assert.Contains( "WithInitialize", result, StringComparison.Ordinal );
    }

    // Exercises the Diff/Preview scenario where the IInitializable implementer lives in a referenced project.
    // This verifies that the cross-project ContainsInitializableTypes flag is correctly aggregated from
    // the referenced DesignTimeAspectPipelineResult (read via ITransitiveAspectManifestProvider), so that
    // the linker's walker still runs on the dependent project's syntax trees even though no initializable
    // type is declared in the current project.
    [Fact]
    public async Task WithInitializerAspect_CrossProject()
    {
        var masterCode = new Dictionary<string, string>()
        {
            ["base.cs"] = @"
using Metalama.Framework.RunTime.Initialization;

public class BaseClass : IInitializable
{
    public virtual void Initialize( InitializationContext context = default ) { }
}
"
        };

        var dependentCode = new Dictionary<string, string>()
        {
            ["caller.cs"] = @"
public class Caller
{
    public void Method()
    {
        var b = new BaseClass();
    }
}
"
        };

        var result = await this.RunPreviewAsync( dependentCode, "caller.cs", masterCode );

        // With the cross-project flag aggregated from the referenced DesignTimeAspectPipelineResult,
        // the walker runs and wraps `new BaseClass()` in WithInitialize(...).
        Assert.Contains( "WithInitialize", result, StringComparison.Ordinal );
    }
}