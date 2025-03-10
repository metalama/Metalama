// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public sealed class DeclarationRefSerializationTests : SerializationTestsBase
{
    [Fact]
    public void SymbolRef()
    {
        const string code = "public class C;";
        using var testContext = this.CreateTestContextWithCode( code );
        var initialRef = testContext.Compilation.Types.Single().ToRef();

        var roundtripRef = TestSerialization( testContext, initialRef, testEquality: false );

        var initialSymbol = initialRef.GetTarget( testContext.Compilation );
        var roundtripSymbol = roundtripRef.GetTarget( testContext.Compilation );

        Assert.Same( initialSymbol, roundtripSymbol );
    }
}