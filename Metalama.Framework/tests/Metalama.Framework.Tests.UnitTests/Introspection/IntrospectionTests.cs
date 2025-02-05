// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Introspection;

#pragma warning disable VSTHRD200 // Use "Async" suffix.

public sealed class IntrospectionTests : UnitTestClass
{
    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );

        services.AddProjectService( _ => new ConcurrentTaskRunner() );
    }

    [Fact]
    public async Task Success()
    {
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

class Aspect : TypeAspect
{

  private static readonly DiagnosticDefinition _warning = new(
            ""MY001"",
        Severity.Warning,
        ""Message"" );


    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
builder.Diagnostics.Report(         _warning );
    }

    [Introduce]
    public void NewMethod(){}

}

[Aspect]
class MyClass
{
    
    void MyMethod() {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );

        var compiler = new IntrospectionCompiler( testContext.ServiceProvider );
        var compilerOutput = await compiler.CompileAsync( compilation );

        Assert.True( compilerOutput.HasMetalamaSucceeded );
        Assert.Single( compilerOutput.Diagnostics );
        var aspectInstances = compilerOutput.AspectInstances;
        Assert.Single( aspectInstances );
        Assert.Single( aspectInstances[0].Diagnostics );
        Assert.Single( aspectInstances[0].Advice );
        var aspectClass = compilerOutput.AspectClasses.Single( x => x.ShortName == "Aspect" );
        Assert.Same( aspectInstances[0], aspectClass.Instances[0] );
        Assert.Same( aspectClass, aspectInstances[0].AspectClass );
    }

    [Fact]
    public async Task UserError()
    {
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

class Aspect : TypeAspect
{

  private static readonly DiagnosticDefinition _error = new(
            ""MY001"",
        Severity.Error,
        ""Message"" );


    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.Diagnostics.Report( _error );
    }

    [Introduce]
    public void NewMethod(){}

}

[Aspect]
class MyClass
{
    
    void MyMethod() {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );

        var compiler = new IntrospectionCompiler( testContext.ServiceProvider );
        var compilerOutput = await compiler.CompileAsync( compilation );

        Assert.True( compilerOutput.HasMetalamaSucceeded );
        Assert.Single( compilerOutput.Diagnostics );
        Assert.Single( compilerOutput.AspectInstances );
        Assert.Single( compilerOutput.AspectInstances[0].Diagnostics );
        Assert.Equal( "MY001", compilerOutput.AspectInstances[0].Diagnostics[0].Id );
    }

    [Fact]
    public async Task SyntaxErrorInCompileTimeCode()
    {
        const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

class Aspect : TypeAspect
{
    public override BuildAspect( IAspectBuilder<INamedType> builder )
   {
        some_error;
   }
}

 

    [Introduce]
    public void NewMethod(){}

}

[Aspect]
class MyClass
{
    
    void MyMethod() {}
}
";

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );

        var compiler = new IntrospectionCompiler( testContext.ServiceProvider );
        var compilerOutput = await compiler.CompileAsync( compilation );

        Assert.False( compilerOutput.HasMetalamaSucceeded );
        Assert.NotEmpty( compilerOutput.Diagnostics );
    }

    [Fact]
    public async Task Options()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Code.DeclarationBuilders;
                            using Metalama.Framework.Options;

                            public class Options : IHierarchicalOptions<IDeclaration>
                            {
                                public string? Path { get; set; }
                            
                                public IHierarchicalOptions GetDefaultOptions(OptionsInitializationContext context) => new Options { Path = "Start" };
                            
                                public object ApplyChanges(object changes, in ApplyChangesContext context)
                                {
                                    var other = (Options)changes;
                            
                                    return new Options { Path = $"{this.Path}->{other.Path}" };
                                }
                            }

                            class Aspect : TypeAspect
                            {
                                public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                {
                                    base.BuildAspect(builder);
                            
                                    var options = builder.Target.Enhancements().GetOptions<Options>();
                                }
                            }

                            [Aspect]
                            class Target;
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );

        var compiler = new IntrospectionCompiler( testContext.ServiceProvider );
        var compilerOutput = await compiler.CompileAsync( compilation );

        Assert.True( compilerOutput.HasMetalamaSucceeded );
        Assert.Empty( compilerOutput.Diagnostics );
        var aspectInstances = compilerOutput.AspectInstances;
        Assert.Single( aspectInstances );
        Assert.Empty( aspectInstances[0].Diagnostics );
        Assert.Empty( aspectInstances[0].Advice );
        var aspectClass = compilerOutput.AspectClasses.Single( x => x.ShortName == "Aspect" );
        Assert.Same( aspectInstances[0], aspectClass.Instances[0] );
        Assert.Same( aspectClass, aspectInstances[0].AspectClass );
    }
}