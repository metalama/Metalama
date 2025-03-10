// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

#pragma warning disable VSTHRD200

public sealed class SideBySideVersionTests( ITestOutputHelper logger ) : SideBySideVersionTestsBase( logger )
{
    [Fact]
    public async Task Inheritance()
    {
        const string masterCode =
            """
            using System;
            using Metalama.Framework.Advising;
            using Metalama.Framework.Advising;
            using Metalama.Framework.Aspects;

            [Inheritable]
            public class TheAspect : TypeAspect
            {
                [Introduce( WhenExists = OverrideStrategy.New )]
                public void IntroducedMethod() {}
            }

            [TheAspect]
            public interface TheInterface;
            """;

        const string dependentCode = "public class TheClass : TheInterface;";

        var result = await this.RunPipeline( masterCode, dependentCode );

        Assert.Single( result.Value.Result.SyntaxTreeResults.Single().Value.AspectInstances );
    }

    [Fact]
    public async Task Inheritance_CompileTimeType()
    {
        const string masterCode = """
                                  using System;
                                  using Metalama.Framework.Advising;
                                  using Metalama.Framework.Aspects;
                                  using Metalama.Framework.Code;

                                  [Inheritable]
                                  public class TheAspect : TypeAspect
                                  {
                                    private Type _compileTimeType;
                                  
                                    public TheAspect()
                                    {
                                        this._compileTimeType = typeof(TestType<int>);
                                    }
                                  
                                    public override void BuildAspect(IAspectBuilder<INamedType> builder)
                                    {
                                        builder.IntroduceMethod(nameof(IntroducedMethod), args: new { type = this._compileTimeType });
                                    }
                                  
                                    [Template]
                                    public Type IntroducedMethod([CompileTime] Type type)
                                    {
                                        return type;
                                    }
                                  }

                                  public class TestType<T>;

                                  [TheAspect]
                                  public interface TheInterface
                                  {
                                  }

                                  """;

        const string dependentCode = """
                                     public class TheClass : TheInterface
                                     {
                                     }

                                     """;

        var result = await this.RunPipeline( masterCode, dependentCode );

        Assert.Single( result.Value.Result.SyntaxTreeResults.Single().Value.AspectInstances );
    }
}