// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER && NET7_0_OR_GREATER
// We don't run these tests with old frameworks because they require the type CompilerFeatureRequiredAttribute.

using Metalama.Framework.Code;
using System.Collections.Generic;
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

    [Fact]
    public void ExtensionMembers()
    {
        const string code = """
                            using System;
                            using System.Collections.Generic;
                            using System.Linq;
                            using System.Numerics;
                            
                            public static class MyExtensions
                            {
                                extension(IEnumerable<int> source)
                                {
                                    public IEnumerable<int> ValuesGreaterThan(int threshold)
                                        => source.Where(x => x > threshold);
                                }

                                // Same signature as above, to check if they are grouped (they are not).
                                extension(IEnumerable<int> source)
                                {
                                    public IEnumerable<int> ValuesGreaterThanZero
                                        => source.ValuesGreaterThan(0);
                                }
                                
                                extension(IEnumerable<string> source)
                                {
                                  public IEnumerable<string> ValuesDifferentTo(string threshold)
                                      => source.Where(x => x != threshold);

                                  public IEnumerable<string> ValuesNonNull
                                      => source.ValuesDifferentTo(null);
                                }
                                
                                // Static members only, type parameters.
                                extension<TElement>(IEnumerable<TElement>) where TElement : INumber<TElement>
                               {
                                   public static IEnumerable<TElement> operator *(IEnumerable<TElement> vector, TElement scalar) => throw new NotImplementedException();
                                   public static IEnumerable<TElement> operator *(TElement scalar, IEnumerable<TElement> vector) => throw new NotImplementedException();
                               }
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );
        var type = compilation.Types.Single();

        Assert.Empty( type.Types );
        Assert.All( type.Methods, m => Assert.True( m.IsImplicitlyDeclared ) );
        Assert.Equal( 4, type.ExtensionBlocks.Count );

        var extension1 = type.ExtensionBlocks.OfReceivingType( typeof(IEnumerable<int>) ).OrderBy( x => x.Sources[0].Span.Start ).First();
        Assert.Equal( TypeKind.Extension, extension1.TypeKind );
        Assert.Null( extension1.ReceiverParameter.DeclaringMember );

        // Methods.
        var method = Assert.Single( extension1.Methods );
        Assert.Same( extension1, method.DeclaringType );

        // Depth.
        Assert.Equal( type.Depth + 1, extension1.Depth );
        Assert.Equal( extension1.Depth + 1, method.Depth );

        // Refs.
        var reference = extension1.ToRef();
        var roundloop = reference.GetTarget( compilation );
        Assert.Same( extension1, roundloop );
        
        // Nameless parameter.
        var extension4 = type.ExtensionBlocks.OrderBy( x => x.Sources[0].Span.Start ).ElementAt( 3 );
        Assert.Empty( extension4.ReceiverParameter.Name );
        
        // Type parameters.
        Assert.Single( extension4.TypeParameters );
    }
}

#endif