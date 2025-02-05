// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Tests.UnitTestHelpers.Helpers;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System;
using System.Collections.Generic;
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
}