// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Testing.UnitTesting;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class SymbolTranslatorTests : UnitTestClass
{
    private readonly ITestOutputHelper _logger;

    public SymbolTranslatorTests( ITestOutputHelper logger )
    {
        this._logger = logger;
    }

    [Fact]
    public void TestAll()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;

namespace Ns1 
{ 
  class C1 {}
  namespace Ns2 
  {
    class C2 {}
  }
}

class C
{
    int _field;
    int Property { get; set; }
    string this[int index] => """";
    string this[long index] => """";
    void M() {}
    void M(int x) {}
    event EventHandler Event;
}

class D : C
{
}
";

        var compilation = testContext.CreateCompilationModel( code );

        var translator = compilation.CompilationContext.SymbolTranslator;

        foreach ( var declaration in compilation.GetContainedDeclarations() )
        {
            var symbol = declaration.GetSymbol();

            if ( symbol == null )
            {
                continue;
            }

            this._logger.WriteLine( declaration.ToDisplayString() );

            var translatedSymbol = translator.Translate( symbol );
            Assert.Same( symbol, translatedSymbol );
        }
    }
}