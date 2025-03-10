// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Helpers;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#if NET6_0_OR_GREATER
using Metalama.Framework.Aspects;
#endif

#pragma warning disable IDE0079   // Remove unnecessary suppression.
#pragma warning disable CA1307    // Specify StringComparison for clarity
#pragma warning disable VSTHRD200 // Warning VSTHRD200 : Use "Async" suffix in names of methods that return an awaitable type.

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public sealed class DesignTimePipelineTests : DesignTimePipelineTestsBase
{
    public DesignTimePipelineTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void InitializationWithoutAspect()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string> { ["Class1.cs"] = "public class Class1 { }", ["Class2.cs"] = "public class Class2 { }" };

        var compilation = testContext.CreateCSharpCompilation( code );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
        var pipeline = pipelineFactory.CreatePipeline( compilation );
        Assert.True( pipeline.TryExecute( compilation, default, out _ ) );
    }

    [Fact]
    public void InitializationWithAspect()
    {
        using var testContext = this.CreateTestContext();

        // Test that we can initialize the pipeline with a different compilation than the one with which we execute it.

        var code = new Dictionary<string, string>
        {
            ["Aspect.cs"] =
                "public class Aspect : Metalama.Framework.Aspects.OverrideMethodAspect { public override dynamic OverrideMethod() { return null; } }",
            ["Class1.cs"] = "public class Class1 { }",
            ["Class2.cs"] = "public class Class2 { [Aspect]  void Method() {} }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
        var pipeline = pipelineFactory.CreatePipeline( compilation );
        Assert.True( pipeline.TryExecute( compilation, default, out _ ) );
    }

    [Fact]
    public void NoCompileTimeCode()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "F1.cs", "public class X {}" } } );
        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        var pipeline = factory.CreatePipeline( compilation );

        // First execution of the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        var dumpedResults = DumpResults( results );
        this.TestOutput.WriteLine( dumpedResults );

        const string expectedResult = @"
