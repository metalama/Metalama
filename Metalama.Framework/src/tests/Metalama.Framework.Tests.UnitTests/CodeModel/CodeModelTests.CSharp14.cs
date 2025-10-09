// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Code;
using System.Linq;
using Xunit;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed partial class CodeModelTests
{
    [Fact]
    public void UnaryCompoundOperator()
    {
        const string code = """
                            class C1
                            {
                                public int Value;

                                public void operator ++()
                                {
                                    Value++;
                                }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var @operator = compilation.Types.Single().Methods.Single();
        Assert.Equal( MethodKind.Operator, @operator.MethodKind );
        Assert.Equal( OperatorKind.IncrementAssignment, @operator.OperatorKind );
    }
    
    [Fact]
    public void BinaryCompoundOperator()
    {
        const string code = """
                            class C1
                            {
                                public int Value;

                               public void operator +=(int x)
                               {
                                   Value+=x;
                               }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var @operator = compilation.Types.Single().Methods.Single();
        Assert.Equal( MethodKind.Operator, @operator.MethodKind );
        Assert.Equal( OperatorKind.AdditionAssignment, @operator.OperatorKind );
    }
}

#endif