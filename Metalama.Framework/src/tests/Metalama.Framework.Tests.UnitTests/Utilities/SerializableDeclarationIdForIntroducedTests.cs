// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Testing.UnitTesting;
using Xunit;
using Xunit.Abstractions;
using static Metalama.Framework.Tests.UnitTests.Utilities.SerializableDeclarationIdTests;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class SerializableDeclarationIdForIntroducedTests : UnitTestClass
{
    public SerializableDeclarationIdForIntroducedTests( ITestOutputHelper? logger = null ) : base( logger, false ) { }

    [Fact]
    public void TestAllDeclarations()
    {
        const string code = """
                            using Metalama.Framework.Advising;
                            using Metalama.Framework.Aspects; 

                            class Aspect<T> : TypeAspect
                            {
                              [Introduce]
                              void M<T2>(int p) {}
                              [Introduce]
                              int this[int i] => 0;
                              [Introduce]
                              int _field;
                              [Introduce]
                              event System.EventHandler Event;
                              [Introduce]
                              ~Aspect() {}
                            }

                            [Aspect<int>]
                            class C { }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( code );

        foreach ( var declaration in compilation.GetContainedDeclarations() )
        {
            Roundtrip( declaration, compilation, this.TestOutput );
        }

        Roundtrip( compilation, compilation, this.TestOutput );
    }
}