F1.cs:
0 diagnostic(s):
0 suppression(s):
0 introductions(s):
";

        AssertEx.EolInvariantEqual( expectedResult.Trim(), dumpedResults );

        Assert.Equal( 1, pipeline.PipelineExecutionCount );

        // Second execution. The result should be the same, and the number of executions should not change.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results2 ) );
        var dumpedResults2 = DumpResults( results2 );
        AssertEx.EolInvariantEqual( expectedResult.Trim(), dumpedResults2 );
        Assert.Equal( 1, pipeline.PipelineExecutionCount );
    }

    [Fact]
    public void ErrorInCompileTimeCode()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            using Metalama.Framework.Aspects; 

                            public class Aspect : OverrideMethodAspect
                            {
                                public override dynamic? OverrideMethod()
                                {
                                    dynamic[] x; // This should cause LAMA0227
                                    return null;
                                }
                            }

                            """;

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "F1.cs", code } } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        var pipeline = factory.CreatePipeline( compilation );

        // First execution of the pipeline.
        Assert.False( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _, out var diagnostics ) );
        Assert.Equal( 1, pipeline.PipelineExecutionCount );
        Assert.Single( diagnostics.Where( d => d.Id == TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction.Id ) );

        // Second execution. The result should be the same, and the number of executions should not change.
        Assert.False( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _, out var diagnostics2 ) );
        Assert.Equal( 1, pipeline.PipelineExecutionCount );
        Assert.Single( diagnostics2.Where( d => d.Id == TemplatingDiagnosticDescriptors.InvalidDynamicTypeConstruction.Id ) );
    }

    [Fact]
    public async Task ChangeInAspectCode()
    {
        var assemblyName = "test_" + RandomIdGenerator.GenerateId();

        const string aspectCode = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;

class MyAspect : MethodAspect
{
   private static readonly DiagnosticDefinition<int> _description = new(""MY001"", Severity.Warning, ""AspectVersion=$version$,TargetVersion={0}"" );
   public int Version;

   public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
   {
aspectBuilder.Diagnostics.Report( _description.WithArguments( this.Version ) );
   }
}
";

        const string targetCode = @"
class C
{
   [MyAspect(Version=$version$)]
   void M() {}
}
";

        const string expectedResult = @"
Aspect.cs:
0 diagnostic(s):
0 suppression(s):
0 introductions(s):
----------------------------------------------------------
Target.cs:
1 diagnostic(s):
   Warning MY001 on `M`: `AspectVersion=$AspectVersion$,TargetVersion=$TargetVersion$`
0 suppression(s):
0 introductions(s):
";

        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>()
            {
                { "Aspect.cs", aspectCode.Replace( "$version$", "1" ) }, { "Target.cs", targetCode.Replace( "$version$", "1" ) }
            },
            assemblyName: assemblyName );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        var pipeline = factory.CreatePipeline( compilation );

        // First execution of the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        var dumpedResults = DumpResults( results );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults );
        Assert.Equal( 1, pipeline.PipelineExecutionCount );
        Assert.Equal( 1, pipeline.PipelineInitializationCount );

        // Second execution with the same compilation. The result should be the same, and the number of executions should not change because the result is cached.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results2 ) );
        var dumpedResults2 = DumpResults( results2 );
        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults2 );
        Assert.Equal( 1, pipeline.PipelineExecutionCount );
        Assert.Equal( 1, pipeline.PipelineInitializationCount );

        // Third execution, this time with modified target but same aspect code.
        var compilation3 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>()
            {
                { "Aspect.cs", aspectCode.Replace( "$version$", "1" ) }, { "Target.cs", targetCode.Replace( "$version$", "2" ) }
            },
            assemblyName: assemblyName );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation3, default, out var results3 ) );
        var dumpedResults3 = DumpResults( results3 );

        this.TestOutput.WriteLine( dumpedResults3 );

        Assert.Equal( 2, pipeline.PipelineExecutionCount );
        Assert.Equal( 1, pipeline.PipelineInitializationCount );
        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "2" ).Trim(), dumpedResults3 );

        // Forth execution, with modified aspect but not target code. This should pause the pipeline. We don't resume the pipeline, so we should get the old result.
        var compilation4 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>()
            {
                { "Aspect.cs", aspectCode.Replace( "$version$", "2" ) }, { "Target.cs", targetCode.Replace( "$version$", "2" ) }
            },
            assemblyName: assemblyName );

        var aspect4 = compilation4.SyntaxTrees.Single( t => t.FilePath == "Aspect.cs" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation4, default, out var results4 ) );

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, pipeline.Status );
        Assert.True( factory.EventHub.IsEditingCompileTimeCode );
        Assert.True( pipeline.IsCompileTimeSyntaxTreeOutdated( "Aspect.cs" ) );

        var dumpedResults4 = DumpResults( results4 );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "2" ).Trim(), dumpedResults4 );
        Assert.Equal( 2, pipeline.PipelineExecutionCount );
        Assert.Equal( 1, pipeline.PipelineInitializationCount );

        // There must be an error on the aspect.
        List<Diagnostic> diagnostics4 = new();

        pipeline.ValidateTemplatingCode( compilation4.GetSemanticModel( aspect4 ), diagnostics4.Add, null );

        Assert.Contains(
            diagnostics4,
            d => d.Id == TemplatingDiagnosticDescriptors.CompileTimeTypeNeedsRebuild.Id );

        // Fifth execution, the same scenario as before.
        var compilation5 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>()
            {
                { "Aspect.cs", aspectCode.Replace( "$version$", "3" ) }, { "Target.cs", targetCode.Replace( "$version$", "2" ) }
            },
            assemblyName: assemblyName );

        var aspect5 = compilation5.SyntaxTrees.Single( t => t.FilePath == "Aspect.cs" );

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, pipeline.Status );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation5, default, out var results5 ) );
        var dumpedResults5 = DumpResults( results5 );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "2" ).Trim(), dumpedResults5 );
        Assert.Equal( 2, pipeline.PipelineExecutionCount );
        Assert.Equal( 1, pipeline.PipelineInitializationCount );

        List<Diagnostic> diagnostics5 = new();

        pipeline.ValidateTemplatingCode( compilation5.GetSemanticModel( aspect5 ), diagnostics5.Add, null );

        Assert.Contains(
            diagnostics5,
            d => d.Id == TemplatingDiagnosticDescriptors.CompileTimeTypeNeedsRebuild.Id );

        // Simulate an external build event. This is normally triggered by the build touch file or by a UI signal.
        await pipeline.ResumeAsync( AsyncExecutionContext.Get(), false );
        Assert.False( factory.EventHub.IsEditingCompileTimeCode );

        // A new evaluation of the design-time pipeline should now give the new results.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation5, default, out var results6 ) );
        var dumpedResults6 = DumpResults( results6 );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "3" ).Replace( "$TargetVersion$", "2" ).Trim(), dumpedResults6 );
        Assert.Equal( 3, pipeline.PipelineExecutionCount );
        Assert.Equal( 2, pipeline.PipelineInitializationCount );
        Assert.False( pipeline.IsCompileTimeSyntaxTreeOutdated( "Aspect.cs" ) );

        List<Diagnostic> diagnostics6 = new();

        pipeline.ValidateTemplatingCode( compilation5.GetSemanticModel( aspect5 ), diagnostics6.Add, null );

        Assert.DoesNotContain(
            diagnostics6,
            d => d.Id == TemplatingDiagnosticDescriptors.CompileTimeTypeNeedsRebuild.Id );
    }

    [Fact]
    public async Task ChangeInAspectCodeSeparateProject()
    {
        var aspectAssemblyName = "aspect_" + RandomIdGenerator.GenerateId();
        var targetAssemblyName = "target_" + RandomIdGenerator.GenerateId();

        const string aspectCode = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;

public class MyAspect : MethodAspect
{
   private static readonly DiagnosticDefinition<int> _description = new(""MY001"", Severity.Warning, ""AspectVersion=$version$,TargetVersion={0}"" );
   public int Version;

   public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
   {
       aspectBuilder.Diagnostics.Report( _description.WithArguments( this.Version ) );
   }
}
";

        const string targetCode = @"
class C
{
   [MyAspect(Version=$version$)]
   void M() {}
}
";

        const string expectedResult = @"
Target.cs:
1 diagnostic(s):
   Warning MY001 on `M`: `AspectVersion=$AspectVersion$,TargetVersion=$TargetVersion$`
0 suppression(s):
0 introductions(s):
";

        using var testContext = this.CreateTestContext();

        var aspectCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode.Replace( "$version$", "1" ) } },
            assemblyName: aspectAssemblyName );

        var targetCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Target.cs", targetCode.Replace( "$version$", "1" ) } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { aspectCompilation.ToMetadataReference() } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        var aspectProjectPipeline = factory.CreatePipeline( aspectCompilation );
        var targetProjectPipeline = factory.CreatePipeline( targetCompilation );

        // First execution of the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out var results ) );
        var dumpedResults = DumpResults( results );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );

        // Second execution with the same compilation. The result should be the same, and the number of executions should not change because the result is cached.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out var results2 ) );
        var dumpedResults2 = DumpResults( results2 );
        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults2 );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );

        // Third execution, with modified aspect but not target code. This should pause the pipeline. We don't resume the pipeline, so we should get the old result.
        var aspectCompilation3 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode.Replace( "$version$", "2" ) } },
            assemblyName: aspectAssemblyName );

        var targetCompilation3 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Target.cs", targetCode.Replace( "$version$", "1" ) } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { aspectCompilation3.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation3, default, out var results3 ) );

        await targetProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        await aspectProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, targetProjectPipeline.Status );
        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, aspectProjectPipeline.Status );
        Assert.True( factory.EventHub.IsEditingCompileTimeCode );
        Assert.True( aspectProjectPipeline.IsCompileTimeSyntaxTreeOutdated( "Aspect.cs" ) );

        var dumpedResults3 = DumpResults( results3 );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "1" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults3 );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 1, targetProjectPipeline.PipelineInitializationCount );

        // Simulate an external build event. This is normally triggered by the build touch file or by a UI signal.
        await aspectProjectPipeline.ResumeAsync( AsyncExecutionContext.Get(), false );
        await aspectProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        await targetProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        Assert.Equal( DesignTimeAspectPipelineStatus.Default, targetProjectPipeline.Status );
        Assert.Equal( DesignTimeAspectPipelineStatus.Default, aspectProjectPipeline.Status );
        Assert.False( factory.EventHub.IsEditingCompileTimeCode );

        // A new evaluation of the design-time pipeline should now give the new results.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation3, default, out var results6 ) );
        var dumpedResults6 = DumpResults( results6 );

        AssertEx.EolInvariantEqual( expectedResult.Replace( "$AspectVersion$", "2" ).Replace( "$TargetVersion$", "1" ).Trim(), dumpedResults6 );
        await targetProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        await aspectProjectPipeline.ProcessJobQueueWhenLockAvailableAsync();
        Assert.Equal( 2, targetProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 2, targetProjectPipeline.PipelineInitializationCount );
        Assert.False( targetProjectPipeline.IsCompileTimeSyntaxTreeOutdated( "Aspect.cs" ) );
    }

    [Fact]
    public void ChangeInTargetCode()
    {
        var assemblyName = "test_" + RandomIdGenerator.GenerateId();

        const string aspectCode = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;

class MyAspect : TypeAspect
{
   [Introduce]
   public void NewProperty() { }
}
";

        const string expectedResult = @"
Aspect.cs:
0 diagnostic(s):
0 suppression(s):
0 introductions(s):
----------------------------------------------------------
Target.cs:
0 diagnostic(s):
0 suppression(s):
1 introductions(s):
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class C
{
    public void NewProperty()
    {
    }
}
";

        using var testContext = this.CreateTestContext();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        void TestWithTargetCode( string targetCode )
        {
            var compilation1 = testContext.CreateCSharpCompilation(
                new Dictionary<string, string>() { { "Aspect.cs", aspectCode }, { "Target.cs", targetCode } },
                assemblyName,
                true );

            Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation1, default, out var results ) );

            var dumpedResults = DumpResults( results );

            this.TestOutput.WriteLine( "-----------------" );
            this.TestOutput.WriteLine( dumpedResults );

            Assert.Equal( expectedResult.Trim().Replace( "\r\n", "\n" ), dumpedResults.Trim().Replace( "\r\n", "\n" ) );
        }

        TestWithTargetCode( "[MyAspect] partial class C { }" );
        TestWithTargetCode( "[MyAspect] partial class C { void }" );
        TestWithTargetCode( "[MyAspect] partial class C { void NewMethod() {} }" );
        TestWithTargetCode( "[MyAspect] partial class C { void NewMethod() { ; } }" );
    }

    [Fact]
    public void ProjectDependencyWithNoMetalamaReferenceButSystemCompileTimeType()
    {
        using var testContext = this.CreateTestContext();

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // The dependency cannot have a reference to Metalama.
        // It needs to define a system type that is considered as compile-time.
        var dependency = testContext.CreateCSharpCompilation( "namespace System; struct Index {}", addMetalamaReferences: false );

        // The main compilation must have a compile-time syntax tree.
        var compilation = testContext.CreateCompilationModel(
            "using Metalama.Framework.Aspects;  class A : TypeAspect {}",
            additionalReferences: new[] { dependency.ToMetadataReference() } );

        Assert.True( pipelineFactory.TryExecute( testContext.ProjectOptions, compilation.RoslynCompilation, default, out _ ) );
    }

    [Fact]
    public void ChangeInDependency_CacheInvalidation()
    {
        var observer = new TestDesignTimePipelineObserver();
        var mocks = new AdditionalServiceCollection( observer );
        using var testContext = this.CreateTestContext( mocks );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var dependentCode = new Dictionary<string, string>()
        {
            ["dependent.cs"] = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using System.Linq;
using System;

class MyAspect : MethodAspect
{
   private static readonly DiagnosticDefinition<string> _description = new(""MY001"", Severity.Warning, ""Fields='{0}'"" );
   
   public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
   {
        var allFields = string.Join( "","",  aspectBuilder.Target.DeclaringType.AllFields.Select( f => f.Name ) );
        aspectBuilder.Diagnostics.Report( _description.WithArguments( allFields ) );
   }
}

class C : BaseClass
{
   [MyAspect]
   void M() {}
}
"
        };

        // First compilation.
        var masterCode1 = new Dictionary<string, string>() { ["master.cs"] = @"public class BaseClass { public int Field1; }" };

        var masterCompilation1 = testContext.CreateCSharpCompilation( masterCode1, assemblyName: "Master" );

        var dependentCompilation1 = testContext.CreateCSharpCompilation(
            dependentCode,
            assemblyName: "Dependent",
            additionalReferences: new[] { masterCompilation1.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, dependentCompilation1, default, out var results1 ) );

        Assert.Equal( 2, observer.InitializePipelineEvents.Count );
        Assert.Contains( "Master", observer.InitializePipelineEvents );
        Assert.Contains( "Dependent", observer.InitializePipelineEvents );
        observer.InitializePipelineEvents.Clear();

        Assert.Contains(
            "Fields='Field1'",
            results1.Result.SyntaxTreeResults.Single().Value.Diagnostics.Single().GetMessage( CultureInfo.InvariantCulture ) );

        // Second compilation with a different master compilation.
        var masterCode2 = new Dictionary<string, string>() { ["master.cs"] = @"public partial class BaseClass { public int Field2; }" };

        var masterCompilation2 = testContext.CreateCSharpCompilation( masterCode2, assemblyName: "Master" );

        var dependentCompilation2 = testContext.CreateCSharpCompilation(
            dependentCode,
            assemblyName: "Dependent",
            additionalReferences: new[] { masterCompilation2.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, dependentCompilation2, default, out var results2 ) );

        Assert.Empty( observer.InitializePipelineEvents );

        Assert.Contains(
            "Fields='Field2'",
            results2.Result.SyntaxTreeResults.Single().Value.Diagnostics.Single().GetMessage( CultureInfo.InvariantCulture ) );

        // Third compilation. Add a syntax tree with a partial type.
        var masterCode3 = new Dictionary<string, string>()
        {
            ["master.cs"] = @"public partial class BaseClass { public int Field2; }", ["partial.cs"] = "partial class BaseClass { public int Field3; }"
        };

        var masterCompilation3 = testContext.CreateCSharpCompilation( masterCode3, assemblyName: "Master" );

        var dependentCompilation3 = testContext.CreateCSharpCompilation(
            dependentCode,
            assemblyName: "Dependent",
            additionalReferences: new[] { masterCompilation3.ToMetadataReference() } );

        observer.InitializePipelineEvents.Clear();

        Assert.True( factory.TryExecute( testContext.ProjectOptions, dependentCompilation3, default, out var results3 ) );

        Assert.Empty( observer.InitializePipelineEvents );

        Assert.Contains(
            "Fields='Field2,Field3'",
            results3.Result.SyntaxTreeResults.Single().Value.Diagnostics.Single().GetMessage( CultureInfo.InvariantCulture ) );
    }

    [Fact]
    public async Task FixingTemplateErrorAsync()
    {
        using var testContext = this.CreateTestContext();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        const string code1 = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

class MyAspect : TypeAspect
{
   SomeError;
}

";

        var compilation1 = testContext.CreateCSharpCompilation( code1, assemblyName: "project", ignoreErrors: true );

        var result1 = await factory.ExecuteAsync( compilation1, AsyncExecutionContext.Get() );
        Assert.False( result1.IsSuccessful );

        const string code2 = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

class MyAspect : TypeAspect
{
   
}

";

        var compilation2 = testContext.CreateCSharpCompilation( code2, assemblyName: "project" );

        var result2 = await factory.ExecuteAsync( compilation2, AsyncExecutionContext.Get() );
        Assert.True( result2.IsSuccessful );
    }

    [Fact]
    public void CompilationMissingAnyReferenceDuringInitialization()
    {
        using var testContext = this.CreateTestContext();
        var observer = new TestDesignTimePipelineObserver();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider.WithService( observer ) );

        var code = new Dictionary<string, string>()
        {
            ["dependent.cs"] = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using System.Linq;
using System;

class MyAspect : MethodAspect
{
   private static readonly DiagnosticDefinition<string> _description = new(""MY001"", Severity.Warning, ""Fields='{0}'"" );
   
   public override void BuildAspect( IAspectBuilder<IMethod> aspectBuilder )
   {
        var allFields = string.Join( "","",  aspectBuilder.Target.DeclaringType.AllFields.Select( f => f.Name ) );
        aspectBuilder.Diagnostics.Report( _description.WithArguments( allFields ) );
   }
}

class C 
{
   [MyAspect]
   void M() {}
}
"
        };

        // First compilation without any reference.

        var compilation1 = testContext.CreateCSharpCompilation( code, assemblyName: "Project" ).WithReferences( Enumerable.Empty<MetadataReference>() );

        Assert.False( factory.TryExecute( testContext.ProjectOptions, compilation1, default, out _ ) );

        // Second compilation with proper references.
        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: "Project" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation2, default, out _ ) );
    }

    [Fact]
    public async Task PipelineConfigurationDoesNotKeepReferenceToCompilation()
    {
        var output = await this.PipelineConfigurationDoesNotKeepReferenceToCompilationCore();

        for ( var i = 0; i < 10; i++ )
        {
            var hasDanglingRef = false;

            if ( output.DependentCompilationRef.IsAlive )
            {
                hasDanglingRef = true;
                this.TestOutput.WriteLine( "Reference to the dependent compilation." );
            }

            if ( output.MasterCompilationRef.IsAlive )
            {
                hasDanglingRef = true;
                this.TestOutput.WriteLine( "Reference to the master compilation." );
            }

            if ( output.SyntaxTreeRefs.Any( r => r.IsAlive ) )
            {
                hasDanglingRef = true;
                this.TestOutput.WriteLine( "Reference to a syntax tree." );
            }

            if ( !hasDanglingRef )
            {
                this.TestOutput.WriteLine( "No more dangling reference." );

                return;
            }

            this.TestOutput.WriteLine( "GC.Collect()" );
#if NET6_0_OR_GREATER
            this.TestOutput.WriteLine( $"Finalizing queue: {GC.GetGCMemoryInfo().FinalizationPendingCount}" );
#endif
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        MemoryDumpHelper.CaptureMiniDumpOnce();
        MemoryDumpHelper.CaptureDotMemoryDumpAndThrow( $"There is still a dangling reference to {output.DependentCompilationRef.Target}." );

        GC.KeepAlive( output.Configuration );
    }

    private async Task<(
            WeakReference MasterCompilationRef,
            WeakReference DependentCompilationRef,
            List<WeakReference> SyntaxTreeRefs,
            AspectPipelineConfiguration Configuration,
            DesignTimeAspectPipeline Pipeline)>
        PipelineConfigurationDoesNotKeepReferenceToCompilationCore()
    {
        using var testContext = this.CreateTestContext();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

        var (masterCompilation, dependentCompilation) = CreateCompilations( testContext, 1 );
        var syntaxTreeRefs = new List<WeakReference>();
        syntaxTreeRefs.AddRange( masterCompilation.SyntaxTrees.Select( x => new WeakReference( x ) ) );
        syntaxTreeRefs.AddRange( dependentCompilation.SyntaxTrees.Select( x => new WeakReference( x ) ) );

        var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, dependentCompilation )!;

        var configuration = await pipeline.GetConfigurationAsync(
            PartialCompilation.CreateComplete( dependentCompilation ),
            true,
            AsyncExecutionContext.Get(),
            default );

        var (_, dependentCompilation2) = CreateCompilations( testContext, 2 );

        // This is to make sure that the first compilation is not the last one, because it's ok to hold a reference to the last-seen compilation.
        await pipeline.ExecuteAsync( dependentCompilation2, true, AsyncExecutionContext.Get() );

        Assert.Same( pipeline.LastProjectVersion!.Compilation, dependentCompilation2 );

        return (new WeakReference( masterCompilation ), new WeakReference( dependentCompilation ), syntaxTreeRefs, configuration.Value, pipeline);
    }

    private static ( CSharpCompilation Master, CSharpCompilation Dependent ) CreateCompilations( TestContext testContext, int version )
    {
        var masterCode = new Dictionary<string, string>()
        {
            ["aspect.cs"] = $@"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using System.Linq;
using System;

public class MyAspect{version} : OverrideMethodAspect
{{
   public override dynamic? OverrideMethod() 
   {{
      return meta.Proceed();
    }}
}}
",
            ["usage.cs"] = $@"

class C{version} 
{{
   [MyAspect{version}]
   void M() {{}}
}}
"
        };

        var dependentCode = new Dictionary<string, string>()
        {
            ["dependent.cs"] = $@"
class D{version} 
{{
   [MyAspect{version}]
   void M() {{}}
}}

"
        };

        var masterCompilation = testContext.CreateCSharpCompilation(
            masterCode,
            assemblyName: "Master" );

        var dependentCompilation = testContext.CreateCSharpCompilation(
            dependentCode,
            assemblyName: "Dependent",
            additionalReferences: new[] { masterCompilation.ToMetadataReference() } );

        return (masterCompilation, dependentCompilation);
    }

#if NET6_0_OR_GREATER
    [SkippableFact]
    public void OverrideMethodWithMultipleTargetFrameworks()
    {
        const string code =
            """
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects; 

            class Aspect : OverrideMethodAspect
            {
                public override dynamic? OverrideMethod() => null;
            }

            class Target
            {
                [Aspect]
                void M() {}
            }
            """;

        using var testContext = this.CreateTestContext();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var netCompilation = testContext.CreateCSharpCompilation( code, assemblyName: "Project" );

        var coreReferences = netCompilation.References
            .Where( reference => reference.Display?.EndsWith( "Metalama.Framework.dll", StringComparison.Ordinal ) != true );

        var netMetalamaFrameworkPath = typeof(CompileTimeAttribute).Assembly.Location;
        var baseDirectoryPath = new FileInfo( netMetalamaFrameworkPath ).Directory!.Parent!.FullName;
        var metalamaFrameworkAssemblyName = Path.GetFileName( netMetalamaFrameworkPath );

        var netFrameworkMetalamaFrameworkPath = Path.Combine( baseDirectoryPath, "netframework4.8", metalamaFrameworkAssemblyName );

        // It may be possible that only the .Net 6.0 TFM of this project has been built. In that case, this test cannot proceed.
        Skip.If( !File.Exists( netFrameworkMetalamaFrameworkPath ) );

        var netFrameworkCompilation = testContext.CreateCSharpCompilation( "", assemblyName: "Project" )
            .WithReferences( coreReferences.Append( MetadataReference.CreateFromFile( netFrameworkMetalamaFrameworkPath ) ) );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, netFrameworkCompilation, default, out _ ) );
        Assert.True( factory.TryExecute( testContext.ProjectOptions, netCompilation, default, out var result ) );

        foreach ( var (_, treeResult) in result.Result.SyntaxTreeResults )
        {
            Assert.Empty( treeResult.Diagnostics );
        }
    }
#endif

    [Fact]
    public async Task ResumeWithErrorAsync()
    {
        static CSharpCompilation CreateCompilation( TestContext testContext, string statement )
        {
            var code = new Dictionary<string, string>
            {
                ["Aspect.cs"] =
                    $$"""
                      using Metalama.Framework.Advising;
                      using Metalama.Framework.Aspects; 
                      using System;

                      public class Aspect : OverrideMethodAspect
                      {
                          public override dynamic? OverrideMethod()
                          {
                              {{statement}}
                              return null;
                          }
                      }
                      """
            };

            return testContext.CreateCSharpCompilation( code, ignoreErrors: true );
        }

        using var testContext = this.CreateTestContext();
        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation1 = CreateCompilation( testContext, "" );
        var pipeline = pipelineFactory.CreatePipeline( compilation1 );

        Assert.Equal( DesignTimeAspectPipelineStatus.Default, pipeline.Status );

        // Execute with the initial valid code.
        Assert.True( pipeline.TryExecute( compilation1, default, out _ ) );

        Assert.Equal( DesignTimeAspectPipelineStatus.Ready, pipeline.Status );

        // Execute with incomplete/invalid statement.
        var compilation2 = CreateCompilation( testContext, "Console" );
        Assert.True( pipeline.TryExecute( compilation2, default, out var compilationResult ) );

        // Note that LAMA0118 is no longer reported by the pipeline but by the analyzer.
        Assert.Empty( compilationResult.GetDiagnosticsOnSyntaxTree( "Aspect.cs" ) );

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, pipeline.Status );

        // Resume while the code is still invalid.
        await pipeline.ResumeAsync( AsyncExecutionContext.Get(), false );

        Assert.Equal( DesignTimeAspectPipelineStatus.Default, pipeline.Status );

        // Executing with the same code fails at this point.
        var executionResult = await pipeline.ExecuteAsync( compilation2, AsyncExecutionContext.Get() );
        Assert.False( executionResult.IsSuccessful );

        Assert.Equal( DesignTimeAspectPipelineStatus.Default, pipeline.Status );

        // Executing with new invalid code fails and causes pausing.
        var compilation3 = CreateCompilation( testContext, "Console.Write" );
        executionResult = await pipeline.ExecuteAsync( compilation3, AsyncExecutionContext.Get() );
        Assert.False( executionResult.IsSuccessful );

        // Note that LAMA0118 is no longer reported by the pipeline but by the analyzer.
        Assert.Empty( executionResult.Diagnostics );

        Assert.Equal( DesignTimeAspectPipelineStatus.Paused, pipeline.Status );
    }

    [Fact]
    public void GenericTargetIntroductions()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects; 
            using Metalama.Framework.Code;

            public class RepositoryAspect : TypeAspect
            {
                [Introduce]
                public int Id { get; set; }
            }

            [RepositoryAspect]
            public partial class Repository<T1, T2>
            {
            }

            [RepositoryAspect]
            public partial interface IVariantRepository<in T>
            {
            }

            public partial class Outer<T1>
            {
                [RepositoryAspect]
                public partial class Inner<T2>
                {
                }
            }
            """;

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "F1.cs", code } } );
        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        var dumpedResults = DumpResults( results );
        this.TestOutput.WriteLine( dumpedResults );

        const string expectedResult =
            """
            F1.cs:
            0 diagnostic(s):
            0 suppression(s):
            3 introductions(s):
            /// <auto-generated>
            /// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
            /// </auto-generated>
            partial interface IVariantRepository<in T>
            {
                global::System.Int32 Id { get; set; }
            }
            /// <auto-generated>
            /// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
            /// </auto-generated>
            partial class Outer<T1>
            {
                partial class Inner<T2>
                {
                    public global::System.Int32 Id { get; set; }
                }
            }
            /// <auto-generated>
            /// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
            /// </auto-generated>
            partial class Repository<T1, T2>
            {
                public global::System.Int32 Id { get; set; }
            }
            """;

        Assert.Equal( expectedResult.Replace( "\r\n", "\n" ), dumpedResults.Replace( "\r\n", "\n" ) );
    }

    [Fact]
    public void OptionsTest()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["options.cs"] = OptionsTestHelper.OptionsCode,
            ["aspect.cs"] = OptionsTestHelper.ReportWarningFromOptionAspectCode,
            ["fabric.cs"] = """
                            using Metalama.Framework.Fabrics;
                            using Metalama.Framework.Options;
                            class Fabric : ProjectFabric
                            {
                                public override void AmendProject( IProjectAmender amender )
                                {
                                    amender.SetOptions( o => new MyOptions { Value = "THE_VALUE" } );
                                }
                            }
                            """,
            ["target.cs"] =
                """
                class C
                {
                    [ReportWarningFromOptionsAspect]
                    void M(){}
                }
                """
        };

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var compilation = testContext.CreateCSharpCompilation( code );
        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        Assert.Contains( results.GetAllDiagnostics(), d => d.GetMessage( CultureInfo.InvariantCulture ).Contains( "Option='THE_VALUE'" ) );
    }

    [Fact]
    public async Task OptionsCrossProjectTest()
    {
        using var testContext = this.CreateTestContext();

        const string fabricCode = """
                                  using Metalama.Framework.Fabrics;
                                  using Metalama.Framework.Options;
                                  class Fabric : ProjectFabric
                                  {
                                      public override void AmendProject( IProjectAmender amender )
                                      {
                                          amender.SetOptions( o => new MyOptions { Value = "THE_VALUE" } );
                                      }
                                  }
                                  """;

        var dependencyCode = new Dictionary<string, string>()
        {
            ["options.cs"] = OptionsTestHelper.OptionsCode,
            ["fabric.cs"] = fabricCode,
            ["code.cs"] =
                """
                public class C {}
                """
        };

        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] = OptionsTestHelper.ReportWarningFromOptionAspectCode,
            ["target.cs"] =
                """
                class D : C
                {
                    [ReportWarningFromOptionsAspect]
                    void M(){}
                }
                """
        };

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var dependencyCompilation = testContext.CreateCSharpCompilation( dependencyCode, assemblyName: "dependency" );

        var compilation = testContext.CreateCSharpCompilation(
            code,
            assemblyName: "main",
            additionalReferences: new[] { dependencyCompilation.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        Assert.Contains( results.GetAllDiagnostics(), d => d.GetMessage( CultureInfo.InvariantCulture ).Contains( "Option='THE_VALUE'" ) );

        // Try an update.
        dependencyCode["fabric.cs"] = fabricCode.Replace( "THE_VALUE", "THE_UPDATED_VALUE" );
        var updatedDependencyCompilation = testContext.CreateCSharpCompilation( dependencyCode, assemblyName: "dependency" );

        var updatedCompilation = testContext.CreateCSharpCompilation(
            code,
            assemblyName: "main",
            additionalReferences: new[] { updatedDependencyCompilation.ToMetadataReference() } );

        // The next pipeline run will pause the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, updatedCompilation, default, out _ ) );

        // Resume the pipeline.
        await factory.ResumePipelinesAsync( AsyncExecutionContext.Get(), false, default );

        // Run the pipeline again.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, updatedCompilation, default, out var updatedResults ) );
        Assert.Contains( updatedResults.GetAllDiagnostics(), d => d.GetMessage( CultureInfo.InvariantCulture ).Contains( "Option='THE_UPDATED_VALUE'" ) );
    }

    [Fact]
    public void AnnotationsCrossProjectTest()
    {
        using var testContext = this.CreateTestContext();

        var dependencyCode = new Dictionary<string, string>()
        {
            ["annotation.cs"] =
                """
                using Metalama.Framework.Code;
                public class TheAnnotation : IAnnotation<INamedType>
                {
                    public string Value;
                    
                    public TheAnnotation( string value )
                    {
                        this.Value = value;
                    }
                }
                """,
            ["aspect.cs"] =
                """
                using Metalama.Framework.Advising;
                using Metalama.Framework.Aspects; 
                using Metalama.Framework.Code;
                public class AddAnnotation : TypeAspect
                {
                    public string Value;
                   
                   public AddAnnotation( string value )
                   {
                       this.Value = value;
                   }
                    
                    public override void BuildAspect( IAspectBuilder<INamedType> builder )
                    {
                        builder.AddAnnotation( new TheAnnotation(this.Value), true );
                    }
                }
                """,
            ["code.cs"] =
                """
                [AddAnnotation("THE_VALUE")]
                public class C {}
                """
        };

        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] =
                """
                using Metalama.Framework.Advising;
                using Metalama.Framework.Aspects; 
                using Metalama.Framework.Code;
                using Metalama.Framework.Diagnostics;
                using Metalama.Framework.Eligibility;
                using System.Linq;
                using System;

                class ReportWarningFromAnnotationAspect : TypeAspect
                {
                   private static readonly DiagnosticDefinition<string> _description = new("MY001", Severity.Warning, "Option='{0}'" );
                   
                   public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
                   {
                        var annotation = aspectBuilder.Target.BaseType.Enhancements().GetAnnotations<TheAnnotation>().SingleOrDefault();
                        aspectBuilder.Diagnostics.Report( _description.WithArguments( annotation?.Value ?? "<undefined>" ) );
                   }
                }
                """,
            ["target.cs"] =
                """
                [ReportWarningFromAnnotationAspect]
                class D : C
                {
                    
                }
                """
        };

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var dependencyCompilation = testContext.CreateCSharpCompilation( dependencyCode, assemblyName: "dependency" );

        var compilation = testContext.CreateCSharpCompilation(
            code,
            assemblyName: "main",
            additionalReferences: new[] { dependencyCompilation.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results ) );
        Assert.Contains( results.GetAllDiagnostics(), d => d.GetMessage( CultureInfo.InvariantCulture ).Contains( "Option='THE_VALUE'" ) );

        // Try an update.
        dependencyCode["code.cs"] = dependencyCode["code.cs"].Replace( "THE_VALUE", "THE_UPDATED_VALUE" );
        var updatedDependencyCompilation = testContext.CreateCSharpCompilation( dependencyCode, assemblyName: "dependency" );

        var updatedCompilation = testContext.CreateCSharpCompilation(
            code,
            assemblyName: "main",
            additionalReferences: new[] { updatedDependencyCompilation.ToMetadataReference() } );

        // Run the pipeline again.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, updatedCompilation, default, out var updatedResults ) );
        Assert.Contains( updatedResults.GetAllDiagnostics(), d => d.GetMessage( CultureInfo.InvariantCulture ).Contains( "Option='THE_UPDATED_VALUE'" ) );
    }

    [Fact]
    public void SameFileTwiceTest()
    {
        // This tests a situation that happens in VS for some reason when a new C# file is added to a project.

        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string> { ["Empty.cs"] = string.Empty };

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var compilation = testContext.CreateCSharpCompilation( code );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );

        const string firstFileCode =
            """
            class C
            {
            }
            """;

        const string secondFileCode =
            """
            internal class C
            {
            }
            """;

        var options = compilation.SyntaxTrees[0].Options;

        compilation = compilation.AddSyntaxTrees(
            SyntaxFactory.ParseSyntaxTree( firstFileCode, options, "C.cs" ),
            SyntaxFactory.ParseSyntaxTree( secondFileCode, options, "C.cs" ) );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }

    [Fact]
    public void AssemblyLocatorTest()
    {
        // Tests that IAssemblyLocator contains the correct references when project changes.

        using var testContext = this.CreateTestContext();

        var dependencyPath = Path.Combine( testContext.BaseDirectory, "dependency.dll" );

        var dependency = testContext.CreateCSharpCompilation( code: new Dictionary<string, string>(), assemblyName: "dependency" );
        var emitResult = dependency.Emit( dependencyPath );

        Assert.True( emitResult.Success );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var compilation = testContext.CreateCSharpCompilation( code: new Dictionary<string, string>() );

        var pipeline1 = factory.CreatePipeline( compilation );

        var pipeline2 = factory.CreatePipeline( compilation );

        Assert.Same( pipeline1, pipeline2 );

        var assemblyLocator1 = pipeline1.ServiceProvider.GetRequiredService<IAssemblyLocator>();

        Assert.False( assemblyLocator1.TryFindAssembly( new AssemblyIdentity( "dependency" ), out _ ) );

        var dependencyReference = MetadataReference.CreateFromFile( dependencyPath );
        compilation = compilation.AddReferences( dependencyReference );

        var pipeline3 = factory.CreatePipeline( compilation );

        var assemblyLocator2 = pipeline3.ServiceProvider.GetRequiredService<IAssemblyLocator>();

        Assert.True( assemblyLocator2.TryFindAssembly( new AssemblyIdentity( "dependency" ), out var foundReference ) );
        Assert.Same( dependencyReference, foundReference );
    }

    [Fact]
    public void PromotedFieldAccessor()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects; 
            using Metalama.Framework.Code;
            using Metalama.Framework.Fabrics;
            using System;
            using System.Linq;

            [assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(UninlineableOverrideAspect), typeof(OverridePropertyAttribute))]

            class OverridePropertyAttribute : OverrideFieldOrPropertyAspect
            {
                public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
                {
                    base.BuildAspect( builder );
                    
                    foreach ( var a in ((IField)builder.AdvisedTarget).OverridingProperty.Accessors )
                    {
                        builder.With( a ).AddAspect<UninlineableOverrideAspect>();
                    }
                }
                public override dynamic? OverrideProperty
                {
                    get
                    {
                        Console.WriteLine("This is the overridden getter.");
                        return meta.Proceed();
                    }
            
                    set
                    {
                        Console.WriteLine($"This is the overridden setter.");
                        meta.Proceed();
                    }
                }
            }

            class UninlineableOverrideAspect : OverrideMethodAspect
            {
                public override dynamic? OverrideMethod()
                {
                    if (new Random().Next() == 0)
                    {
                        Console.WriteLine($"Uninlineable: randomly");
                        return meta.Proceed();
                    }
                    else
                    {
                        Console.WriteLine($"Uninlineable: normally");
                        return meta.Proceed();
                    }
                }
            }

            class TargetClass
            {
                [OverrideProperty]
                int i;
            }
            """;

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "code.cs", code } } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }

    [Fact]
    public void FabricTest()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            using Metalama.Framework.Fabrics;

                            public class TargetClass
                            {
                                class Fabric : TypeFabric
                                {
                                }
                            }
                            """;

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "code.cs", code } } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }

    [Fact]
    public void HasAspectInEligibility()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            using System;
            using System.Linq;
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects; 
            using Metalama.Framework.Code;
            using Metalama.Framework.Eligibility;

            class Aspect1 : OverrideMethodAspect
            {
                public override void BuildEligibility(IEligibilityBuilder<IMethod> builder)
                {
                    builder.MustSatisfy(method => !method.Enhancements().HasAspect<Aspect2>(), _ => $"");
                }
            
                public override dynamic? OverrideMethod()
                {
                    throw new NotImplementedException();
                }
            }

            class Aspect2 : MethodAspect
            {
                public override void BuildEligibility(IEligibilityBuilder<IMethod> builder)
                {
                    builder.MustSatisfy(method => !method.Enhancements().HasAspect<OverrideMethodAspect>(), _ => $"");
                }
            }

            class TargetCode
            {
                private void NoAspectMethod() {}
            
                [Aspect1]
                private int Aspect1Method(int a)
                {
                    return a;
                }
            
                [Aspect2]
                private void Aspect2Method() { }
            }
            """;

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string>() { { "code.cs", code } } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation );

        Assert.True( pipeline.TryExecute( compilation, default, out _ ) );

        var noAspectMethod = compilation.GetSymbolsWithName( "NoAspectMethod" ).OfType<IMethodSymbol>().Single();
        var aspect1Method = compilation.GetSymbolsWithName( "Aspect1Method" ).OfType<IMethodSymbol>().Single();
        var aspect2Method = compilation.GetSymbolsWithName( "Aspect2Method" ).OfType<IMethodSymbol>().Single();

        Assert.Equal(
            ["Aspect1", "Aspect2"],
            pipeline.GetEligibleAspects( compilation, noAspectMethod, default ).SelectAsArray( a => a.FullName ).OrderBy( a => a ) );

        Assert.Empty( pipeline.GetEligibleAspects( compilation, aspect1Method, default ) );
        Assert.Empty( pipeline.GetEligibleAspects( compilation, aspect2Method, default ) );
    }

    [Fact]
    public void AssemblyVersion()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] =
                """
                using Metalama.Framework.Aspects;

                class MyAspect : MethodAspect
                {
                }
                """,
            ["target.cs"] =
                """
                class Target
                {
                    [MyAspect]
                    void M()
                    {
                    }
                }
                """
        };

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

        Assert.Empty( result.GetAllDiagnostics() );

        code.Add( "assemblyattribute.cs", """[assembly: System.Reflection.AssemblyVersion("1.2.3.4")]""" );

        var compilation2 = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation2, default, out var result2 ) );

        Assert.Empty( result2.GetAllDiagnostics() );
    }

    [Fact]
    public void SuppressionOnDeclarativeAdviceFromAnotherProject()
    {
        using var testContext = this.CreateTestContext();

        var libraryCode = new Dictionary<string, string>
        {
            ["introduceDependency.cs"] =
                """
                using Metalama.Framework.Aspects;
                using Metalama.Framework.Code;
                using Metalama.Framework.Diagnostics;

                public class IntroduceDependencyAttribute : DeclarativeAdviceAttribute
                {
                    internal static readonly SuppressionDefinition NonNullableFieldMustContainValue = new( "CS8618" );
                
                    public sealed override void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder )
                    {
                        builder.Diagnostics.Suppress( NonNullableFieldMustContainValue, templateMember );
                    }
                }
                """,
            ["aspect.cs"] =
                """
                using Metalama.Framework.Aspects;

                public interface ILogger;

                public class LogAttribute : MethodAspect
                {
                    [IntroduceDependency]
                    private readonly ILogger _logger;
                }
                """
        };

        var targetCode = new Dictionary<string, string>
        {
            ["target.cs"] =
                """
                class C
                {
                    [Log]
                    void M() {}
                }
                """
        };

        var libraryCompilation = testContext.CreateCSharpCompilation( libraryCode );

        var targetCompilation = testContext.CreateCSharpCompilation( targetCode, additionalReferences: [libraryCompilation.ToMetadataReference()] );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out _ ) );
    }

    [Fact]
    public void OldPipelineDoesntLeak()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>();

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        var targetCompilation = testContext.CreateCSharpCompilation( code, "test" );

        var targetPipeline1 = CreatePipeline( testContext.ProjectOptions );

        CreatePipeline( new TestProjectOptions( testContext.TestProjectOptions, CodeFormattingOptions.None ) );

        GC.Collect();

        Assert.False( targetPipeline1.TryGetTarget( out _ ) );

        WeakReference<DesignTimeAspectPipeline> CreatePipeline( IProjectOptions options )
        {
            var pipeline = factory.GetOrCreatePipeline( options, targetCompilation );

            return new WeakReference<DesignTimeAspectPipeline>( pipeline );
        }
    }

    [Fact]
    public void AssemblyAttributeOptionsAdded()
    {
        const string options =
            """
            using Metalama.Framework.Code;
            using Metalama.Framework.Options;
            using System;
            using System.Collections.Generic;

            class MyOptions : IHierarchicalOptions<IMethod>, IHierarchicalOptions<ICompilation>
            {
                public bool? IsEnabled { get; init; }
            
                public object ApplyChanges(object changes, in ApplyChangesContext context)
                {
                    var other = (MyOptions)changes;
            
                    return new MyOptions { IsEnabled = other.IsEnabled ?? this.IsEnabled };
                }
            
                public IHierarchicalOptions? GetDefaultOptions(OptionsInitializationContext context) => null;
            }

            [AttributeUsage(AttributeTargets.Assembly)]
            class MyOptionsAttribute : Attribute, IHierarchicalOptionsProvider
            {
                public bool IsEnabled { get; init; }
            
                public IEnumerable<IHierarchicalOptions> GetOptions(in OptionsProviderContext context)
                {
                    return [new MyOptions { IsEnabled = this.IsEnabled }];
                }
            }
            """;

        const string aspect =
            """
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;
            using Metalama.Framework.Diagnostics;

            class Aspect : MethodAspect
            {
                static DiagnosticDefinition notEnabledWarning = new("NE", Severity.Warning, "Not enabled.");
            
                public override void BuildAspect(IAspectBuilder<IMethod> builder)
                {
                    var options = builder.Target.Enhancements().GetOptions<MyOptions>();
            
                    if (options.IsEnabled != true)
                    {
                        builder.Diagnostics.Report(notEnabledWarning);
                    }
                }
            }
            """;

        const string optionsAttribute = """[assembly: MyOptions(IsEnabled = true)]""";

        const string target =
            """
            class Target
            {
                [Aspect]
                void M() { }
            }
            """;

        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["options.cs"] = options,
            ["aspect.cs"] = aspect,
            ["optionsAttribute.cs"] = "",
            ["target.cs"] = target,
#if NETFRAMEWORK
            ["isexternalinit.cs"] = "namespace System.Runtime.CompilerServices { internal static class IsExternalInit; }"
#endif
        };

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

        var warning = Assert.Single( result.GetAllDiagnostics() );

        Assert.Equal( "Not enabled.", warning.GetMessage( CultureInfo.InvariantCulture ) );

        code["optionsAttribute.cs"] = optionsAttribute;

        var updatedCompilation = testContext.CreateCSharpCompilation( code, assemblyName: "test" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, updatedCompilation, default, out var updatedResult ) );

        Assert.Empty( updatedResult.GetAllDiagnostics() );
    }

    [Fact]
    public void DirectReferenceAspectConflict()
    {
        // Simulates situation where two aspects of the same name are directly visible through aliases.
        // This is not something we want to support but should not cause a crash. In reality this 
        var aspectAssemblyName1 = "aspect1_" + RandomIdGenerator.GenerateId();
        var aspectAssemblyName2 = "aspect2_" + RandomIdGenerator.GenerateId();
        var targetAssemblyName = "target_" + RandomIdGenerator.GenerateId();

        const string aspectCode = @"
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;

public class MyAspect : MethodAspect
{
}
";

        const string targetCode = @"
extern alias aspects1;
extern alias aspects2;

class C
{
   [aspects1::MyAspect]
   [aspects2::MyAspect]
   void M() {}
}
";

        const string expectedResult = @"
Target.cs:
0 diagnostic(s):
0 suppression(s):
0 introductions(s):
";

        using var testContext = this.CreateTestContext();

        var aspectCompilation1 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode } },
            assemblyName: aspectAssemblyName1 );

        var aspectCompilation2 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode } },
            assemblyName: aspectAssemblyName2 );

        var targetCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Target.cs", targetCode } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { aspectCompilation1.ToMetadataReference( ["aspects1"] ), aspectCompilation2.ToMetadataReference( ["aspects2"] ) } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        factory.CreatePipeline( aspectCompilation1 );
        factory.CreatePipeline( aspectCompilation2 );
        var targetProjectPipeline = factory.CreatePipeline( targetCompilation );

        // Execute the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out var results ) );
        var dumpedResults = DumpResults( results );

        AssertEx.EolInvariantEqual( expectedResult.Trim(), dumpedResults );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );
    }

    [Fact]
    public void IndirectReferenceAspectConflict()
    {
        // Simulates situation where two aspects of the same name are indirectly visible through two separate references.
        // This simulates the situation which occurs while renaming a project in Visual Studio.
        var aspect1AssemblyName = "aspect1_" + RandomIdGenerator.GenerateId();
        var aspect2AssemblyName = "aspect2_" + RandomIdGenerator.GenerateId();
        var leftAssemblyName = "left_" + RandomIdGenerator.GenerateId();
        var rightAssemblyName = "right_" + RandomIdGenerator.GenerateId();
        var targetAssemblyName = "target_" + RandomIdGenerator.GenerateId();

        const string aspectCode = """
                                  using Metalama.Framework.Aspects; 
                                  using Metalama.Framework.Code;
                                  using Metalama.Framework.Diagnostics;
                                  using Metalama.Framework.Eligibility;

                                  [Inheritable]
                                  public class MyAspect : TypeAspect
                                  {
                                  }
                                  """;

        const string leftCode = """
                                [MyAspect]
                                public class Left
                                {
                                }
                                """;

        const string rightCode = """
                                 [MyAspect]
                                 public class Right
                                 {
                                 }
                                 """;

        const string targetCode = """
                                  class C : Left {}
                                  class D : Right {}
                                  """;

        var expectedResult = $"""
                              :
                              2 diagnostic(s):
                                 Error LAMA0113 on ``: `Cannot find in the current compilation the aspect type 'MyAspect' defined in the aspect library '{aspect1AssemblyName}'.`
                                 Error LAMA0113 on ``: `Cannot find in the current compilation the aspect type 'MyAspect' defined in the aspect library '{aspect2AssemblyName}'.`
                              0 suppression(s):
                              0 introductions(s):
                              ----------------------------------------------------------
                              Target.cs:
                              0 diagnostic(s):
                              0 suppression(s):
                              0 introductions(s):
                              """;

        using var testContext = this.CreateTestContext();

        var aspect1Compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode } },
            assemblyName: aspect1AssemblyName );

        var aspect2Compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode } },
            assemblyName: aspect2AssemblyName );

        var leftCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Left.cs", leftCode } },
            assemblyName: leftAssemblyName,
            additionalReferences: new[] { aspect1Compilation.ToMetadataReference() } );

        var rightCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Right.cs", rightCode } },
            assemblyName: rightAssemblyName,
            additionalReferences: new[] { aspect2Compilation.ToMetadataReference() } );

        var targetCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Target.cs", targetCode } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { leftCompilation.ToMetadataReference(), rightCompilation.ToMetadataReference() } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        factory.CreatePipeline( aspect1Compilation );
        factory.CreatePipeline( aspect2Compilation );
        factory.CreatePipeline( leftCompilation );
        factory.CreatePipeline( rightCompilation );
        var targetProjectPipeline = factory.CreatePipeline( targetCompilation );

        // First execution of the pipeline.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out var results ) );
        var dumpedResults = DumpResults( results );

        AssertEx.EolInvariantEqual( expectedResult, dumpedResults );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );

        // Second execution with the same compilation. The result should be the same, and the number of executions should not change because the result is cached.
        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation, default, out var results2 ) );
        var dumpedResults2 = DumpResults( results2 );
        AssertEx.EolInvariantEqual( expectedResult, dumpedResults2 );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );
    }

    [Fact]
    public void IntroducedSyntaxTreeConflictAndChange()
    {
        // Tests a situation when designtime pipeline generated a syntax tree with undeterministic name.
        // Removing a type caused names to change in such a way that invalidated syntax trees were not correctly cleaned from AspectPipelineResult,
        // causing an exception.
        // For the user it happened quite reliably when trying to cut-paste a type.
        var aspectAssemblyName = "aspect_" + RandomIdGenerator.GenerateId();
        var targetAssemblyName = "target_" + RandomIdGenerator.GenerateId();

        const string aspectCode = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

public class TestAspect : TypeAspect
{
    [Introduce]
    public void Foo() {}
}
";

        const string targetCodeA1 = @"
[TestAspect]
public partial class A
{
}

[TestAspect]
public partial class A<T>
{
}
";

        const string targetCodeB1 = @"
[TestAspect]
public partial class A<T,U>
{
}
";

        const string targetCodeA2 = @"
[TestAspect]
public partial class A
{
}
";

        const string targetCodeB2 = @"
[TestAspect]
public partial class A<T,U>
{
}
";

        const string expectedResult1 = @"
TargetA.cs:
0 diagnostic(s):
0 suppression(s):
2 introductions(s):
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class A
{
    public void Foo()
    {
    }
}
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class A<T>
{
    public void Foo()
    {
    }
}
----------------------------------------------------------
TargetB.cs:
0 diagnostic(s):
0 suppression(s):
1 introductions(s):
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class A<T, U>
{
    public void Foo()
    {
    }
}
";

        const string expectedResult2 = @"
TargetA.cs:
0 diagnostic(s):
0 suppression(s):
1 introductions(s):
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class A
{
    public void Foo()
    {
    }
}
----------------------------------------------------------
TargetB.cs:
0 diagnostic(s):
0 suppression(s):
1 introductions(s):
/// <auto-generated>
/// Generated by Metalama to support the code editing experience. This is NOT the code that gets executed.
/// </auto-generated>
partial class A<T, U>
{
    public void Foo()
    {
    }
}
";

        using var testContext = this.CreateTestContext();

        var aspectCompilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "Aspect.cs", aspectCode } },
            assemblyName: aspectAssemblyName );

        var targetCompilation1 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "TargetA.cs", targetCodeA1 }, { "TargetB.cs", targetCodeB1 } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { aspectCompilation.ToMetadataReference() } );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );
        var aspectProjectPipeline = factory.CreatePipeline( aspectCompilation );
        var targetProjectPipeline = factory.CreatePipeline( targetCompilation1 );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation1, default, out var results1 ) );
        var dumpedResults1 = DumpResults( results1 );

        AssertEx.EolInvariantEqual( expectedResult1.Trim(), dumpedResults1 );
        Assert.Equal( 1, aspectProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 1, aspectProjectPipeline.PipelineInitializationCount );
        Assert.Equal( 1, targetProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 1, targetProjectPipeline.PipelineInitializationCount );

        var targetCompilation2 = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>() { { "TargetA.cs", targetCodeA2 }, { "TargetB.cs", targetCodeB2 } },
            assemblyName: targetAssemblyName,
            additionalReferences: new[] { aspectCompilation.ToMetadataReference() } );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, targetCompilation2, default, out var results2 ) );
        var dumpedResults2 = DumpResults( results2 );

        AssertEx.EolInvariantEqual( expectedResult2.Trim(), dumpedResults2 );
        Assert.Equal( 1, aspectProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 1, aspectProjectPipeline.PipelineInitializationCount );
        Assert.Equal( 2, targetProjectPipeline.PipelineExecutionCount );
        Assert.Equal( 1, targetProjectPipeline.PipelineInitializationCount );
    }

    [Fact]
    public void IncompleteClassWithAspect()
    {
        const string aspect =
            """
            using Metalama.Framework.Aspects;

            class Aspect : TypeAspect
            {
            }
            """;

        const string target =
            """
            [Aspect]
            public partial c
            """;

        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string> { ["aspect.cs"] = aspect, ["target.cs"] = target };

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test", ignoreErrors: true );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }

    [Fact]
    public void TopLevelStatementWithInvalidAttribute()
    {
        const string attribute = "class MyAttribute : System.Attribute;";

        const string program =
            """
            [MyAttribute]
            System.Console.WriteLine();
            """;

        const string aspect =
            """
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;

            [assembly: Aspect]

            class Aspect : CompilationAspect
            {
                public override void BuildAspect(IAspectBuilder<ICompilation> builder)
                {
                    foreach (var attribute in builder.Target.GetAllAttributesOfType(typeof(MyAttribute)))
                    {
                        _ = attribute.ContainingDeclaration;
                    }
                }
            }
            """;

        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string> { ["attribute.cs"] = attribute, ["program.cs"] = program, ["aspect.cs"] = aspect };

        var compilation = testContext.CreateCSharpCompilation( code, assemblyName: "test", ignoreErrors: true );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

        Assert.Empty( result.GetAllDiagnostics() );
    }

    [Fact]
    public void IncompleteGenericArgumentInTransformationTarget()
    {
        // This is test of #35362.
        // This bug only occured when the pipeline had a valid state and the compilation got into invalid state, during AspectPipelineResult.Update.
        // It happened during the second TryExecute because the representation of ErrorTypeSymbol was throwing exceptions.
        // The third step is there just for completeness, so that the pipeline manages to recover from the error.
        const string aspect =
            """
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;

            public class TestAspect : ConstructorAspect
            {
                public override void BuildAspect(IAspectBuilder<IConstructor> builder)
                {
                    builder.IntroduceParameter("TestParameter", typeof(int), TypedConstant.Create(1));
                }
            }
            """;

        const string target1 =
            """
            using System.Collections.Generic;

            public partial class TargetCode
            {
                [TestAspect]
                public TargetCode(List<int> x)
                {
                }
            }
            """;

        const string target2 =
            """
            using System.Collections.Generic;

            public partial class TargetCode
            {
                [TestAspect]
                public TargetCode(List<List<>> x)
                {
                }
            }
            """;

        const string target3 =
            """
            using System.Collections.Generic;

            public partial class TargetCode
            {
                [TestAspect]
                public TargetCode(List<List<int>> x)
                {
                }
            }
            """;

        using var testContext = this.CreateTestContext();

        var code1 = new Dictionary<string, string> { ["aspect.cs"] = aspect, ["target.cs"] = target1 };
        var code2 = new Dictionary<string, string> { ["aspect.cs"] = aspect, ["target.cs"] = target2 };
        var code3 = new Dictionary<string, string> { ["aspect.cs"] = aspect, ["target.cs"] = target3 };

        var compilation1 = testContext.CreateCSharpCompilation( code1, assemblyName: "test", ignoreErrors: true );
        var compilation2 = testContext.CreateCSharpCompilation( code2, assemblyName: "test", ignoreErrors: true );
        var compilation3 = testContext.CreateCSharpCompilation( code3, assemblyName: "test", ignoreErrors: true );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation1, default, out var result1 ) );
        Assert.Single( result1.Result.IntroducedSyntaxTrees );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation2, default, out var result2 ) );
        Assert.Single( result2.Result.IntroducedSyntaxTrees );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation3, default, out var result3 ) );
        Assert.Single( result3.Result.IntroducedSyntaxTrees );
    }

    [Fact]
    public void TypeFabric()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>()
        {
            ["aspect.cs"] =
                """
                using Metalama.Framework.Aspects;

                class MyAspect : TypeAspect
                {
                   [Introduce]
                   void IntroducedMethod() {}
                }
                """,
            ["target.cs"] =
                """
                using Metalama.Framework.Fabrics;
                using Metalama.Framework.Aspects;

                class C
                {
                    class Fabric : TypeFabric
                    {
                        public override void AmendType( ITypeAmender amender )
                            => amender.AddAspect<MyAspect>();
                    } 
                }
                """
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }
}