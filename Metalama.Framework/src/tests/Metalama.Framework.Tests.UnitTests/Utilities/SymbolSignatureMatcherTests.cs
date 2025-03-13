// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

        var oldType = oldCompilation.GetTypeByMetadataName( "MyString" );
        var oldOverload = oldType.GetMembers( "TrimStart" ).Single();

        var newCompilation = context.CreateCSharpCompilation( newCode );

        var newOverload = newCompilation.GetTypeByMetadataName( "MyString" )
            .GetMembers( "TrimStart" )
            .OfType<IMethodSymbol>()
            .Single( m => !m.Parameters[0].IsParams );

        var foundOverloads = SymbolSignatureMatcher.GetMembersOfCompatibleSignature( 
            oldType,
            CompilationContextFactory.GetCompilationContext( oldCompilation ),
            newOverload,
            CompilationContextFactory.GetCompilationContext( newCompilation ) );

        Assert.Same( oldOverload, Assert.Single( foundOverloads ) );
    }

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

        var oldType = oldCompilation.GetTypeByMetadataName( "MyStringBuilder" );
        var oldOverload = oldType.GetMembers( "Append" ).Single();

        var newCompilation = context.CreateCSharpCompilation( newCode );

        var newOverload = newCompilation.GetTypeByMetadataName( "MyStringBuilder" )
            .GetMembers( "Append" )
            .OfType<IMethodSymbol>()
            .Single( m => m.Parameters[0].RefKind == RefKind.Ref );

        var foundOverloads = SymbolSignatureMatcher.GetMembersOfCompatibleSignature(
            oldType,
            CompilationContextFactory.GetCompilationContext( oldCompilation ),
            newOverload,
            CompilationContextFactory.GetCompilationContext( newCompilation ) );

        Assert.Same( oldOverload, Assert.Single( foundOverloads ) );
    }
}