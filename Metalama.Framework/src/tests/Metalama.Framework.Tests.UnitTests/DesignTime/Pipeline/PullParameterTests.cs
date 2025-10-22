// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

public class PullParameterTests : UnitTestClass
{
#if NET5_0_OR_GREATER
        [Fact( Skip = "CLR internal error when unloading the domain" )]
#else
    [Fact]
#endif
    public void CrossProjectIntegration()
    {
        using var testContext = this.CreateTestContext();

        const string code1 = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

public class Aspect1 : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            ""p1"",
            typeof(int),
            TypedConstant.Create( 15 ),
            PullStrategy.IntroduceParameterAndPull( defaultValue: TypedConstant.Create( 20 ) ) );
    }
}

public partial class C
{
    [Aspect1]
    public C() { }

    public C( string s ) : this() { }
}

";

        const string code2 = """
                             partial class D : C 
                             {
                               D( string s ) : base( s ) {}
                             }
                             """;

        using var testContext1 = this.CreateTestContext();

        var compilation1 = testContext.CreateCSharpCompilation( code1 );

        using var testContext2 = this.CreateTestContext();

        var compilation2 = testContext.CreateCSharpCompilation( code2, additionalReferences: [compilation1.ToMetadataReference()] );

        // We have to execute the pipeline on compilation1 first and explicitly because implicit running is not currently possible
        // because of missing project options.

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        Assert.True( pipelineFactory.TryExecute( testContext1.ProjectOptions, compilation1, default, out var compilationResult1 ) );
        Assert.True( pipelineFactory.TryExecute( testContext2.ProjectOptions, compilation2, default, out var compilationResult2 ) );

        Assert.Single( compilationResult2.Result.IntroducedSyntaxTrees );
    }
}