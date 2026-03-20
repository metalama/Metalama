// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public class SymbolSignatureMatcherTests : UnitTestClass
{
    [Fact]
    public void NonParamsOverloadAdded()
    {
        using var context = this.CreateTestContext();

        const string oldCode = """
                               class MyString
                               {
                                   public MyString TrimStart(params char[]? trimChars) => this;
                               }
                               """;

        const string newCode = """
                               class MyString
                               {
                                   public MyString TrimStart(char trimChar) => this;
                                   public MyString TrimStart(params char[]? trimChars) => this;
                               }
                               """;

        var oldCompilation = context.CreateCSharpCompilation( oldCode );

        var oldType = oldCompilation.GetTypeByMetadataName( "MyString" ).AssertNotNull();
        var oldOverload = oldType.GetMembers( "TrimStart" ).Single();

        var newCompilation = context.CreateCSharpCompilation( newCode );

        var newOverload = newCompilation.GetTypeByMetadataName( "MyString" )
            .AssertNotNull()
            .GetMembers( "TrimStart" )
            .OfType<IMethodSymbol>()
            .Single( m => !m.Parameters[0].IsParams );

        var foundOverloads = oldType.GetMembersOfCompatibleSignature(
            oldCompilation.GetCompilationContext(),
            newOverload,
            newCompilation.GetCompilationContext() );

        Assert.Same( oldOverload, Assert.Single( foundOverloads ) );
    }

#if ROSLYN_4_12_0_OR_GREATER
    [Fact]
    public void OverloadResolutionPriorityOrdering()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            using System;
                            using System.Runtime.CompilerServices;

                            class C
                            {
                                public void Foo(int x) { }
                                [OverloadResolutionPriority(1)]
                                public void Foo(object x) { }
                            }

                            namespace System.Runtime.CompilerServices
                            {
                                [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
                                sealed class OverloadResolutionPriorityAttribute : Attribute
                                {
                                    public OverloadResolutionPriorityAttribute(int priority) { Priority = priority; }
                                    public int Priority { get; }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var compilationContext = compilation.GetCompilationContext();
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();

        var intType = compilation.GetSpecialType( SpecialType.System_Int32 );

        var results = type.GetMethodsOfCompatibleSignature(
            compilationContext,
            "Foo",
            new ITypeSymbol?[] { intType },
            compilationContext,
            isStatic: null ).ToArray();

        Assert.Equal( 2, results.Length );

        // The method with OverloadResolutionPriority(1) should come first.
        Assert.Equal( "Foo", results[0].Name );
        Assert.Equal( SpecialType.System_Object, results[0].Parameters[0].Type.SpecialType );
        Assert.Equal( SpecialType.System_Int32, results[1].Parameters[0].Type.SpecialType );
    }
#endif

    [Fact]
    public void InterpolatedStringHandlerOverloadAdded()
    {
        using var context = this.CreateTestContext();

        const string oldCode = """
                               class MyStringBuilder
                               {
                                   public void Append(string? value) { }
                               }
                               """;

        const string newCode = """
                               using System;
                               using System.Runtime.CompilerServices;
                               using System.Text;

                               class MyStringBuilder
                               {
                                   public void Append(string? value) { }
                                   public void Append(ref AppendInterpolatedStringHandler handler) { }

                                   [InterpolatedStringHandler]
                                   public struct AppendInterpolatedStringHandler
                                   {
                                       public AppendInterpolatedStringHandler(int literalLength, int formattedCount) { }
                                   }
                               }

                               namespace System.Runtime.CompilerServices
                               {
                                   public sealed class InterpolatedStringHandlerAttribute : Attribute;
                               }
                               """;

        var oldCompilation = context.CreateCSharpCompilation( oldCode );

        var oldType = oldCompilation.GetTypeByMetadataName( "MyStringBuilder" ).AssertNotNull();
        var oldOverload = oldType.GetMembers( "Append" ).Single();

        var newCompilation = context.CreateCSharpCompilation( newCode );

        var newOverload = newCompilation.GetTypeByMetadataName( "MyStringBuilder" )
            .AssertNotNull()
            .GetMembers( "Append" )
            .OfType<IMethodSymbol>()
            .Single( m => m.Parameters[0].RefKind == RefKind.Ref );

        var foundOverloads = oldType.GetMembersOfCompatibleSignature(
            oldCompilation.GetCompilationContext(),
            newOverload,
            newCompilation.GetCompilationContext() );

        Assert.Same( oldOverload, Assert.Single( foundOverloads ) );
    }
}