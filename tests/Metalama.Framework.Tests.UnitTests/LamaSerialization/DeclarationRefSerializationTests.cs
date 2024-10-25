// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public class DeclarationRefSerializationTests : SerializationTestsBase
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